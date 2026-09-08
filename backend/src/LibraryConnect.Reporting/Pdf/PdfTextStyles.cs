using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryConnect.Reporting.Pdf;

/// <summary>
/// Bộ chữ dùng chung cho mọi tệp PDF của sản phẩm — báo cáo, phích, thẻ bạn đọc, tem mã vạch,
/// nhãn gáy sách, biểu mẫu in.
///
/// Lato phủ đủ dấu tiếng Việt và được QuestPDF nhúng sẵn nên máy chủ không cần cài phông. Nhưng nó
/// có sẵn các ghép chữ (ligature) của OpenType, và HarfBuzz thay từng cặp chữ bằng **một glyph
/// duy nhất** — trong khi bảng ToUnicode mà QuestPDF ghi vào tệp không tra ngược được glyph ấy về
/// đúng hai chữ cái. Kết quả: trang in nhìn vẫn đúng, mà lớp chữ bên dưới thì sai.
///
/// Đo được: rút chữ từ chính tệp báo cáo xuất ra thì "thông tin" thành "thông ঞn", "tình trạng"
/// thành "টnh trạng", "office" thành "oﬃce" — Ctrl+F trong tệp không tìm ra, chép ra ngoài dán
/// thành chữ Bengali. Hồ sơ nộp thầu và tệp đi kèm quyết định đều là tệp PDF này.
///
/// Cách chữa: tắt ghép chữ. Chữ tiếng Việt không cần ligature nào, còn "fi"/"ff" rời nhau ra thì
/// mắt thường không phân biệt được ở cỡ chữ báo cáo.
/// </summary>
public static class PdfTextStyles
{
    /// <summary>Các nhóm ghép chữ của OpenType phải tắt, kể cả nhóm bật sẵn theo mặc định.</summary>
    private static readonly string[] Ligatures =
    {
        FontFeatures.StandardLigatures,   // liga — bật sẵn, chính là nhóm gây lỗi
        "clig",                           // ghép chữ theo ngữ cảnh
        "dlig",                           // ghép chữ tuỳ chọn
        "hlig",                           // ghép chữ kiểu cổ
        "rlig"                            // ghép chữ bắt buộc của một số hệ chữ khác
    };

    /// <summary>Phông chuẩn của mọi trang in, đã tắt ghép chữ.</summary>
    public static TextStyle Base(this TextStyle style)
    {
        var result = style.FontFamily(Fonts.Lato);

        foreach (var feature in Ligatures)
        {
            result = result.DisableFontFeature(feature);
        }

        return result;
    }
}
