using System.Text;

namespace LibraryConnect.Marc;

/// <summary>
/// Ghi biểu ghi MARC 21 ra định dạng trao đổi ISO 2709 (tệp .mrc).
///
/// Every length in the record — the directory entries and the two numeric areas of the leader — is a
/// count of BYTES in the UTF-8 encoding of the data, never a count of characters. For Vietnamese
/// this is the difference between a readable file and an unreadable one: "Giáo trình" is 10
/// characters but 13 bytes, so a character-based count would leave every downstream reader parsing
/// from the wrong offset for the rest of the record.
/// </summary>
public static class Iso2709Writer
{
    /// <summary>Ghi một biểu ghi thành mảng byte, đã bao gồm ký tự kết thúc biểu ghi.</summary>
    public static byte[] Write(MarcRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        // Trường dài quá 9.999 byte không khai nổi trong danh mục ISO 2709. Chuẩn bảo chia thành
        // nhiều lần lặp — làm luôn ở đây thay vì bắt người gọi nhớ, vì quên là hỏng cả tệp xuất.
        var (chuanBi, boQua) = Iso2709FieldSplitter.Prepare(record);

        if (chuanBi is null)
        {
            throw new MarcException(boQua!.Reason);
        }

        record = chuanBi;

        var entries = BuildFields(record);

        // The directory has one 12-byte entry per field plus its own terminator; the data area
        // starts immediately after it.
        var directoryLength = entries.Count * MarcConstants.DirectoryEntryLength + 1;
        var baseAddress = MarcLeader.Length + directoryLength;
        var dataLength = entries.Sum(entry => entry.Data.Length);
        var recordLength = baseAddress + dataLength + 1;

        if (recordLength > MarcConstants.MaxRecordLength)
        {
            throw new MarcException(
                $"Biểu ghi dài {MarcText.Number(recordLength)} byte, vượt giới hạn {MarcText.Number(MarcConstants.MaxRecordLength)} byte của " +
                "định dạng ISO 2709. Hãy tách bớt nội dung, ví dụ chuyển phần tóm tắt dài sang tệp đính kèm.");
        }

        var leader = record.Leader.Clone();
        leader.SetRecordLength(recordLength);
        leader.SetBaseAddressOfData(baseAddress);
        // The stored record is always UTF-8 in this system, so position 09 says so.
        leader.CharacterCodingScheme = 'a';

        var buffer = new byte[recordLength];
        var offset = 0;

        offset += Encoding.UTF8.GetBytes(leader.ToString(), 0, MarcLeader.Length, buffer, offset);

        var position = 0;

        foreach (var entry in entries)
        {
            offset += WriteDirectoryEntry(buffer, offset, entry.Tag, entry.Data.Length, position);
            position += entry.Data.Length;
        }

        buffer[offset++] = MarcConstants.FieldTerminator;

        foreach (var entry in entries)
        {
            Buffer.BlockCopy(entry.Data, 0, buffer, offset, entry.Data.Length);
            offset += entry.Data.Length;
        }

        buffer[offset] = MarcConstants.RecordTerminator;

        return buffer;
    }

    /// <summary>Kết quả ghi một lô: tệp đã dựng và danh sách biểu ghi phải bỏ lại.</summary>
    public record Iso2709WriteResult(byte[] Content, IReadOnlyList<Iso2709FieldSplitter.BoQua> Skipped);

    /// <summary>
    /// Ghi nhiều biểu ghi vào một tệp .mrc, nối tiếp nhau không có dấu ngăn nào khác.
    ///
    /// Biểu ghi nào không biểu diễn nổi bằng ISO 2709 thì bỏ lại và ghi vào danh sách, chứ không
    /// đánh đổ cả lô. Đây là lỗi đã xảy ra thật: xuất 7.675 biểu ghi trả về lỗi hệ thống và không
    /// lấy được biểu ghi nào, chỉ vì đúng một biểu ghi có phần tóm tắt dài 10.686 byte.
    /// </summary>
    public static Iso2709WriteResult WriteMany(IEnumerable<MarcRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        using var stream = new MemoryStream();
        var boQua = new List<Iso2709FieldSplitter.BoQua>();

        foreach (var record in records)
        {
            var (chuanBi, ly) = Iso2709FieldSplitter.Prepare(record);

            if (chuanBi is null)
            {
                boQua.Add(ly!);
                continue;
            }

            byte[] bytes;

            try
            {
                bytes = Write(chuanBi);
            }
            catch (MarcException ex)
            {
                boQua.Add(new Iso2709FieldSplitter.BoQua(
                    record.ControlNumber, record.GetSubfield("245", 'a'), ex.Message));
                continue;
            }

            stream.Write(bytes, 0, bytes.Length);
        }

        return new Iso2709WriteResult(stream.ToArray(), boQua);
    }

    public static async Task WriteManyAsync(
        IEnumerable<MarcRecord> records,
        Stream destination,
        CancellationToken ct = default)
    {
        foreach (var record in records)
        {
            ct.ThrowIfCancellationRequested();
            var bytes = Write(record);
            await destination.WriteAsync(bytes, ct);
        }

        await destination.FlushAsync(ct);
    }

    private static List<FieldEntry> BuildFields(MarcRecord record)
    {
        var entries = new List<FieldEntry>(record.ControlFields.Count + record.DataFields.Count);

        foreach (var field in record.ControlFields)
        {
            ValidateTag(field.Tag);
            entries.Add(new FieldEntry(field.Tag, BuildControlFieldData(field)));
        }

        foreach (var field in record.DataFields)
        {
            ValidateTag(field.Tag);
            entries.Add(new FieldEntry(field.Tag, BuildDataFieldData(field)));
        }

        return entries;
    }

    private static byte[] BuildControlFieldData(MarcControlField field)
    {
        var value = Encoding.UTF8.GetBytes(field.Value);
        var data = new byte[value.Length + 1];

        Buffer.BlockCopy(value, 0, data, 0, value.Length);
        data[^1] = MarcConstants.FieldTerminator;

        EnsureFieldFits(field.Tag, data.Length);

        return data;
    }

    private static byte[] BuildDataFieldData(MarcDataField field)
    {
        using var stream = new MemoryStream();

        stream.WriteByte(EncodeIndicator(field.Tag, field.Indicator1));
        stream.WriteByte(EncodeIndicator(field.Tag, field.Indicator2));

        foreach (var subfield in field.Subfields)
        {
            stream.WriteByte(MarcConstants.SubfieldDelimiter);
            stream.WriteByte(EncodeSubfieldCode(field.Tag, subfield.Code));

            var value = Encoding.UTF8.GetBytes(subfield.Value);
            stream.Write(value, 0, value.Length);
        }

        stream.WriteByte(MarcConstants.FieldTerminator);

        var data = stream.ToArray();
        EnsureFieldFits(field.Tag, data.Length);

        return data;
    }

    private static int WriteDirectoryEntry(byte[] buffer, int offset, string tag, int length, int position)
    {
        var entry = string.Concat(
            tag,
            length.ToString().PadLeft(4, '0'),
            position.ToString().PadLeft(5, '0'));

        return Encoding.UTF8.GetBytes(entry, 0, entry.Length, buffer, offset);
    }

    private static void ValidateTag(string tag)
    {
        if (tag.Length != MarcConstants.TagLength || !tag.All(char.IsAsciiDigit))
        {
            throw new MarcException($"Tag trường \"{tag}\" không hợp lệ: phải gồm đúng 3 chữ số.");
        }
    }

    private static void EnsureFieldFits(string tag, int length)
    {
        if (length > MarcConstants.MaxFieldLength)
        {
            throw new MarcException(
                $"Trường {tag} dài {MarcText.Number(length)} byte, vượt giới hạn {MarcText.Number(MarcConstants.MaxFieldLength)} byte của " +
                "một trường ISO 2709. Hãy chia nội dung thành nhiều lần lặp của trường này.");
        }
    }

    /// <summary>
    /// Indicator và mã trường con chiếm đúng một byte trong biểu ghi, nên chúng phải là ký tự ASCII.
    /// Bắt lỗi ở đây thay vì để dữ liệu lệch offset khi hệ thống khác đọc lại.
    /// </summary>
    private static byte EncodeIndicator(string tag, char indicator)
    {
        if (!char.IsAscii(indicator))
        {
            throw new MarcException(
                $"Chỉ thị \"{indicator}\" của trường {tag} không hợp lệ: chỉ thị phải là một ký tự ASCII " +
                "(chữ số, chữ cái thường hoặc khoảng trắng).");
        }

        return (byte)indicator;
    }

    private static byte EncodeSubfieldCode(string tag, char code)
    {
        if (!char.IsAsciiLetterLower(code) && !char.IsAsciiDigit(code))
        {
            throw new MarcException(
                $"Mã trường con \"{code}\" của trường {tag} không hợp lệ: chỉ chấp nhận chữ cái thường a–z " +
                "hoặc chữ số 0–9.");
        }

        return (byte)code;
    }

    private readonly record struct FieldEntry(string Tag, byte[] Data);
}
