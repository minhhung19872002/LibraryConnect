using System.Globalization;
using FluentAssertions;
using LibraryConnect.Application.Features.Cataloging;

namespace LibraryConnect.UnitTests.Cataloging;

/// <summary>
/// Tông màu của bìa dựng sẵn phải phân biệt được bằng mắt.
///
/// Bìa dựng sẵn là ảnh bìa của **99,96% kho** (chỉ 3 trên 7.675 biểu ghi tra được ảnh thật), nên màu
/// của nó không phải chuyện trang trí: đó là thứ duy nhất cho bạn đọc lướt trang kết quả mà nhận ra
/// ngay đâu là luận văn, đâu là giáo trình, đâu là bài giảng.
///
/// Bảng màu đầu tiên chọn toàn tông tối cùng độ sáng và cùng vùng xanh lam – tím, nên nhìn cả trang
/// chỉ thấy một dải xanh navy: đúng là mỗi dạng một mã màu khác nhau, nhưng mắt không phân biệt
/// được. Phép thử này đo bằng máy hai điều mắt phải thấy: **màu chữ đủ tương phản để đọc**, và
/// **các dạng tài liệu chính đủ khác sắc độ để nhận ra nhau**.
/// </summary>
public class CoverPaletteTests
{
    /// <summary>Chín dạng tài liệu có thật trong kho — đây là chỗ mắt phải phân biệt được.</summary>
    private static readonly string[] DangChinh =
    {
        "SACH", "GIAOTRINH", "LUANVAN", "LUANAN", "BAIGIANG",
        "DETAI", "KYYEU", "BAITRICH", "TAPCHI",
    };

    private static readonly string[] TatCa =
    {
        "SACH", "GIAOTRINH", "LUANVAN", "LUANAN", "BAIGIANG", "DETAI", "KYYEU", "BAITRICH",
        "TAPCHI", "BAO", "TUDIEN", "BANDO", "AUDIO", "VIDEO",
    };

    // -----------------------------------------------------------------------------------------

    private static (double R, double G, double B) Tach(string hex)
    {
        var value = hex.TrimStart('#');

        return (
            int.Parse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d,
            int.Parse(value.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d,
            int.Parse(value.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d);
    }

    /// <summary>Độ chói tương đối theo WCAG.</summary>
    private static double DoChoi(string hex)
    {
        var (r, g, b) = Tach(hex);

        static double Tuyen(double c) => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

        return 0.2126 * Tuyen(r) + 0.7152 * Tuyen(g) + 0.0722 * Tuyen(b);
    }

    /// <summary>Tỉ lệ tương phản giữa hai màu theo WCAG.</summary>
    private static double TuongPhan(string a, string b)
    {
        var (sang, toi) = (Math.Max(DoChoi(a), DoChoi(b)), Math.Min(DoChoi(a), DoChoi(b)));

        return (sang + 0.05) / (toi + 0.05);
    }

    /// <summary>Sắc độ trên vòng tròn màu, tính bằng độ (0–360).</summary>
    private static double SacDo(string hex)
    {
        var (r, g, b) = Tach(hex);
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var d = max - min;

        if (d < 0.0001)
        {
            return 0;
        }

        var h = max == r
            ? 60 * (((g - b) / d) % 6)
            : max == g ? 60 * ((b - r) / d + 2) : 60 * ((r - g) / d + 4);

        return h < 0 ? h + 360 : h;
    }

    /// <summary>Độ bão hòa — màu càng gần xám thì sắc độ của nó càng vô nghĩa.</summary>
    private static double BaoHoa(string hex)
    {
        var (r, g, b) = Tach(hex);
        var max = Math.Max(r, Math.Max(g, b));

        return max < 0.0001 ? 0 : (max - Math.Min(r, Math.Min(g, b))) / max;
    }

    /// <summary>Khoảng cách sắc độ ngắn nhất trên vòng tròn.</summary>
    private static double CachSacDo(string a, string b)
    {
        var d = Math.Abs(SacDo(a) - SacDo(b)) % 360;

        return d > 180 ? 360 - d : d;
    }

    // -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData("SACH")]
    [InlineData("GIAOTRINH")]
    [InlineData("LUANVAN")]
    [InlineData("LUANAN")]
    [InlineData("BAIGIANG")]
    [InlineData("DETAI")]
    [InlineData("KYYEU")]
    [InlineData("BAITRICH")]
    [InlineData("TAPCHI")]
    [InlineData("BAO")]
    [InlineData("TUDIEN")]
    [InlineData("BANDO")]
    [InlineData("AUDIO")]
    [InlineData("VIDEO")]
    [InlineData("MOT_MA_LA")]
    public void Chu_tren_nen_du_tuong_phan_de_doc(string ma)
    {
        var palette = CoverImageBuilder.PaletteFor(ma);

        // Mức AAA của WCAG cho chữ thường là 7:1. Nhan đề trên bìa hiện ở cỡ nhỏ trong lưới kết quả
        // nên phải đạt mức ấy, không chỉ mức AA.
        TuongPhan(palette.Background, palette.Foreground)
            .Should().BeGreaterThanOrEqualTo(7.0, $"nền của {ma} phải đủ tối cho chữ trắng");
    }

    [Fact]
    public void Moi_dang_tai_lieu_mot_ma_mau_rieng()
    {
        var mau = TatCa.Select(ma => CoverImageBuilder.PaletteFor(ma).Background).ToList();

        mau.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Chin_dang_co_that_trong_kho_du_khac_sac_do_de_nhan_ra_nhau()
    {
        var gan = new List<string>();

        for (var i = 0; i < DangChinh.Length; i++)
        {
            for (var j = i + 1; j < DangChinh.Length; j++)
            {
                var a = CoverImageBuilder.PaletteFor(DangChinh[i]).Background;
                var b = CoverImageBuilder.PaletteFor(DangChinh[j]).Background;

                // Màu xám không có sắc độ nào đáng kể; đứng cạnh màu bão hòa nào cũng phân biệt
                // được, nên đo sắc độ của nó là vô nghĩa.
                if (BaoHoa(a) < 0.25 || BaoHoa(b) < 0.25)
                {
                    continue;
                }

                var cach = CachSacDo(a, b);

                // Dưới 25 độ thì hai màu nhìn như một, nhất là ở ô bìa nhỏ trên lưới kết quả.
                if (cach < 25)
                {
                    gan.Add($"{DangChinh[i]} ({a}) và {DangChinh[j]} ({b}) chỉ cách nhau "
                            + $"{cach:F0} độ sắc");
                }
            }
        }

        gan.Should().BeEmpty(string.Join("\n", gan));
    }

    [Fact]
    public void Khong_don_het_vao_mot_vung_mau()
    {
        // Bảng cũ dồn cả chín dạng vào vùng xanh lam – tím nên nhìn cả trang chỉ thấy một dải navy.
        var trongVungLamTim = DangChinh
            .Select(ma => CoverImageBuilder.PaletteFor(ma).Background)
            .Where(mau => BaoHoa(mau) >= 0.25)
            .Count(mau => SacDo(mau) is >= 180 and <= 290);

        trongVungLamTim.Should().BeLessThanOrEqualTo(4,
            "quá nửa số dạng nằm trong vùng xanh lam – tím thì cả trang kết quả nhìn như một màu");
    }

    [Fact]
    public void Mau_diem_nhan_sang_hon_nen_de_thay_gay_sach()
    {
        foreach (var ma in TatCa)
        {
            var palette = CoverImageBuilder.PaletteFor(ma);

            DoChoi(palette.Accent).Should().BeGreaterThan(DoChoi(palette.Background),
                $"dải gáy sách của {ma} phải sáng hơn nền mới thấy được");
        }
    }
}
