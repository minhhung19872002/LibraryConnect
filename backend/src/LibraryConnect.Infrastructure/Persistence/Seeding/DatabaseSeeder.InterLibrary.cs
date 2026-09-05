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
    ///
    /// Máy chủ nào có cả hai lối thì khai luôn địa chỉ SRU vào dòng Z39.50 làm lối dự phòng: khi
    /// bước Present bị từ chối, hệ thống lấy lại cùng truy vấn ấy qua SRU thay vì trả danh sách rỗng.
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
                // Lối dự phòng của chính thư viện này. Máy chủ nhận truy vấn và báo đúng số kết quả
                // nhưng nhiều lúc từ chối bước Present, trả về tay không; khai sẵn địa chỉ SRU thì
                // hệ thống tự lấy qua lối kia thay vì để cán bộ nhìn một danh sách rỗng.
                SruBaseUrl = "http://lx2.loc.gov:210/lcdb",
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
                // Máy chủ thứ ba giữ ở **trạng thái tắt**: đo ngày 06/09/2026 từ máy chủ thật,
                // z3950.library.yale.edu mở cổng nhưng đóng phiên ngay sau bắt tay, và hai máy chủ
                // công khai khác (BnF, Thư viện Quốc gia Úc) cũng từ chối phiên ẩn danh. Để nó bật
                // là màn hình "Kiểm tra kết nối" hiện một dòng đỏ ngay trong buổi nghiệm thu, do
                // chính sách của thư viện bạn chứ không phải lỗi của mình. Cán bộ bật lại được bất
                // cứ lúc nào; cơ sở dữ liệu LCDB_MARC8 dưới đây thay chỗ nó ở danh sách đang chạy.
                Name = "Thư viện Đại học Yale (đang tắt — máy chủ từ chối phiên ẩn danh)",
                Host = "z3950.library.yale.edu",
                Port = 7090,
                DatabaseName = "voyager",
                Charset = "MARC-8",
                RecordSyntax = "USMARC",
                IsActive = false,
                ShowOnOpac = false,
                TimeoutSeconds = 30,
                SortOrder = 40,
            },
            new Z3950Target
            {
                // Cơ sở dữ liệu MARC-8 của chính Thư viện Quốc hội Mỹ: cùng máy chủ, khác kho, và
                // trả lời được (đo 06/09/2026: 949.926 kết quả cho phép tra thử).
                Name = "Thư viện Quốc hội Mỹ (kho MARC-8)",
                Host = "lx2.loc.gov",
                Port = 210,
                DatabaseName = "LCDB_MARC8",
                Charset = "MARC-8",
                RecordSyntax = "USMARC",
                TimeoutSeconds = 30,
                IsActive = true,
                ShowOnOpac = false,
                SortOrder = 30,
            });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Đã nạp các máy chủ thư viện công khai để tra cứu liên thư viện.");
    }
}
