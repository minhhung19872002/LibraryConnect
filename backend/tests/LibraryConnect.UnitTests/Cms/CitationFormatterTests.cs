using FluentAssertions;
using LibraryConnect.Application.Features.Opac;

namespace LibraryConnect.UnitTests.Cms;

/// <summary>
/// Xuất trích dẫn (IX.2).
///
/// Ba kiểu đầu là chữ sinh viên chép thẳng vào khóa luận, ba kiểu sau là tệp nạp vào phần mềm quản
/// lý tài liệu tham khảo. Kiểu tệp mà sai một dấu là phần mềm bên kia từ chối cả tệp, nên phần thử
/// tập trung vào những chỗ dễ sai: dấu ngăn tác giả, ký tự điều khiển, trường bỏ trống.
/// </summary>
public class CitationFormatterTests
{
    private static CitationSource Book(
        string? publisher = "Nhà xuất bản Giáo dục",
        int? year = 2023,
        params string[] authors) =>
        new(
            "Giáo trình cơ sở dữ liệu",
            "Dành cho sinh viên công nghệ thông tin",
            authors.Length == 0 ? new[] { "Nguyễn Văn A" } : authors,
            publisher,
            "Hà Nội",
            year,
            "Tái bản lần 2",
            "978-604-0-12345-6",
            "320 tr.",
            "Sách",
            "LC00000123");

    [Fact]
    public void APA_dat_nam_trong_ngoac_ngay_sau_ten_tac_gia()
    {
        var citation = CitationFormatter.Format(Book(), CitationStyle.Apa);

        citation.Should().StartWith("Nguyễn Văn A. (2023).");
        citation.Should().Contain("Giáo trình cơ sở dữ liệu: Dành cho sinh viên công nghệ thông tin");
        citation.Should().EndWith("Nhà xuất bản Giáo dục.");
    }

    [Fact]
    public void Khong_co_nam_thi_ghi_n_d_chu_khong_de_trong()
    {
        var citation = CitationFormatter.Format(Book(year: null), CitationStyle.Apa);

        citation.Should().Contain("(n.d.)");
    }

    [Fact]
    public void Chicago_ghi_noi_xuat_ban_truoc_nha_xuat_ban()
    {
        var citation = CitationFormatter.Format(Book(), CitationStyle.Chicago);

        citation.Should().Contain("Hà Nội: Nhà xuất bản Giáo dục, 2023.");
    }

    [Fact]
    public void BibTeX_ngan_cac_tac_gia_bang_tu_and()
    {
        // Dấu phẩy trong BibTeX là ranh giới giữa họ và tên của cùng một người; dùng dấu phẩy để
        // ngăn hai tác giả là biến hai người thành một.
        var citation = CitationFormatter.Format(
            Book(authors: new[] { "Nguyễn Văn A", "Trần Thị B" }), CitationStyle.BibTex);

        citation.Should().Contain("author = {Nguyễn Văn A and Trần Thị B}");
        citation.Should().StartWith("@book{");
        citation.Should().Contain("year = {2023}");
    }

    [Fact]
    public void BibTeX_bo_dau_ngoac_nhon_trong_du_lieu()
    {
        var source = Book(publisher: "Nhà xuất bản {Giáo dục}");
        var citation = CitationFormatter.Format(source, CitationStyle.BibTex);

        // Ngoặc nhọn là ký tự điều khiển của BibTeX; để nguyên là hỏng cả tệp từ dòng đó trở đi.
        citation.Should().Contain("publisher = {Nhà xuất bản Giáo dục}");
    }

    [Fact]
    public void RIS_mo_bang_TY_va_dong_bang_ER()
    {
        var citation = CitationFormatter.Format(
            Book(authors: new[] { "Nguyễn Văn A", "Trần Thị B" }), CitationStyle.Ris);

        citation.Should().StartWith("TY  - BOOK");
        citation.Should().Contain("AU  - Nguyễn Văn A");
        citation.Should().Contain("AU  - Trần Thị B");
        citation.TrimEnd().Should().EndWith("ER  -");
    }

    [Fact]
    public void EndNote_dung_ma_truong_bat_dau_bang_dau_phan_tram()
    {
        var citation = CitationFormatter.Format(Book(), CitationStyle.EndNote);

        citation.Should().StartWith("%0 Book");
        citation.Should().Contain("%A Nguyễn Văn A");
        citation.Should().Contain("%D 2023");
    }

    [Fact]
    public void Thieu_nha_xuat_ban_thi_khong_de_lai_dau_cham_thua()
    {
        var citation = CitationFormatter.Format(Book(publisher: null), CitationStyle.Apa);

        citation.Should().NotContain("..");
        citation.Should().NotContain(" .");
    }
}
