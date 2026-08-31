using System.Text;
using FluentAssertions;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Application.Features.Digital;
using LibraryConnect.Domain.Entities.Dig;

namespace LibraryConnect.UnitTests.Digital;

/// <summary>
/// Phân hệ V — những quy tắc không cần cơ sở dữ liệu: nhận dạng định dạng tệp, làm sạch văn bản
/// rút từ tệp, quy ước tên đối tượng trong kho và trạng thái phiên tải theo mảnh.
/// </summary>
public class DigitalFileTypeTests
{
    [Fact]
    public void Nhan_dang_PDF_bang_chu_ky_nhi_phan()
    {
        var content = Encoding.ASCII.GetBytes("%PDF-1.7\n...");

        DigitalStorage.DetectMimeType(content, "bat-ky.txt").Should().Be("application/pdf");
    }

    [Fact]
    public void Doi_duoi_tep_khong_qua_mat_duoc_he_thong()
    {
        // Một tệp thực thi đổi tên thành .pdf vẫn phải bị từ chối (yêu cầu 6.4).
        var content = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03 };

        DigitalStorage.DetectMimeType(content, "giao-trinh.pdf").Should().BeNull();
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "image/jpeg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image/png")]
    public void Nhan_dang_dung_cac_dinh_dang_anh(byte[] signature, string expected)
    {
        DigitalStorage.DetectMimeType(signature, "anh").Should().Be(expected);
    }

    [Fact]
    public void Phan_biet_cac_dinh_dang_cung_vo_zip_bang_phan_mo_rong()
    {
        var zip = Encoding.ASCII.GetBytes("PK\u0003\u0004rest");

        DigitalStorage.DetectMimeType(zip, "luan-van.docx")
            .Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        DigitalStorage.DetectMimeType(zip, "sach.epub").Should().Be("application/epub+zip");
        DigitalStorage.DetectMimeType(zip, "khong-ro.abc").Should().BeNull();
    }

    [Fact]
    public void Tep_rong_khong_nhan_dang_duoc()
    {
        DigitalStorage.DetectMimeType(Array.Empty<byte>(), "rong.pdf").Should().BeNull();
    }

    [Theory]
    [InlineData("application/pdf", "PDF")]
    [InlineData("video/mp4", "VIDEO")]
    [InlineData("audio/mpeg", "AUDIO")]
    [InlineData("image/png", "IMAGE")]
    [InlineData("application/epub+zip", "EPUB")]
    [InlineData("application/msword", "OFFICE")]
    [InlineData("application/x-la-gi-do", "OTHER")]
    public void Xep_dung_nhom_dinh_dang(string mimeType, string expected)
    {
        DigitalStorage.FormatGroup(mimeType).Should().Be(expected);
    }

    [Fact]
    public void Ten_doi_tuong_giu_nguyen_phan_mo_rong_va_khong_lo_ten_tep_goc()
    {
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var name = DigitalStorage.OriginalObject(id, "Giáo trình CSDL.PDF");

        name.Should().Be("documents/11111111222233334444555555555555/goc.pdf");
    }

    [Fact]
    public void Ten_manh_tep_sap_xep_dung_thu_tu_theo_chuoi()
    {
        var id = Guid.NewGuid();

        // Đánh số có đệm 0: ghép mảnh theo thứ tự tên là ra đúng thứ tự byte, không bị 10 đứng
        // trước 2 như khi so chuỗi thông thường.
        var second = DigitalStorage.ChunkObject(id, 2);
        var tenth = DigitalStorage.ChunkObject(id, 10);

        string.CompareOrdinal(second, tenth).Should().BeLessThan(0);
    }

    [Fact]
    public void Ma_kiem_tra_la_SHA256_dang_chu_thuong()
    {
        var checksum = DigitalStorage.Sha256(Encoding.UTF8.GetBytes("LibraryConnect"));

        checksum.Should().HaveLength(64);
        checksum.Should().MatchRegex("^[0-9a-f]{64}$");
    }
}

public class PlainTextTests
{
    [Fact]
    public void Bo_ky_tu_rong_ma_PostgreSQL_khong_luu_duoc()
    {
        var text = "Giáo trình \0Cơ sở dữ liệu";

        PlainText.RemoveUnstorableCharacters(text).Should().Be("Giáo trình Cơ sở dữ liệu");
    }

    [Fact]
    public void Giu_nguyen_dau_tieng_Viet_va_bo_cuc_dong()
    {
        var text = "Chương 1\nMục 1.1\tTổng quan";

        PlainText.RemoveUnstorableCharacters(text).Should().Be("Chương 1\nMục 1.1\tTổng quan");
    }

    [Fact]
    public void Bo_cac_ky_tu_dieu_khien_khac()
    {
        var text = "\u0001Nhan đề\u0007 sách\u001f";

        PlainText.RemoveUnstorableCharacters(text).Should().Be("Nhan đề sách");
    }

    [Fact]
    public void Chuoi_rong_va_null_tra_ve_chuoi_rong()
    {
        PlainText.RemoveUnstorableCharacters(null).Should().BeEmpty();
        PlainText.RemoveUnstorableCharacters(string.Empty).Should().BeEmpty();
        PlainText.RemoveUnstorableCharacters("   ").Should().BeEmpty();
    }
}

public class DigitalUploadSessionTests
{
    private static DigitalUploadSession NewSession(int totalChunks) => new()
    {
        FileName = "giao-trinh.pdf",
        TotalSize = totalChunks * 1024L,
        ChunkSize = 1024,
        TotalChunks = totalChunks,
    };

    [Fact]
    public void Phien_moi_mo_thi_chua_nhan_manh_nao()
    {
        var session = NewSession(5);

        session.ReceivedList().Should().BeEmpty();
        session.HasAllChunks().Should().BeFalse();
    }

    [Fact]
    public void Ghi_nhan_manh_theo_thu_tu_tang_dan_du_gui_lung_tung()
    {
        var session = NewSession(4);

        session.MarkReceived(3);
        session.MarkReceived(0);
        session.MarkReceived(2);

        session.ReceivedList().Should().Equal(0, 2, 3);
    }

    [Fact]
    public void Gui_lai_mot_manh_da_co_khong_lam_dem_hai_lan()
    {
        var session = NewSession(3);

        session.MarkReceived(1);
        session.MarkReceived(1);

        // Mạng chập chờn thì cùng một mảnh bị gửi lại là chuyện thường; đếm trùng sẽ khiến hệ thống
        // tưởng đã đủ mảnh và ghép ra tệp hỏng.
        session.ReceivedList().Should().Equal(1);
        session.HasAllChunks().Should().BeFalse();
    }

    [Fact]
    public void Nhan_du_manh_thi_bao_da_xong()
    {
        var session = NewSession(3);

        session.MarkReceived(0);
        session.MarkReceived(1);
        session.MarkReceived(2);

        session.HasAllChunks().Should().BeTrue();
    }
}
