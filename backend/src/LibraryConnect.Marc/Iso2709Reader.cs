using System.Text;

namespace LibraryConnect.Marc;

/// <summary>
/// Cách hiểu chuỗi byte của biểu ghi thành ký tự.
/// </summary>
public enum MarcCharset
{
    /// <summary>Tự nhận: theo Leader vị trí 09, có kiểm tra lại bằng chính dữ liệu.</summary>
    Auto = 0,

    Utf8 = 1,

    Marc8 = 2
}

/// <summary>
/// Đọc biểu ghi MARC 21 từ định dạng trao đổi ISO 2709 (tệp .mrc).
///
/// Real exchange files are not always well formed: directory offsets can disagree with the data,
/// the record length in the leader can be wrong, and files sometimes carry line breaks between
/// records. The reader therefore works from the directory when the directory is trustworthy and
/// falls back to scanning for field terminators when it is not, so a file that another system wrote
/// carelessly still imports instead of failing wholesale.
/// </summary>
public static class Iso2709Reader
{
    /// <summary>Đọc đúng một biểu ghi từ mảng byte.</summary>
    public static MarcRecord Read(byte[] data, MarcCharset charset = MarcCharset.Auto)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ReadRecord(data.AsSpan(), charset);
    }

    /// <summary>Đọc mọi biểu ghi trong một tệp. Dừng ngay ở biểu ghi hỏng đầu tiên.</summary>
    public static IReadOnlyList<MarcRecord> ReadAll(byte[] data, MarcCharset charset = MarcCharset.Auto)
    {
        var result = ReadAllTolerant(data, charset);

        if (result.Errors.Count > 0)
        {
            var first = result.Errors[0];
            throw new MarcException(first.Message) { RecordNumber = first.RecordNumber, Position = first.Position };
        }

        return result.Records;
    }

    /// <summary>
    /// Đọc mọi biểu ghi trong tệp và ghi nhận lỗi của từng biểu ghi thay vì dừng lại.
    /// Đây là cách màn hình nhập ISO 2709 dùng: cán bộ thấy được biểu ghi nào hỏng và vì sao,
    /// những biểu ghi còn lại vẫn nhập được.
    /// </summary>
    public static Iso2709ReadResult ReadAllTolerant(byte[] data, MarcCharset charset = MarcCharset.Auto)
    {
        ArgumentNullException.ThrowIfNull(data);

        var records = new List<MarcRecord>();
        var errors = new List<MarcReadError>();
        var offset = SkipByteOrderMark(data);
        var number = 0;

        while (offset < data.Length)
        {
            // Files written on Windows often carry a line break after each record terminator.
            if (IsFiller(data[offset]))
            {
                offset++;
                continue;
            }

            number++;
            var start = offset;
            var end = FindRecordEnd(data, start);

            try
            {
                records.Add(ReadRecord(data.AsSpan(start, end - start), charset));
            }
            catch (MarcException exception)
            {
                errors.Add(new MarcReadError(number, start, exception.Message));
            }

            offset = end;
        }

        return new Iso2709ReadResult(records, errors);
    }

    /// <summary>Đọc từng biểu ghi từ luồng, không nạp toàn bộ tệp vào bộ nhớ.</summary>
    public static async Task<Iso2709ReadResult> ReadAllAsync(
        Stream stream,
        MarcCharset charset = MarcCharset.Auto,
        CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);

        return ReadAllTolerant(buffer.ToArray(), charset);
    }

    private static MarcRecord ReadRecord(ReadOnlySpan<byte> data, MarcCharset charset)
    {
        if (data.Length < MarcLeader.Length)
        {
            throw new MarcException(
                $"Biểu ghi chỉ dài {data.Length} byte, ngắn hơn 24 byte của đầu biểu. Tệp có thể bị cắt cụt.");
        }

        var leaderText = Encoding.ASCII.GetString(data[..MarcLeader.Length]);
        var leader = new MarcLeader(leaderText);

        var directoryEnd = data.IndexOf(MarcConstants.FieldTerminator);

        if (directoryEnd < MarcLeader.Length)
        {
            throw new MarcException(
                "Không tìm thấy dấu kết thúc danh mục (0x1E). Tệp không đúng định dạng ISO 2709.");
        }

        var directoryLength = directoryEnd - MarcLeader.Length;

        if (directoryLength % MarcConstants.DirectoryEntryLength != 0)
        {
            throw new MarcException(
                $"Danh mục dài {directoryLength} byte, không chia hết cho 12 byte của một mục. " +
                "Biểu ghi bị hỏng cấu trúc.");
        }

        var entryCount = directoryLength / MarcConstants.DirectoryEntryLength;
        var baseAddress = directoryEnd + 1;

        // The base address recorded in the leader must agree with where the directory actually ends.
        // When it does not, the directory wins: it is the part the offsets are relative to.
        var record = new MarcRecord { Leader = leader };
        var resolvedCharset = ResolveCharset(charset, leader, data[baseAddress..]);

        // Field boundaries scanned from the data itself, used when the directory cannot be trusted.
        var scanned = ScanFields(data[baseAddress..]);
        var trustDirectory = true;
        var entries = new List<DirectoryEntry>(entryCount);

        for (var index = 0; index < entryCount; index++)
        {
            var entryStart = MarcLeader.Length + index * MarcConstants.DirectoryEntryLength;
            var entryText = Encoding.ASCII.GetString(
                data.Slice(entryStart, MarcConstants.DirectoryEntryLength));

            var tag = entryText[..MarcConstants.TagLength];

            if (!int.TryParse(entryText.AsSpan(3, 4), out var length) ||
                !int.TryParse(entryText.AsSpan(7, 5), out var position))
            {
                throw new MarcException(
                    $"Mục danh mục \"{entryText}\" không hợp lệ: độ dài và vị trí phải là chữ số.");
            }

            entries.Add(new DirectoryEntry(tag, length, position));

            if (position < 0 || length <= 0 || baseAddress + position + length > data.Length)
            {
                trustDirectory = false;
            }
        }

        if (trustDirectory)
        {
            foreach (var entry in entries)
            {
                var content = data.Slice(baseAddress + entry.Position, entry.Length);
                AddField(record, entry.Tag, TrimTerminator(content), resolvedCharset);
            }

            return record;
        }

        // The offsets are wrong. Fields still sit in directory order and are still separated by the
        // field terminator, so pairing the scanned boundaries with the directory tags recovers the
        // record. Anything past the shorter of the two lists is genuinely lost.
        if (scanned.Count < entries.Count)
        {
            throw new MarcException(
                $"Danh mục khai báo {entries.Count} trường nhưng chỉ tìm thấy {scanned.Count} trường trong " +
                "vùng dữ liệu, và các vị trí trong danh mục cũng sai. Biểu ghi bị hỏng, không thể phục hồi.");
        }

        for (var index = 0; index < entries.Count; index++)
        {
            var (start, length) = scanned[index];
            var content = data.Slice(baseAddress + start, length);
            AddField(record, entries[index].Tag, content, resolvedCharset);
        }

        return record;
    }

    private static void AddField(
        MarcRecord record,
        string tag,
        ReadOnlySpan<byte> content,
        MarcCharset charset)
    {
        if (MarcConstants.IsControlFieldTag(tag))
        {
            record.ControlFields.Add(new MarcControlField(tag, Decode(content, charset)));
            return;
        }

        if (content.Length < 2)
        {
            throw new MarcException(
                $"Trường {tag} chỉ dài {content.Length} byte, không đủ chỗ cho hai chỉ thị.");
        }

        var field = new MarcDataField(tag, (char)content[0], (char)content[1]);
        var body = content[2..];

        if (body.Length > 0 && body[0] != MarcConstants.SubfieldDelimiter)
        {
            throw new MarcException(
                $"Trường {tag} không bắt đầu bằng mã trường con (0x1F) sau hai chỉ thị. " +
                "Biểu ghi có thể do phần mềm khác xuất sai cấu trúc.");
        }

        while (body.Length > 0)
        {
            // body[0] is the delimiter; body[1] is the code; the value runs to the next delimiter.
            if (body.Length < 2)
            {
                throw new MarcException($"Trường {tag} kết thúc ngay sau dấu phân cách trường con.");
            }

            var code = (char)body[1];
            var value = body[2..];
            var next = value.IndexOf(MarcConstants.SubfieldDelimiter);

            if (next >= 0)
            {
                field.Subfields.Add(new MarcSubfield(code, Decode(value[..next], charset)));
                body = value[next..];
            }
            else
            {
                field.Subfields.Add(new MarcSubfield(code, Decode(value, charset)));
                break;
            }
        }

        record.DataFields.Add(field);
    }

    /// <summary>Cắt bỏ dấu kết thúc trường nếu có; độ dài trong danh mục đã tính cả dấu này.</summary>
    private static ReadOnlySpan<byte> TrimTerminator(ReadOnlySpan<byte> content) =>
        content.Length > 0 && content[^1] == MarcConstants.FieldTerminator ? content[..^1] : content;

    private static List<(int Start, int Length)> ScanFields(ReadOnlySpan<byte> dataArea)
    {
        var fields = new List<(int, int)>();
        var start = 0;

        for (var index = 0; index < dataArea.Length; index++)
        {
            var value = dataArea[index];

            if (value == MarcConstants.FieldTerminator)
            {
                fields.Add((start, index - start));
                start = index + 1;
            }
            else if (value == MarcConstants.RecordTerminator)
            {
                break;
            }
        }

        return fields;
    }

    /// <summary>
    /// Quyết định bảng mã của biểu ghi.
    ///
    /// Leader/09 is what the standard says to use, but exports that write UTF-8 while leaving the
    /// position blank are common enough that the claim is checked against the data: if the bytes
    /// decode as strict UTF-8 they are UTF-8, because a MARC-8 stream carrying diacritics does not
    /// form valid UTF-8 sequences by accident.
    /// </summary>
    private static MarcCharset ResolveCharset(MarcCharset requested, MarcLeader leader, ReadOnlySpan<byte> dataArea)
    {
        if (requested != MarcCharset.Auto)
        {
            return requested;
        }

        if (leader.CharacterCodingScheme == 'a')
        {
            return MarcCharset.Utf8;
        }

        return IsValidUtf8(dataArea) ? MarcCharset.Utf8 : MarcCharset.Marc8;
    }

    private static bool IsValidUtf8(ReadOnlySpan<byte> bytes)
    {
        try
        {
            new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static string Decode(ReadOnlySpan<byte> bytes, MarcCharset charset) =>
        charset == MarcCharset.Marc8
            ? Marc8Encoding.GetString(bytes)
            : Encoding.UTF8.GetString(bytes);

    /// <summary>
    /// Tìm điểm kết thúc của biểu ghi bắt đầu tại <paramref name="start"/>.
    /// Ưu tiên độ dài ghi trong đầu biểu, nhưng chỉ khi nó trỏ đúng vào dấu kết thúc biểu ghi.
    /// </summary>
    private static int FindRecordEnd(byte[] data, int start)
    {
        if (start + MarcLeader.Length <= data.Length &&
            int.TryParse(Encoding.ASCII.GetString(data, start, 5), out var length) &&
            length >= MarcLeader.Length &&
            start + length <= data.Length &&
            data[start + length - 1] == MarcConstants.RecordTerminator)
        {
            return start + length;
        }

        var terminator = Array.IndexOf(data, MarcConstants.RecordTerminator, start);

        return terminator >= 0 ? terminator + 1 : data.Length;
    }

    private static int SkipByteOrderMark(byte[] data) =>
        data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF ? 3 : 0;

    private static bool IsFiller(byte value) =>
        value is (byte)'\r' or (byte)'\n' or 0x00 or (byte)' ';

    private readonly record struct DirectoryEntry(string Tag, int Length, int Position);
}

/// <summary>Kết quả đọc một tệp ISO 2709: các biểu ghi đọc được và lỗi của từng biểu ghi hỏng.</summary>
public record Iso2709ReadResult(IReadOnlyList<MarcRecord> Records, IReadOnlyList<MarcReadError> Errors);

/// <summary>Một biểu ghi không đọc được, kèm số thứ tự và vị trí byte để cán bộ tra lại trong tệp.</summary>
public record MarcReadError(int RecordNumber, long Position, string Message);
