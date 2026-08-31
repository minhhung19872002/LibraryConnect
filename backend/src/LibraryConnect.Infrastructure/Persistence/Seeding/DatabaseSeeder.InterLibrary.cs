using LibraryConnect.Domain.Entities.Ill;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>
    /// Nạp sẵn ba máy chủ thư viện công khai để bấm tra là chạy được ngay (mục 8 của đặc tả).
    ///
    /// Hai máy chủ Z39.50 thật của Thư viện Quốc hội Mỹ, cộng một đường SRU cũng của họ — SRU đi
    /// qua tường lửa dễ hơn nhiều, nên thư viện nào chặn cổng lạ vẫn tra được ít nhất một nguồn.
    /// Mật khẩu không có vì cả ba đều phục vụ tra cứu công khai.
    /// </summary>
    private async Task SeedInterLibraryTargetsAsync(CancellationToken ct)
    {
        if (await _db.Z3950Targets.AnyAsync(ct))
        {
            return;
        }

        _db.Z3950Targets.AddRange(
            new Z3950Target
            {
                Name = "Thư viện Quốc hội Mỹ (Z39.50)",
                Host = "lx2.loc.gov",
                Port = 210,
                DatabaseName = "LCDB",
                Charset = "MARC-8",
                RecordSyntax = "USMARC",
                TimeoutSeconds = 30,
                IsActive = true,
                ShowOnOpac = true,
                SortOrder = 10,
            },
            new Z3950Target
            {
                Name = "Thư viện Quốc hội Mỹ (SRU)",
                Host = "lx2.loc.gov",
                Port = 443,
                DatabaseName = "lcdb",
                UseSru = true,
                SruBaseUrl = "http://lx2.loc.gov:210/lcdb",
                Charset = "UTF-8",
                RecordSyntax = "XML",
                TimeoutSeconds = 30,
                IsActive = true,
                ShowOnOpac = true,
                SortOrder = 20,
            },
            new Z3950Target
            {
                Name = "Thư viện Đại học Yale",
                Host = "z3950.library.yale.edu",
                Port = 7090,
                DatabaseName = "voyager",
                Charset = "MARC-8",
                RecordSyntax = "USMARC",
                TimeoutSeconds = 30,
                IsActive = true,
                ShowOnOpac = false,
                SortOrder = 30,
            });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Đã nạp ba máy chủ thư viện công khai để tra cứu liên thư viện.");
    }
}
