namespace LibraryConnect.Infrastructure.Persistence.Seeding;

/// <summary>
/// Khóa nhập học và hạn thẻ của bạn đọc trong bộ dữ liệu trình diễn.
/// </summary>
public static class DemoReaderCohort
{
    /// <summary>Số khóa sinh viên trải ra trong bộ dữ liệu.</summary>
    public const int Cohorts = 4;

    /// <summary>Thẻ sinh viên có hạn năm năm kể từ ngày nhập học.</summary>
    public const int CardYears = 5;

    /// <summary>
    /// Bốn khóa gần nhất tính từ ngày nạp dữ liệu, khóa mới nhất đã nhập học (ngày 05/09).
    ///
    /// Từng viết cứng 2021–2024: đúng ngày 05/09/2026 chạy nghiệm thu thử trên máy chủ thật thì
    /// cả khóa 2021 hết hạn thẻ cùng lúc — 162 bạn đọc mẫu bị quầy từ chối từ hôm sau. Tính theo
    /// ngày nạp thì khóa cũ nhất còn ít nhất một năm thẻ.
    /// </summary>
    public static int EnrolmentYear(DateOnly today, int index)
    {
        var academicYear = today >= new DateOnly(today.Year, 9, 5) ? today.Year : today.Year - 1;

        return academicYear - (Cohorts - 1) + (index % Cohorts);
    }

    public static DateOnly CardIssueDate(int enrolmentYear) => new(enrolmentYear, 9, 5);

    public static DateOnly CardExpireDate(int enrolmentYear) => new(enrolmentYear + CardYears, 9, 5);
}
