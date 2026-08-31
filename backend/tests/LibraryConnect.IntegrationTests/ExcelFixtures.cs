using ClosedXML.Excel;

namespace LibraryConnect.IntegrationTests;

/// <summary>Bảng tính dựng sẵn cho các kịch bản nhập từ Excel.</summary>
public static class ExcelFixtures
{
    /// <summary>Cột của tệp mẫu đề nghị mua (III.1), đúng thứ tự và đúng tên tiêu đề.</summary>
    private static readonly string[] PurchaseRequestHeaders =
    {
        "Nhan đề", "Tác giả", "Nhà xuất bản", "Năm xuất bản", "ISBN",
        "ISSN", "Số lượng", "Đơn giá", "Nhà cung cấp", "Ghi chú"
    };

    /// <summary>
    /// Dựng một tệp đề nghị mua. Mỗi phần tử của <paramref name="rows"/> là một dòng, các ô theo
    /// đúng thứ tự cột của tệp mẫu; ô thiếu ở cuối dòng được hiểu là để trống.
    /// </summary>
    public static byte[] BuildPurchaseRequestSheet(IReadOnlyList<string[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Đề nghị mua");

        for (var index = 0; index < PurchaseRequestHeaders.Length; index++)
        {
            sheet.Cell(1, index + 1).Value = PurchaseRequestHeaders[index];
        }

        for (var index = 0; index < rows.Count; index++)
        {
            var cells = rows[index];

            for (var column = 0; column < cells.Length && column < PurchaseRequestHeaders.Length; column++)
            {
                // Ghi dưới dạng chuỗi để giữ nguyên cách gõ của người dùng — "180.000" phải đến được
                // bộ đọc đúng như trong tệp thật, chứ không bị Excel đổi thành số thập phân.
                sheet.Cell(index + 2, column + 1).SetValue(cells[column]);
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }
}
