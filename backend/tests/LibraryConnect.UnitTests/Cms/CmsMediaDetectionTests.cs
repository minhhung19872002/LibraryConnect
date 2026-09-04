using System.IO.Compression;
using System.Text;
using FluentAssertions;
using LibraryConnect.Application.Features.Cms;

namespace LibraryConnect.UnitTests.Cms;

/// <summary>
/// Nhận dạng tệp nội dung bằng chữ ký nhị phân (VIII.1, yêu cầu 6.4).
///
/// Word và Excel cùng là gói ZIP, nên một tệp ZIP thường hay một tệp đổi đuôi phải bị từ chối chứ
/// không được nhận nhầm — chỗ này chỉ nhìn bốn byte đầu là sai.
/// </summary>
public class CmsMediaDetectionTests
{
    [Fact]
    public void Pdf_nhan_ra_bang_chu_ky_dau_tep_khong_can_duoi()
    {
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%âãÏÓ\n1 0 obj");

        CmsMedia.DetectDocumentType(pdf).Should().Be(CmsMedia.Pdf);
        CmsMedia.DetectFileType(pdf).Should().Be(CmsMedia.Pdf);
        CmsMedia.IsImage(CmsMedia.Pdf).Should().BeFalse();
    }

    [Fact]
    public void Docx_va_xlsx_phan_biet_bang_thu_muc_ben_trong_goi()
    {
        CmsMedia.DetectDocumentType(Package("word/document.xml")).Should().Be(CmsMedia.Docx);
        CmsMedia.DetectDocumentType(Package("xl/workbook.xml")).Should().Be(CmsMedia.Xlsx);
    }

    [Fact]
    public void Zip_thuong_hay_tep_doi_duoi_thi_bi_tu_choi()
    {
        CmsMedia.DetectDocumentType(Package("ghi-chu.txt")).Should().BeNull();
        CmsMedia.DetectDocumentType(Encoding.ASCII.GetBytes("PK\x03\x04 rac")).Should().BeNull();
        CmsMedia.DetectDocumentType(new byte[] { 0x4D, 0x5A, 0x90, 0x00 }).Should().BeNull("tệp thực thi không bao giờ được nhận");
        CmsMedia.DetectFileType(Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'/>")).Should().BeNull("SVG chứa được script");
    }

    [Fact]
    public void Ten_doi_tuong_cua_tep_dinh_kem_mang_dung_duoi_va_kieu_suy_nguoc_lai_duoc()
    {
        var name = CmsMedia.ObjectName("file", "Quyết định số 12.docx", CmsMedia.Docx);

        name.Should().StartWith("cms/file/quyet-dinh-so-12-").And.EndWith(".docx");
        CmsMedia.ContentTypeOf(name).Should().Be(CmsMedia.Docx);
        CmsMedia.ContentTypeOf("cms/file/a.pdf").Should().Be(CmsMedia.Pdf);
        CmsMedia.ContentTypeOf("cms/file/a.xlsx").Should().Be(CmsMedia.Xlsx);
        CmsMedia.ContentTypeOf("cms/news/a.webp").Should().Be("image/webp");
    }

    private static byte[] Package(string entryName)
    {
        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var content = archive.CreateEntry("[Content_Types].xml").Open())
            {
                content.Write(Encoding.UTF8.GetBytes("<Types/>"));
            }

            using var entry = archive.CreateEntry(entryName).Open();
            entry.Write(Encoding.UTF8.GetBytes("<x/>"));
        }

        return buffer.ToArray();
    }
}
