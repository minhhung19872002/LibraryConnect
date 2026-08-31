using ClosedXML.Excel;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Reporting.Excel;

namespace LibraryConnect.UnitTests.Admin;

/// <summary>
/// The Excel service carries every import and export in the product, and the files it reads are
/// edited by hand, so its tolerance for messy input is a feature rather than an accident.
/// </summary>
public class ExcelServiceTests
{
    private readonly ExcelService _service = new();

    private static MemoryStream BuildWorkbook(Action<IXLWorksheet> build, string sheetName = "Sheet1")
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(sheetName);
        build(sheet);

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public void Reads_rows_addressed_by_header()
    {
        using var stream = BuildWorkbook(sheet =>
        {
            sheet.Cell(1, 1).Value = "Tên đăng nhập";
            sheet.Cell(1, 2).Value = "Họ và tên";
            sheet.Cell(2, 1).Value = "nguyenvana";
            sheet.Cell(2, 2).Value = "Nguyễn Văn A";
            sheet.Cell(3, 1).Value = "tranthib";
            sheet.Cell(3, 2).Value = "Trần Thị B";
        });

        var result = _service.Read(stream);

        result.Rows.Should().HaveCount(2);
        result.Rows[0].Get("Họ và tên").Should().Be("Nguyễn Văn A");
        result.Rows[1].Get("Tên đăng nhập").Should().Be("tranthib");
    }

    [Fact]
    public void Header_matching_ignores_case_and_surrounding_spaces()
    {
        using var stream = BuildWorkbook(sheet =>
        {
            sheet.Cell(1, 1).Value = "  Tên đăng nhập  ";
            sheet.Cell(2, 1).Value = "nguyenvana";
        });

        var result = _service.Read(stream);

        result.Headers.Should().Contain("Tên đăng nhập");
        result.Rows[0].Get("TÊN ĐĂNG NHẬP").Should().Be("nguyenvana");
    }

    [Fact]
    public void Cell_values_are_trimmed()
    {
        using var stream = BuildWorkbook(sheet =>
        {
            sheet.Cell(1, 1).Value = "Email";
            sheet.Cell(2, 1).Value = "  a@b.vn ";
        });

        _service.Read(stream).Rows[0].Get("Email").Should().Be("a@b.vn");
    }

    [Fact]
    public void Blank_rows_are_skipped()
    {
        using var stream = BuildWorkbook(sheet =>
        {
            sheet.Cell(1, 1).Value = "Mã";
            sheet.Cell(2, 1).Value = "A1";
            // Row 3 deliberately left empty, as happens when a user deletes a line's content.
            sheet.Cell(4, 1).Value = "A2";
        });

        var rows = _service.Read(stream).Rows;

        rows.Should().HaveCount(2);
        rows.Select(r => r.Get("Mã")).Should().Equal("A1", "A2");
    }

    [Fact]
    public void Row_numbers_match_the_sheet_so_errors_point_at_the_right_line()
    {
        using var stream = BuildWorkbook(sheet =>
        {
            sheet.Cell(1, 1).Value = "Mã";
            sheet.Cell(2, 1).Value = "A1";
            sheet.Cell(3, 1).Value = "A2";
        });

        _service.Read(stream).Rows.Select(r => r.RowNumber).Should().Equal(2, 3);
    }

    [Fact]
    public void A_missing_column_reads_as_an_empty_string_rather_than_throwing()
    {
        using var stream = BuildWorkbook(sheet =>
        {
            sheet.Cell(1, 1).Value = "Mã";
            sheet.Cell(2, 1).Value = "A1";
        });

        _service.Read(stream).Rows[0].Get("Cột không tồn tại").Should().BeEmpty();
    }

    [Fact]
    public void An_empty_sheet_yields_no_rows()
    {
        using var stream = BuildWorkbook(_ => { });

        var result = _service.Read(stream);

        result.Rows.Should().BeEmpty();
        result.Headers.Should().BeEmpty();
    }

    [Fact]
    public void Written_workbook_round_trips_vietnamese_text()
    {
        var columns = new List<ExcelColumn<(string Name, int Count)>>
        {
            new("Tên tài liệu", r => r.Name),
            new("Số lượng", r => r.Count)
        };

        var rows = new List<(string, int)> { ("Giáo trình Cơ sở dữ liệu", 12), ("Từ điển Việt–Anh", 3) };

        // No title: this is the shape used for data exchange, where the header is the first row and
        // the file can be read back in.
        var content = _service.Write("Báo cáo", columns, rows);

        using var stream = new MemoryStream(content);
        var reread = _service.Read(stream);

        reread.Rows.Should().HaveCount(2);
        reread.Rows.Select(r => r.Get("Tên tài liệu"))
            .Should().Equal("Giáo trình Cơ sở dữ liệu", "Từ điển Việt–Anh");
        reread.Rows[0].Get("Số lượng").Should().Be("12");
    }

    [Fact]
    public void A_titled_export_puts_the_header_below_the_title_block()
    {
        var columns = new List<ExcelColumn<(string Name, int Count)>>
        {
            new("Tên tài liệu", r => r.Name),
            new("Số lượng", r => r.Count)
        };

        var content = _service.Write("Báo cáo", columns, new[] { ("Giáo trình Cơ sở dữ liệu", 12) },
            "THỐNG KÊ TÀI LIỆU");

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();

        // Row 1 title, row 2 export timestamp, row 4 header, row 5 first data row. A titled report is
        // meant for a human reader, not for re-import, which is why Read is not used here.
        sheet.Cell(1, 1).GetString().Should().Be("THỐNG KÊ TÀI LIỆU");
        sheet.Cell(2, 1).GetString().Should().StartWith("Ngày xuất:");
        sheet.Cell(4, 1).GetString().Should().Be("Tên tài liệu");
        sheet.Cell(5, 1).GetString().Should().Be("Giáo trình Cơ sở dữ liệu");
    }

    [Fact]
    public void Template_has_a_guidance_sheet_next_to_the_data_sheet()
    {
        var columns = new List<ExcelTemplateColumn>
        {
            new("Tên đăng nhập", "Bắt buộc", Required: true, Example: "nguyenvana"),
            new("Email", "Tùy chọn")
        };

        var content = _service.WriteTemplate("Người dùng", columns);

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);

        workbook.Worksheets.Select(w => w.Name).Should().Contain(new[] { "Người dùng", "Hướng dẫn" });
        workbook.Worksheet("Hướng dẫn").Cell(2, 2).GetString().Should().Be("Có");
        workbook.Worksheet("Hướng dẫn").Cell(3, 2).GetString().Should().Be("Không");
    }

    [Fact]
    public void Sheet_name_is_trimmed_to_what_excel_accepts()
    {
        var columns = new List<ExcelColumn<int>> { new("Giá trị", value => value) };

        // Excel rejects names over 31 characters and the characters : \ / ? * [ ]
        var content = _service.Write("Báo cáo/thống kê rất dài vượt quá giới hạn của Excel", columns, new[] { 1 });

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);

        workbook.Worksheets.First().Name.Length.Should().BeLessThanOrEqualTo(31);
        workbook.Worksheets.First().Name.Should().NotContain("/");
    }
}
