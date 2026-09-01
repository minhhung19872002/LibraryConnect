using System.Text;

namespace LibraryConnect.Marc;

/// <summary>
/// Chia trường dài quá giới hạn của ISO 2709 thành nhiều lần lặp.
///
/// Danh mục của ISO 2709 khai độ dài mỗi trường bằng đúng bốn chữ số, nên một trường không thể quá
/// 9.999 byte. Đây là giới hạn thật của định dạng chứ không phải lỗi — nhưng nó gặp thường xuyên với
/// biểu ghi thu hoạch về, vì phần tóm tắt của luận án hay bài giảng dài hàng chục nghìn ký tự.
///
/// Cách chuẩn quy định là chia nội dung thành nhiều lần lặp của cùng một trường. Chỉ làm được với
/// trường **lặp được**; trường không lặp được mà quá dài thì biểu ghi ấy không biểu diễn nổi bằng
/// ISO 2709, phải nói thẳng chứ không cắt bớt lén.
/// </summary>
public static class Iso2709FieldSplitter
{
    /// <summary>
    /// Trường MARC 21 **không** lặp được, theo bản đặc tả của Thư viện Quốc hội Mỹ.
    ///
    /// Chỉ liệt kê nhóm hay gặp trong biểu ghi thư mục; trường nào không có trong danh sách này thì
    /// chuẩn cho lặp, nên chia được.
    /// </summary>
    private static readonly HashSet<string> KhongLapDuoc = new(StringComparer.Ordinal)
    {
        "001", "003", "005", "008", "010", "018", "036", "038", "040", "042", "043", "044",
        "045", "066", "100", "110", "111", "130", "240", "243", "245", "254", "256", "263",
        "306", "357", "384", "507", "514", "841", "842", "843", "844", "882", "883", "884", "885",
    };

    /// <summary>Chừa chỗ cho indicator, mã trường con và dấu kết thúc trường.</summary>
    private const int ChuaCho = 32;

    /// <summary>Vì sao một biểu ghi không xuất được ra ISO 2709.</summary>
    public record BoQua(string? ControlNumber, string? Title, string Reason);

    /// <summary>
    /// Trả về bản sao của biểu ghi đã chia nhỏ những trường quá dài, hoặc <c>null</c> kèm lý do khi
    /// biểu ghi không biểu diễn nổi bằng ISO 2709.
    /// </summary>
    public static (MarcRecord? Record, BoQua? Skipped) Prepare(MarcRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.DataFields.All(field => DoDai(field) <= MarcConstants.MaxFieldLength))
        {
            return (record, null);
        }

        var qua = record.DataFields.FirstOrDefault(field =>
            DoDai(field) > MarcConstants.MaxFieldLength && KhongLapDuoc.Contains(field.Tag));

        if (qua is not null)
        {
            return (null, new BoQua(
                record.ControlNumber,
                record.GetSubfield("245", 'a'),
                $"Trường {qua.Tag} dài {MarcText.Number(DoDai(qua))} byte, vượt giới hạn "
                + $"{MarcText.Number(MarcConstants.MaxFieldLength)} byte của ISO 2709, mà trường này "
                + "không lặp được nên không chia ra được. Hãy rút ngắn nội dung, hoặc xuất biểu ghi "
                + "này bằng MARCXML — định dạng ấy không có giới hạn độ dài trường."));
        }

        var ban = MarcJson.Deserialize(MarcJson.Serialize(record));
        var moi = new List<MarcDataField>();

        foreach (var field in ban.DataFields)
        {
            if (DoDai(field) <= MarcConstants.MaxFieldLength)
            {
                moi.Add(field);
                continue;
            }

            moi.AddRange(Chia(field));
        }

        ban.DataFields.Clear();
        ban.DataFields.AddRange(moi);

        return (ban, null);
    }

    /// <summary>Chia một trường thành nhiều lần lặp, mỗi lần vừa trong giới hạn.</summary>
    private static IEnumerable<MarcDataField> Chia(MarcDataField field)
    {
        var ket = new List<MarcDataField>();
        var hienTai = MoiTruong(field);
        var daDung = 0;

        foreach (var subfield in field.Subfields)
        {
            foreach (var manh in ChiaGiaTri(subfield.Value))
            {
                var soByte = Encoding.UTF8.GetByteCount(manh) + 2;

                // Trường đang dựng không còn chỗ thì chốt lại và mở một lần lặp mới.
                if (daDung > 0 && daDung + soByte > MarcConstants.MaxFieldLength - ChuaCho)
                {
                    ket.Add(hienTai);
                    hienTai = MoiTruong(field);
                    daDung = 0;
                }

                hienTai.AddSubfield(subfield.Code, manh);
                daDung += soByte;
            }
        }

        if (hienTai.Subfields.Count > 0)
        {
            ket.Add(hienTai);
        }

        return ket;
    }

    private static MarcDataField MoiTruong(MarcDataField goc) =>
        new() { Tag = goc.Tag, Indicator1 = goc.Indicator1, Indicator2 = goc.Indicator2 };

    /// <summary>
    /// Cắt một chuỗi thành từng mảnh vừa giới hạn, luôn cắt ở chỗ giáp từ.
    ///
    /// Cắt theo byte thì rơi vào giữa một chữ tiếng Việt — "nước" chiếm 6 byte — và bên nhận đọc ra
    /// ký tự hỏng. Đếm theo byte nhưng cắt theo khoảng trắng thì vừa đúng giới hạn vừa đọc được.
    /// </summary>
    private static IEnumerable<string> ChiaGiaTri(string value)
    {
        var gioiHan = MarcConstants.MaxFieldLength - ChuaCho;

        if (Encoding.UTF8.GetByteCount(value) <= gioiHan)
        {
            yield return value;
            yield break;
        }

        var builder = new StringBuilder();
        var soByte = 0;

        foreach (var tu in TachTu(value))
        {
            var cua = Encoding.UTF8.GetByteCount(tu);

            if (soByte > 0 && soByte + cua > gioiHan)
            {
                yield return builder.ToString().TrimEnd();
                builder.Clear();
                soByte = 0;
            }

            builder.Append(tu);
            soByte += cua;
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString().TrimEnd();
        }
    }

    /// <summary>Tách chuỗi thành từng từ, giữ nguyên khoảng trắng đứng sau mỗi từ.</summary>
    private static IEnumerable<string> TachTu(string value)
    {
        var start = 0;

        for (var index = 0; index < value.Length; index++)
        {
            if (!char.IsWhiteSpace(value[index]))
            {
                continue;
            }

            while (index + 1 < value.Length && char.IsWhiteSpace(value[index + 1]))
            {
                index++;
            }

            yield return value[start..(index + 1)];
            start = index + 1;
        }

        if (start < value.Length)
        {
            yield return value[start..];
        }
    }

    /// <summary>Số byte một trường dữ liệu chiếm trong tệp ISO 2709.</summary>
    private static int DoDai(MarcDataField field) =>
        2 + field.Subfields.Sum(sub => 2 + Encoding.UTF8.GetByteCount(sub.Value)) + 1;
}
