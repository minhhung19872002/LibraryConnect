using FluentAssertions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Application.Features.Opac;

namespace LibraryConnect.UnitTests.Cms;

/// <summary>
/// Cách ghép điều kiện của tra cứu nâng cao (IX.2).
///
/// Ba dấu nối VÀ / HOẶC / KHÔNG phải gộp thành đúng một biểu thức để dịch xuống một câu lệnh duy
/// nhất. Ghép sai thì kết quả vẫn ra — chỉ là ra nhầm, và không ai phát hiện bằng mắt thường.
/// </summary>
public class OpacQueryTests
{
    private sealed record Doc(string Title, string Author);

    private static readonly Doc[] Shelf =
    {
        new("Kỹ thuật lập trình", "Nguyễn Văn A"),
        new("Kỹ thuật điện tử", "Trần Thị B"),
        new("Cơ sở dữ liệu", "Nguyễn Văn A")
    };

    /// <summary>Áp một biểu thức lọc lên danh sách trong bộ nhớ, đúng cách EF sẽ áp lên cơ sở dữ liệu.</summary>
    private static IReadOnlyList<Doc> Apply(
        System.Linq.Expressions.Expression<Func<Doc, bool>> predicate) =>
        Shelf.AsQueryable().Where(predicate).ToList();

    private static System.Linq.Expressions.Expression<Func<Doc, bool>> Title(string term) =>
        doc => doc.Title.Contains(term);

    private static System.Linq.Expressions.Expression<Func<Doc, bool>> Author(string term) =>
        doc => doc.Author.Contains(term);

    [Fact]
    public void Ghep_bang_VA_thu_hep_ket_qua()
    {
        var result = Apply(Title("Kỹ thuật").And(Author("Nguyễn")));

        result.Should().ContainSingle().Which.Title.Should().Be("Kỹ thuật lập trình");
    }

    [Fact]
    public void Ghep_bang_HOAC_mo_rong_ket_qua()
    {
        var result = Apply(Title("Cơ sở").Or(Author("Trần")));

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Ghep_bang_KHONG_loai_tru_dung_phan_can_bo()
    {
        var result = Apply(Title("Kỹ thuật").And(Title("điện tử").Not()));

        result.Should().ContainSingle().Which.Title.Should().Be("Kỹ thuật lập trình");
    }

    [Fact]
    public void Ghep_ba_menh_de_giu_dung_thu_tu_trai_sang_phai()
    {
        // "Kỹ thuật" HOẶC "Cơ sở" rồi VÀ tác giả Nguyễn: đọc từ trái sang phải, ra hai cuốn của
        // Nguyễn Văn A.
        var combined = Title("Kỹ thuật").Or(Title("Cơ sở")).And(Author("Nguyễn"));

        Apply(combined).Should().HaveCount(2);
    }

    [Fact]
    public void Menh_de_bo_trong_khong_lam_hong_ca_cau_truy_van()
    {
        var clauses = new List<OpacSearchClause>
        {
            new() { Connector = OpacConnector.And, Field = OpacSearchScope.Title, Term = "  " },
            new() { Connector = OpacConnector.And, Field = OpacSearchScope.Title, Term = "" }
        };

        // Không có mệnh đề nào dùng được thì trả về điều kiện luôn đúng, tức là không lọc gì —
        // chứ không phải trả về danh sách rỗng.
        var predicate = OpacQueryBuilder.Combine(clauses).Compile();

        predicate(null!).Should().BeTrue();
    }

    [Fact]
    public void Chuan_hoa_tu_khoa_bo_dau_va_viet_thuong()
    {
        OpacQueryBuilder.Normalise("  Cơ Sở Dữ Liệu  ").Should().Be("co so du lieu");
    }

    [Fact]
    public void Ten_pham_vi_tim_kiem_deu_co_chu_tieng_Viet()
    {
        foreach (var scope in Enum.GetValues<OpacSearchScope>())
        {
            OpacScopeLabels.Describe(scope).Should().NotBeNullOrWhiteSpace();
        }
    }
}

/// <summary>Đường dẫn thân thiện cho trang tĩnh và bản tin (VIII.1, VIII.2).</summary>
public class UrlSlugTests
{
    [Fact]
    public void Bo_dau_viet_thuong_va_ngan_bang_dau_noi()
    {
        VietnameseText.UrlSlug("Nội quy thư viện").Should().Be("noi-quy-thu-vien");
    }

    [Fact]
    public void Gop_cac_ky_tu_ngan_cach_lien_tiep_thanh_mot_dau_noi()
    {
        VietnameseText.UrlSlug("Thông báo:   nghỉ lễ 30/4 & 1/5")
            .Should().Be("thong-bao-nghi-le-30-4-1-5");
    }

    [Fact]
    public void Khong_de_dau_noi_o_dau_hoac_cuoi()
    {
        var slug = VietnameseText.UrlSlug("--- Giới thiệu ---");

        slug.Should().Be("gioi-thieu");
    }

    [Fact]
    public void Cat_ngan_theo_gioi_han_ma_khong_de_lai_dau_noi_thua()
    {
        var slug = VietnameseText.UrlSlug("Thư viện trường đại học tài nguyên và môi trường", 20);

        slug.Length.Should().BeLessThanOrEqualTo(20);
        slug.Should().NotEndWith("-");
    }

    [Fact]
    public void Chuoi_khong_co_chu_cai_tra_ve_rong_de_ben_goi_tu_dat_ten_thay()
    {
        VietnameseText.UrlSlug("!!!").Should().BeEmpty();
    }
}
