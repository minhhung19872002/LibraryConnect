using LibraryConnect.Domain.Entities.Cat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>
    /// Khoa, ngành đào tạo và môn học ban đầu (Phân hệ X).
    ///
    /// Danh sách lấy theo khung chương trình phổ biến của trường đại học Việt Nam, chọn những môn
    /// nhiều ngành cùng học — Tin học đại cương, Xác suất thống kê, Pháp luật đại cương — vì đó
    /// chính là chỗ nghiệp vụ cần quan hệ nhiều-nhiều và cũng là chỗ thư viện hay thiếu bản.
    ///
    /// Chỉ nạp khi ba bảng còn trống: thư viện thật sẽ nhập danh mục của mình từ Excel, và không ai
    /// muốn danh mục mẫu chen vào giữa dữ liệu thật sau một lần nâng cấp.
    /// </summary>
    private async Task SeedCoursesAsync(CancellationToken ct)
    {
        if (await _db.Courses.AnyAsync(ct) || await _db.CourseMajors.AnyAsync(ct))
        {
            return;
        }

        var faculties = await EnsureFacultiesAsync(ct);
        var majors = await EnsureMajorsAsync(faculties, ct);

        var courses = new (string Code, string Name, int Credits, string Semester, string[] Majors)[]
        {
            ("DC101", "Tin học đại cương", 3, "Học kỳ 1",
                new[] { "CNTT", "KTPM", "QTKD", "KTMT", "LUAT", "MOITRUONG" }),
            ("DC102", "Pháp luật đại cương", 2, "Học kỳ 1",
                new[] { "CNTT", "KTPM", "QTKD", "KTMT", "LUAT", "MOITRUONG" }),
            ("DC201", "Xác suất thống kê", 3, "Học kỳ 2",
                new[] { "CNTT", "KTPM", "QTKD", "KTMT" }),
            ("CS201", "Cấu trúc dữ liệu và giải thuật", 4, "Học kỳ 3", new[] { "CNTT", "KTPM" }),
            ("CS202", "Cơ sở dữ liệu", 4, "Học kỳ 3", new[] { "CNTT", "KTPM", "KTMT" }),
            ("CS301", "Mạng máy tính", 3, "Học kỳ 4", new[] { "CNTT", "KTMT" }),
            ("CS302", "Công nghệ phần mềm", 3, "Học kỳ 5", new[] { "KTPM" }),
            ("CS401", "Trí tuệ nhân tạo", 3, "Học kỳ 6", new[] { "CNTT", "KTPM" }),
            ("QT201", "Quản trị học", 3, "Học kỳ 2", new[] { "QTKD" }),
            ("QT301", "Marketing căn bản", 3, "Học kỳ 4", new[] { "QTKD" }),
            ("MT201", "Hóa học môi trường", 3, "Học kỳ 3", new[] { "MOITRUONG" }),
            ("MT301", "Quản lý tài nguyên nước", 3, "Học kỳ 5", new[] { "MOITRUONG" }),
            ("LU201", "Luật dân sự", 3, "Học kỳ 3", new[] { "LUAT" }),
            ("LU301", "Luật sở hữu trí tuệ", 2, "Học kỳ 5", new[] { "LUAT", "KTPM" })
        };

        var order = 0;

        foreach (var entry in courses)
        {
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Code = entry.Code,
                Name = entry.Name,
                Credits = entry.Credits,
                Semester = entry.Semester,
                SortOrder = order += 10,
                IsActive = true
            };

            _db.Courses.Add(course);

            foreach (var majorCode in entry.Majors.Where(majors.ContainsKey))
            {
                _db.CourseMajors.Add(new CourseMajor
                {
                    CourseId = course.Id,
                    MajorId = majors[majorCode]
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nạp {Count} môn học mẫu", courses.Length);
    }

    private async Task<Dictionary<string, Guid>> EnsureFacultiesAsync(CancellationToken ct)
    {
        var wanted = new (string Code, string Name)[]
        {
            ("CNTT", "Khoa Công nghệ thông tin"),
            ("KINHTE", "Khoa Kinh tế và Quản trị"),
            ("MOITRUONG", "Khoa Môi trường"),
            ("LUAT", "Khoa Luật")
        };

        var existing = await _db.Faculties.ToDictionaryAsync(row => row.Code, row => row.Id, ct);
        var order = 0;

        foreach (var entry in wanted.Where(entry => !existing.ContainsKey(entry.Code)))
        {
            var faculty = new Faculty
            {
                Id = Guid.NewGuid(),
                Code = entry.Code,
                Name = entry.Name,
                SortOrder = order += 10,
                IsActive = true
            };

            _db.Faculties.Add(faculty);
            existing[entry.Code] = faculty.Id;
        }

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    private async Task<Dictionary<string, Guid>> EnsureMajorsAsync(
        IReadOnlyDictionary<string, Guid> faculties, CancellationToken ct)
    {
        var wanted = new (string Code, string Name, string Faculty, string Level)[]
        {
            ("CNTT", "Công nghệ thông tin", "CNTT", "DH"),
            ("KTPM", "Kỹ thuật phần mềm", "CNTT", "DH"),
            ("KTMT", "Kỹ thuật máy tính", "CNTT", "DH"),
            ("QTKD", "Quản trị kinh doanh", "KINHTE", "DH"),
            ("MOITRUONG", "Công nghệ kỹ thuật môi trường", "MOITRUONG", "DH"),
            ("LUAT", "Luật kinh tế", "LUAT", "DH")
        };

        var existing = await _db.Majors.ToDictionaryAsync(row => row.Code, row => row.Id, ct);
        var order = 0;

        foreach (var entry in wanted.Where(entry => !existing.ContainsKey(entry.Code)))
        {
            var major = new Major
            {
                Id = Guid.NewGuid(),
                Code = entry.Code,
                Name = entry.Name,
                FacultyId = faculties.TryGetValue(entry.Faculty, out var facultyId) ? facultyId : null,
                TrainingLevel = entry.Level,
                SortOrder = order += 10,
                IsActive = true
            };

            _db.Majors.Add(major);
            existing[entry.Code] = major.Id;
        }

        await _db.SaveChangesAsync(ct);
        return existing;
    }
}
