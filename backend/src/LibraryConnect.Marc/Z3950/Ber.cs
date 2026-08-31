using System.Text;

namespace LibraryConnect.Marc.Z3950;

/// <summary>Lớp thẻ của một phần tử BER.</summary>
public enum BerTagClass
{
    Universal = 0,
    Application = 1,
    Context = 2,
    Private = 3,
}

/// <summary>
/// Một phần tử ASN.1 đã mã hóa theo BER (Basic Encoding Rules).
///
/// Z39.50 truyền đi bằng BER chứ không phải JSON hay XML, nên đây là viên gạch dưới cùng của cả
/// phân hệ liên thư viện. Một phần tử gồm ba phần: thẻ (lớp + số hiệu + có phải kiểu ghép không),
/// độ dài, và nội dung. Phần tử ghép thì nội dung lại là một dãy phần tử con.
/// </summary>
public class BerElement
{
    public BerTagClass TagClass { get; init; } = BerTagClass.Universal;

    /// <summary>Số hiệu thẻ trong lớp của nó.</summary>
    public int TagNumber { get; init; }

    /// <summary>Phần tử ghép chứa các phần tử con; phần tử nguyên thủy chứa byte thô.</summary>
    public bool IsConstructed { get; init; }

    public byte[] Content { get; init; } = Array.Empty<byte>();

    public List<BerElement> Children { get; init; } = new();

    // -- Dựng phần tử -------------------------------------------------------------------------

    public static BerElement Primitive(BerTagClass tagClass, int tagNumber, byte[] content) =>
        new() { TagClass = tagClass, TagNumber = tagNumber, IsConstructed = false, Content = content };

    public static BerElement Constructed(
        BerTagClass tagClass, int tagNumber, params BerElement[] children) =>
        new()
        {
            TagClass = tagClass,
            TagNumber = tagNumber,
            IsConstructed = true,
            Children = children.ToList(),
        };

    public static BerElement Constructed(
        BerTagClass tagClass, int tagNumber, IEnumerable<BerElement> children) =>
        new()
        {
            TagClass = tagClass,
            TagNumber = tagNumber,
            IsConstructed = true,
            Children = children.ToList(),
        };

    /// <summary>Số nguyên, mã hóa theo bù hai, độ dài tối thiểu — đúng luật của BER.</summary>
    public static BerElement Integer(BerTagClass tagClass, int tagNumber, long value) =>
        Primitive(tagClass, tagNumber, EncodeInteger(value));

    public static BerElement Boolean(BerTagClass tagClass, int tagNumber, bool value) =>
        Primitive(tagClass, tagNumber, new[] { (byte)(value ? 0xFF : 0x00) });

    /// <summary>Chuỗi. Z39.50 dùng ASCII cho tên cơ sở dữ liệu, UTF-8 cho phần còn lại.</summary>
    public static BerElement String(BerTagClass tagClass, int tagNumber, string value) =>
        Primitive(tagClass, tagNumber, Encoding.UTF8.GetBytes(value));

    /// <summary>Định danh đối tượng, ví dụ 1.2.840.10003.5.10 là cú pháp biểu ghi USMARC.</summary>
    public static BerElement ObjectIdentifier(BerTagClass tagClass, int tagNumber, string oid) =>
        Primitive(tagClass, tagNumber, EncodeOid(oid));

    // -- Đọc giá trị --------------------------------------------------------------------------

    public long AsInteger()
    {
        if (Content.Length == 0)
        {
            return 0;
        }

        // Byte đầu có bit cao là số âm; BER dùng bù hai nên phải mở rộng dấu.
        long value = (sbyte)Content[0];

        for (var index = 1; index < Content.Length; index++)
        {
            value = (value << 8) | Content[index];
        }

        return value;
    }

    public bool AsBoolean() => Content.Length > 0 && Content[0] != 0;

    public string AsString() => Encoding.UTF8.GetString(Content);

    public string AsOid() => DecodeOid(Content);

    /// <summary>Phần tử con đầu tiên mang số hiệu thẻ đã cho, tìm theo lớp ngữ cảnh.</summary>
    public BerElement? Child(int tagNumber, BerTagClass tagClass = BerTagClass.Context) =>
        Children.FirstOrDefault(child => child.TagNumber == tagNumber && child.TagClass == tagClass);

    // -- Mã hóa và giải mã --------------------------------------------------------------------

    public byte[] ToBytes()
    {
        using var buffer = new MemoryStream();
        Write(buffer);
        return buffer.ToArray();
    }

    public void Write(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var content = IsConstructed ? ChildBytes() : Content;

        WriteIdentifier(stream);
        WriteLength(stream, content.Length);
        stream.Write(content, 0, content.Length);
    }

    private byte[] ChildBytes()
    {
        using var buffer = new MemoryStream();

        foreach (var child in Children)
        {
            child.Write(buffer);
        }

        return buffer.ToArray();
    }

    private void WriteIdentifier(Stream stream)
    {
        var first = (byte)(((int)TagClass << 6) | (IsConstructed ? 0x20 : 0x00));

        if (TagNumber < 31)
        {
            stream.WriteByte((byte)(first | TagNumber));
            return;
        }

        // Thẻ từ 31 trở lên viết theo dạng nhiều byte, mỗi byte mang 7 bit.
        stream.WriteByte((byte)(first | 0x1F));

        var parts = new Stack<byte>();
        var value = TagNumber;

        parts.Push((byte)(value & 0x7F));
        value >>= 7;

        while (value > 0)
        {
            parts.Push((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }

        foreach (var part in parts)
        {
            stream.WriteByte(part);
        }
    }

    private static void WriteLength(Stream stream, int length)
    {
        if (length < 128)
        {
            stream.WriteByte((byte)length);
            return;
        }

        // Độ dài lớn viết theo dạng dài: byte đầu là số byte độ dài, rồi tới độ dài dạng big-endian.
        var bytes = new List<byte>();
        var value = length;

        while (value > 0)
        {
            bytes.Insert(0, (byte)(value & 0xFF));
            value >>= 8;
        }

        stream.WriteByte((byte)(0x80 | bytes.Count));

        foreach (var part in bytes)
        {
            stream.WriteByte(part);
        }
    }

    /// <summary>Đọc một phần tử từ vị trí <paramref name="offset"/>, dời con trỏ qua phần tử đó.</summary>
    public static BerElement Read(byte[] data, ref int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset >= data.Length)
        {
            throw new BerException("Hết dữ liệu khi đang đọc thẻ BER.");
        }

        var identifier = data[offset++];
        var tagClass = (BerTagClass)(identifier >> 6);
        var constructed = (identifier & 0x20) != 0;
        var tagNumber = identifier & 0x1F;

        if (tagNumber == 0x1F)
        {
            tagNumber = 0;

            while (true)
            {
                if (offset >= data.Length)
                {
                    throw new BerException("Hết dữ liệu khi đang đọc số hiệu thẻ nhiều byte.");
                }

                var part = data[offset++];
                tagNumber = (tagNumber << 7) | (part & 0x7F);

                if ((part & 0x80) == 0)
                {
                    break;
                }
            }
        }

        var length = ReadLength(data, ref offset, out var indefinite);

        if (indefinite && !constructed)
        {
            throw new BerException("Độ dài không xác định chỉ dùng được cho phần tử ghép.");
        }

        if (constructed)
        {
            var element = new BerElement
            {
                TagClass = tagClass,
                TagNumber = tagNumber,
                IsConstructed = true,
            };

            if (indefinite)
            {
                // Kết thúc bằng hai byte 0x00 0x00 — vài máy chủ Z39.50 đời cũ vẫn gửi kiểu này.
                while (true)
                {
                    if (offset + 1 < data.Length && data[offset] == 0x00 && data[offset + 1] == 0x00)
                    {
                        offset += 2;
                        break;
                    }

                    element.Children.Add(Read(data, ref offset));
                }
            }
            else
            {
                var end = offset + length;

                if (end > data.Length)
                {
                    throw new BerException(
                        $"Phần tử BER khai dài {length} byte nhưng dữ liệu chỉ còn {data.Length - offset} byte.");
                }

                while (offset < end)
                {
                    element.Children.Add(Read(data, ref offset));
                }
            }

            return element;
        }

        if (offset + length > data.Length)
        {
            throw new BerException(
                $"Phần tử BER khai dài {length} byte nhưng dữ liệu chỉ còn {data.Length - offset} byte.");
        }

        var content = new byte[length];
        Array.Copy(data, offset, content, 0, length);
        offset += length;

        return new BerElement
        {
            TagClass = tagClass,
            TagNumber = tagNumber,
            IsConstructed = false,
            Content = content,
        };
    }

    public static BerElement Read(byte[] data)
    {
        var offset = 0;
        return Read(data, ref offset);
    }

    private static int ReadLength(byte[] data, ref int offset, out bool indefinite)
    {
        indefinite = false;

        if (offset >= data.Length)
        {
            throw new BerException("Hết dữ liệu khi đang đọc độ dài BER.");
        }

        var first = data[offset++];

        if ((first & 0x80) == 0)
        {
            return first;
        }

        var count = first & 0x7F;

        if (count == 0)
        {
            indefinite = true;
            return 0;
        }

        if (count > 4)
        {
            throw new BerException($"Độ dài BER {count} byte vượt quá mức hệ thống nhận.");
        }

        var length = 0;

        for (var index = 0; index < count; index++)
        {
            if (offset >= data.Length)
            {
                throw new BerException("Hết dữ liệu khi đang đọc độ dài BER dạng dài.");
            }

            length = (length << 8) | data[offset++];
        }

        return length;
    }

    internal static byte[] EncodeInteger(long value)
    {
        var bytes = new List<byte>();
        var remaining = value;

        // Cắt bớt byte thừa nhưng phải giữ đúng dấu: byte đầu tiên quyết định số âm hay dương.
        while (true)
        {
            bytes.Insert(0, (byte)(remaining & 0xFF));
            remaining >>= 8;

            if ((remaining == 0 && (bytes[0] & 0x80) == 0)
                || (remaining == -1 && (bytes[0] & 0x80) != 0))
            {
                break;
            }
        }

        return bytes.ToArray();
    }

    internal static byte[] EncodeOid(string oid)
    {
        var parts = oid.Split('.').Select(long.Parse).ToArray();

        if (parts.Length < 2)
        {
            throw new BerException($"Định danh đối tượng '{oid}' phải có ít nhất hai thành phần.");
        }

        var bytes = new List<byte> { (byte)(parts[0] * 40 + parts[1]) };

        foreach (var part in parts.Skip(2))
        {
            var group = new Stack<byte>();
            var value = part;

            group.Push((byte)(value & 0x7F));
            value >>= 7;

            while (value > 0)
            {
                group.Push((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }

            bytes.AddRange(group);
        }

        return bytes.ToArray();
    }

    internal static string DecodeOid(byte[] content)
    {
        if (content.Length == 0)
        {
            return string.Empty;
        }

        var parts = new List<long> { content[0] / 40, content[0] % 40 };
        long current = 0;

        foreach (var part in content.Skip(1))
        {
            current = (current << 7) | (uint)(part & 0x7F);

            if ((part & 0x80) == 0)
            {
                parts.Add(current);
                current = 0;
            }
        }

        return string.Join('.', parts);
    }
}

public class BerException : Exception
{
    public BerException(string message) : base(message) { }

    public BerException(string message, Exception inner) : base(message, inner) { }
}
