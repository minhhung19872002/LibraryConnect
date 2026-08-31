using FluentAssertions;
using LibraryConnect.Application.Features.Courses;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.UnitTests.Courses;

/// <summary>
/// Quy tắc đọc mức độ liên quan từ tệp Excel do khoa gửi sang (X.3).
///
/// Bảng do khoa lập nên cột "Mức độ" viết mỗi nơi một kiểu: có dấu, không dấu, viết tắt, viết hoa.
/// Đọc sai thì cả danh mục vào hệ thống với mức độ sai, mà nhìn bằng mắt không phát hiện ra.
/// </summary>
public class CourseRelationParsingTests
{
    [Theory]
    [InlineData("Giáo trình chính")]
    [InlineData("giao trinh chinh")]
    [InlineData("GIÁO TRÌNH")]
    [InlineData("  Giáo trình  ")]
    public void Nhan_ra_giao_trinh_chinh_du_go_kieu_nao(string value)
    {
        CourseDocumentImportColumns.ParseRelation(value)
            .Should().Be(CourseRelationType.MainTextbook);
    }

    [Theory]
    [InlineData("Tài liệu tham khảo thêm")]
    [InlineData("tham khao them")]
    [InlineData("Đọc thêm")]
    public void Nhan_ra_tai_lieu_doc_them(string value)
    {
        CourseDocumentImportColumns.ParseRelation(value)
            .Should().Be(CourseRelationType.AdditionalReference);
    }

    [Theory]
    [InlineData("Tài liệu tham khảo bắt buộc")]
    [InlineData("tham khao")]
    [InlineData("")]
    [InlineData(null)]
    public void Bo_trong_hoac_khong_ro_thi_hieu_la_tham_khao_bat_buoc(string? value)
    {
        // Mặc định phải là mức giữa: đoán thành giáo trình chính là nói quá, đoán thành đọc thêm là
        // hạ thấp tài liệu khoa đã yêu cầu.
        CourseDocumentImportColumns.ParseRelation(value)
            .Should().Be(CourseRelationType.RequiredReference);
    }

    [Fact]
    public void Ba_muc_do_deu_co_chu_tieng_Viet()
    {
        foreach (var type in Enum.GetValues<CourseRelationType>())
        {
            CourseRelationLabels.Describe(type).Should().NotBeNullOrWhiteSpace();
        }

        CourseRelationLabels.Describe(CourseRelationType.MainTextbook)
            .Should().Be("Giáo trình chính");
    }
}

/// <summary>Cách tính tỷ lệ đáp ứng tài liệu của một ngành đào tạo (X.3).</summary>
public class MajorCoverageTests
{
    private static MajorCoverageDto Coverage(int courses, int covered) =>
        new(Guid.NewGuid(), "CNTT", "Công nghệ thông tin", "Khoa CNTT", courses, covered, covered * 2);

    [Fact]
    public void Ty_le_lam_tron_mot_chu_so_thap_phan()
    {
        Coverage(3, 1).CoveragePercent.Should().Be(33.3);
        Coverage(8, 5).CoveragePercent.Should().Be(62.5);
    }

    [Fact]
    public void Nganh_chua_co_mon_hoc_nao_thi_ty_le_bang_khong_chu_khong_chia_cho_khong()
    {
        Coverage(0, 0).CoveragePercent.Should().Be(0);
    }

    [Fact]
    public void Du_tai_lieu_cho_moi_mon_thi_ty_le_bang_mot_tram()
    {
        Coverage(6, 6).CoveragePercent.Should().Be(100);
    }
}

/// <summary>Số tài liệu của một môn học cộng từ ba mức độ.</summary>
public class CourseRowTests
{
    [Fact]
    public void Tong_so_tai_lieu_bang_tong_ba_muc_do()
    {
        var course = new CourseRowDto(
            Guid.NewGuid(), "CS202", "Cơ sở dữ liệu", 4, "Học kỳ 3", null, null, true,
            Array.Empty<CourseMajorDto>(), 2, 3, 4);

        course.DocumentCount.Should().Be(9);
    }
}
