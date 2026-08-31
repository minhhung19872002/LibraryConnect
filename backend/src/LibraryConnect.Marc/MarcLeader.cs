using System.Text;

namespace LibraryConnect.Marc;

/// <summary>
/// Đầu biểu (Leader) của biểu ghi MARC 21: đúng 24 ký tự.
///
/// The leader is stored as a fixed 24-character buffer and each meaningful position is exposed as a
/// named property, because cataloguers refer to these by position ("Leader/06") while the software
/// needs to read them by meaning. Positions the standard fills in automatically — record length
/// (0–4) and base address (12–16) — are recomputed by the ISO 2709 writer, so whatever is stored
/// there is ignored on output.
/// </summary>
public class MarcLeader
{
    public const int Length = 24;

    /// <summary>
    /// Leader mặc định cho một biểu ghi mới: chưa xác định trạng thái, tài liệu chữ in, biểu ghi
    /// đơn lẻ, mô tả theo ISBD đầy đủ.
    /// </summary>
    public const string Default = "00000nam a2200000 a 4500";

    private readonly char[] _value;

    public MarcLeader() : this(Default) { }

    public MarcLeader(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            value = Default;
        }

        // A leader shorter than 24 characters is padded rather than rejected: records from older
        // systems are sometimes truncated, and losing the whole record over it helps nobody.
        _value = value.Length >= Length
            ? value[..Length].ToCharArray()
            : value.PadRight(Length, ' ').ToCharArray();
    }

    public char this[int position]
    {
        get => _value[position];
        set => _value[position] = value;
    }

    /// <summary>Vị trí 05 — Trạng thái biểu ghi: n mới, c đã sửa, d đã xóa, a nâng cấp.</summary>
    public char RecordStatus
    {
        get => _value[5];
        set => _value[5] = value;
    }

    /// <summary>Vị trí 06 — Loại hình biểu ghi: a tài liệu chữ in, e bản đồ, i ghi âm, g tài liệu nhìn…</summary>
    public char RecordType
    {
        get => _value[6];
        set => _value[6] = value;
    }

    /// <summary>Vị trí 07 — Cấp thư mục: m chuyên khảo, s xuất bản phẩm nhiều kỳ, a phần của chuyên khảo…</summary>
    public char BibliographicLevel
    {
        get => _value[7];
        set => _value[7] = value;
    }

    /// <summary>Vị trí 08 — Loại hình kiểm soát: khoảng trắng nếu không phải tài liệu lưu trữ.</summary>
    public char TypeOfControl
    {
        get => _value[8];
        set => _value[8] = value;
    }

    /// <summary>
    /// Vị trí 09 — Sơ đồ mã hóa ký tự. LibraryConnect luôn ghi 'a' (Unicode/UTF-8) vì toàn hệ thống
    /// dùng UTF-8; giá trị khoảng trắng của MARC-8 chỉ gặp khi đọc dữ liệu cũ.
    /// </summary>
    public char CharacterCodingScheme
    {
        get => _value[9];
        set => _value[9] = value;
    }

    /// <summary>Vị trí 17 — Mức độ đầy đủ của biểu ghi: khoảng trắng đầy đủ, 1 đầy đủ chưa kiểm, 7 tối thiểu…</summary>
    public char EncodingLevel
    {
        get => _value[17];
        set => _value[17] = value;
    }

    /// <summary>Vị trí 18 — Quy tắc mô tả: a AACR2, i ISBD, c ISBD rút gọn, n không theo ISBD.</summary>
    public char DescriptiveCatalogingForm
    {
        get => _value[18];
        set => _value[18] = value;
    }

    /// <summary>Vị trí 19 — Mức độ liên kết của biểu ghi nhiều phần.</summary>
    public char MultipartResourceRecordLevel
    {
        get => _value[19];
        set => _value[19] = value;
    }

    /// <summary>Vị trí 00–04 — Tổng độ dài biểu ghi tính bằng byte. Do bộ ghi ISO 2709 tự tính.</summary>
    public int RecordLength => ReadNumber(0, 5);

    /// <summary>Vị trí 12–16 — Địa chỉ bắt đầu vùng dữ liệu. Do bộ ghi ISO 2709 tự tính.</summary>
    public int BaseAddressOfData => ReadNumber(12, 5);

    /// <summary>Biểu ghi đã bị đánh dấu xóa ở nguồn (Leader/05 = d).</summary>
    public bool IsDeleted => RecordStatus == 'd';

    internal void SetRecordLength(int value) => WriteNumber(0, 5, value);

    internal void SetBaseAddressOfData(int value) => WriteNumber(12, 5, value);

    private int ReadNumber(int start, int length) =>
        int.TryParse(new string(_value, start, length), out var number) ? number : 0;

    private void WriteNumber(int start, int length, int value)
    {
        // Five digits cap the record at 99,999 bytes, which is the standard's own limit.
        var text = value.ToString().PadLeft(length, '0');

        if (text.Length > length)
        {
            text = new string('9', length);
        }

        for (var index = 0; index < length; index++)
        {
            _value[start + index] = text[index];
        }
    }

    public override string ToString() => new(_value);

    /// <summary>Tạo bản sao độc lập.</summary>
    public MarcLeader Clone() => new(ToString());

    /// <summary>Số byte UTF-8 của đầu biểu — luôn là 24 vì mọi vị trí đều là ký tự ASCII.</summary>
    internal int ByteLength => Encoding.UTF8.GetByteCount(ToString());
}
