using LibraryConnect.Application.Features.Circulation;

namespace LibraryConnect.UnitTests.Circulation;

/// <summary>Mã QR trạm và phiếu xác thực vị trí (Phase 15, mục 3.2) chỉ có giá trị khi đúng khoá.</summary>
public class SelfCheckoutSignerTests
{
    private const string Secret = "khoa-bi-mat-cua-thu-vien";

    [Fact]
    public void Ma_QR_tram_doc_lai_duoc_bang_dung_khoa()
    {
        var qr = SelfCheckoutSigner.BuildStationQr("khomo-01", Secret);

        qr.Should().StartWith("LCST1|KHOMO-01|");
        SelfCheckoutSigner.ReadStationQr(qr, Secret).Should().Be("KHOMO-01");
    }

    [Fact]
    public void Ma_QR_ky_bang_khoa_khac_hoac_bi_sua_thi_khong_doc_duoc()
    {
        var qr = SelfCheckoutSigner.BuildStationQr("KHOMO-01", Secret);

        SelfCheckoutSigner.ReadStationQr(qr, "khoa-khac").Should().BeNull("mã của thư viện khác không dùng được");
        SelfCheckoutSigner.ReadStationQr(qr.Replace("KHOMO-01", "KHOMO-02"), Secret).Should().BeNull("đổi mã trạm là hỏng chữ ký");
        SelfCheckoutSigner.ReadStationQr("LCST1|KHOMO-01", Secret).Should().BeNull();
        SelfCheckoutSigner.ReadStationQr("https://example.com", Secret).Should().BeNull("QR bất kỳ không phải mã trạm");
        SelfCheckoutSigner.ReadStationQr(null, Secret).Should().BeNull();
    }

    [Fact]
    public void Phieu_xac_thuc_mang_dung_ban_doc_che_do_va_han()
    {
        var readerId = Guid.NewGuid();
        var payload = new SelfCheckoutSigner.TokenPayload(readerId, "QR_STATION", "KHOMO-01", 1_800_000_000);

        var token = SelfCheckoutSigner.IssueToken(payload, Secret);
        var read = SelfCheckoutSigner.ReadToken(token, Secret);

        read.Should().NotBeNull();
        read!.ReaderId.Should().Be(readerId);
        read.Mode.Should().Be("QR_STATION");
        read.Place.Should().Be("KHOMO-01");
        read.ExpiresUnix.Should().Be(1_800_000_000);
    }

    [Fact]
    public void Phieu_bi_sua_hoac_ky_sai_khoa_thi_bi_tu_choi()
    {
        var payload = new SelfCheckoutSigner.TokenPayload(Guid.NewGuid(), "WIFI_SSID", "LC-WIFI", 1_800_000_000);
        var token = SelfCheckoutSigner.IssueToken(payload, Secret);

        SelfCheckoutSigner.ReadToken(token, "khoa-khac").Should().BeNull();
        SelfCheckoutSigner.ReadToken(token[..^3] + "abc", Secret).Should().BeNull();
        SelfCheckoutSigner.ReadToken("x" + token, Secret).Should().BeNull();
        SelfCheckoutSigner.ReadToken("", Secret).Should().BeNull();
    }

    [Fact]
    public void Khoa_moi_du_dai_va_khong_trung()
    {
        var first = SelfCheckoutSigner.NewSecret();
        var second = SelfCheckoutSigner.NewSecret();

        first.Length.Should().BeGreaterThanOrEqualTo(40);
        first.Should().NotBe(second);
    }
}
