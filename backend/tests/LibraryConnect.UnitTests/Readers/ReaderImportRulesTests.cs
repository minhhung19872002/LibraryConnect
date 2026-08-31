using LibraryConnect.Application.Features.Readers;

namespace LibraryConnect.UnitTests.Readers;

/// <summary>
/// Các luật đọc dữ liệu bạn đọc từ tệp ngoài (VI.4).
///
/// Danh sách sinh viên do phòng đào tạo gửi sang không bao giờ sạch: mỗi người gõ ngày một kiểu, cột
/// đặt tên một kiểu. Đây là chỗ quyết định tệp đó có nhập được hay không.
/// </summary>
public class ReaderImportRulesTests
{
    [Theory]
    [InlineData("05/09/2005", 2005, 9, 5)]
    [InlineData("5/9/2005", 2005, 9, 5)]
    [InlineData("05-09-2005", 2005, 9, 5)]
    [InlineData("2005-09-05", 2005, 9, 5)]
    [InlineData("05.09.2005", 2005, 9, 5)]
    [InlineData("05092005", 2005, 9, 5)]
    public void Reads_the_date_formats_people_actually_type(string text, int year, int month, int day)
    {
        ReaderDateParser.TryParse(text, out var date).Should().BeTrue();
        date.Should().Be(new DateOnly(year, month, day));
    }

    [Fact]
    public void Reads_a_date_cell_that_excel_wrote_with_a_time_part()
    {
        // Ô định dạng Ngày của Excel thường ra chuỗi kèm 00:00:00 — không được coi đó là lỗi.
        ReaderDateParser.TryParse("2005-09-05 00:00:00", out var date).Should().BeTrue();
        date.Should().Be(new DateOnly(2005, 9, 5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("không rõ")]
    [InlineData("32/13/2005")]
    public void Refuses_what_it_cannot_read_instead_of_guessing(string text)
    {
        ReaderDateParser.TryParse(text, out _).Should().BeFalse();
    }

    [Fact]
    public void Falls_back_to_the_template_headers_when_no_mapping_is_given()
    {
        var options = new ReaderImportOptions();

        options.HeaderOf(ReaderImportFields.FullName).Should().Be("Họ và tên");
        options.HeaderOf(ReaderImportFields.StudentCode).Should().Be("Mã sinh viên");
    }

    [Fact]
    public void Uses_the_mapped_header_when_the_file_names_its_columns_differently()
    {
        var options = new ReaderImportOptions
        {
            Mapping = { [ReaderImportFields.FullName] = "HoTen", [ReaderImportFields.StudentCode] = "MSSV" }
        };

        options.HeaderOf(ReaderImportFields.FullName).Should().Be("HoTen");
        options.HeaderOf(ReaderImportFields.StudentCode).Should().Be("MSSV");

        // Trường không ánh xạ vẫn dùng tiêu đề mặc định, không được rơi về chuỗi rỗng.
        options.HeaderOf(ReaderImportFields.Email).Should().Be("Email");
    }

    [Fact]
    public void Every_import_field_has_a_header_and_a_description_for_the_template_sheet()
    {
        foreach (var field in ReaderImportFields.DefaultHeaders.Keys)
        {
            ReaderImportFields.Descriptions.Should().ContainKey(field);
            ReaderImportFields.DefaultHeaders[field].Should().NotBeNullOrWhiteSpace();
        }
    }
}
