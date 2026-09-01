using System.Globalization;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Marc;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>
/// Dựng trường 008 — bốn mươi ký tự mã hóa ngày tạo biểu ghi, năm xuất bản, nước và ngôn ngữ.
///
/// Trường này bắt buộc theo MARC 21 và bộ kiểm tra của hệ thống chặn mọi biểu ghi thiếu nó. Vì thế
/// mọi con đường sinh biểu ghi mới — trình soạn MARC, biên mục sơ lược ở màn hình bổ sung, nhập từ
/// tệp — đều phải đi qua đây; nếu không thì biểu ghi tạo ra ở đường này lại không lưu lại được ở
/// đường kia, và cán bộ biên mục phải mở trình hỗ trợ 008 cho từng cuốn một.
/// </summary>
public static class Marc008Builder
{
    public const int Length = 40;

    /// <summary>
    /// Bổ sung những vị trí còn trống của trường 008, giữ nguyên các vị trí cán bộ đã điền.
    /// </summary>
    /// <param name="publishYear">
    /// Năm xuất bản ghi vào vị trí 07–10. Bỏ trống thì lấy năm hiện tại — đúng quy ước với biểu ghi
    /// chưa rõ năm, và cán bộ sửa lại được ở trình soạn MARC.
    /// </param>
    public static async Task EnsureAsync(
        MarcRecord record,
        ISystemParameterService parameters,
        DateOnly today,
        int? publishYear = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(parameters);

        var value = (record.GetControlField("008") ?? string.Empty)
            .PadRight(Length, ' ')[..Length]
            .ToCharArray();

        Write(0, $"{today.Year % 100:00}{today.Month:00}{today.Day:00}");

        // 06 = kiểu ngày tháng; 's' nghĩa là một năm xuất bản duy nhất, đúng với đại đa số sách.
        if (value[6] == ' ')
        {
            value[6] = 's';
        }

        Write(7, (publishYear ?? today.Year).ToString(CultureInfo.InvariantCulture));

        var country = await parameters.GetAsync("CATALOG.DEFAULT_COUNTRY", "vm", ct);

        // Ngôn ngữ lấy theo chính biểu ghi trước, tham số hệ thống chỉ là phương án cuối. Hai chỗ
        // này mà lệch nhau thì cùng một biểu ghi nói hai thứ khác nhau về cùng một điều, và phần
        // mềm nhận biểu ghi tin chỗ nào cũng được.
        var language = record.GetSubfield("041", 'a')
            ?? await parameters.GetAsync("CATALOG.DEFAULT_LANGUAGE", "vie", ct);

        Write(15, country.PadRight(3)[..3]);
        Write(35, language.PadRight(3)[..3]);

        DienKyTuChuaMaHoa(value);

        // 39 = nguồn biên mục; 'd' là "cơ quan khác", giá trị đúng cho biểu ghi do thư viện tự tạo.
        if (value[39] == ' ')
        {
            value[39] = 'd';
        }

        record.SetControlField("008", new string(value));

        void Write(int start, string text)
        {
            for (var index = 0; index < text.Length && start + index < value.Length; index++)
            {
                if (value[start + index] == ' ')
                {
                    value[start + index] = text[index];
                }
            }
        }
    }

    /// <summary>
    /// Vị trí 18-34: nhóm riêng của tài liệu dạng sách. Chỗ nào còn trống thì điền ký tự `|`.
    ///
    /// `|` là ký tự điền của MARC 21, nghĩa là "không mã hóa vị trí này". Để khoảng trắng là một
    /// lời khai hẳn hoi chứ không phải im lặng: ở vị trí 24-27 khoảng trắng nghĩa là "không có nội
    /// dung đặc biệt nào", ở vị trí 28 nghĩa là "không phải xuất bản phẩm nhà nước", ở vị trí 33
    /// nghĩa là "không phải tác phẩm hư cấu. Dublin Core không nói gì về những điều ấy, nên khẳng
    /// định chúng là bịa.
    ///
    /// Hai ngoại lệ: vị trí 32 chuẩn chưa định nghĩa nên bắt buộc để trống, và vị trí 38 (biểu ghi
    /// có bị sửa vì hạn chế bảng mã không) thì khoảng trắng đúng là câu trả lời thật — biểu ghi lưu
    /// bằng UTF-8, không phải cắt bỏ ký tự nào.
    /// </summary>
    private static void DienKyTuChuaMaHoa(char[] value)
    {
        for (var index = 18; index <= 34; index++)
        {
            if (index == 32)
            {
                continue;
            }

            if (value[index] == ' ')
            {
                value[index] = '|';
            }
        }
    }
}
