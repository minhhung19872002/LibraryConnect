using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Entities.Cir;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>
    /// Nạp dữ liệu khởi tạo của Phân hệ VII: chính sách lưu thông theo loại bạn đọc, lịch nghỉ lễ,
    /// tủ gửi đồ và bốn mẫu biểu in ở quầy.
    ///
    /// Thư viện vừa cài đặt phải cho mượn được ngay trong buổi sáng đầu tiên, nên mọi thứ ở đây đều
    /// là giá trị dùng được thật chứ không phải bản nháp: cán bộ chỉ cần sửa số ngày mượn cho hợp
    /// quy chế của trường mình.
    /// </summary>
    private async Task SeedCirculationAsync(CancellationToken ct)
    {
        await SeedCirculationPoliciesAsync(ct);
        await SeedHolidaysAsync(ct);
        await SeedLockersAsync(ct);
        await SeedCirculationFormTemplatesAsync(ct);
    }

    private async Task SeedCirculationPoliciesAsync(CancellationToken ct)
    {
        if (await _db.CirculationPolicies.AnyAsync(ct))
        {
            return;
        }

        var readerTypes = await _db.ReaderTypes
            .ToDictionaryAsync(type => type.Code, type => type.Id, ct);

        // Số ngày mượn và hạn mức theo thông lệ thư viện đại học Việt Nam: sinh viên mượn ít và ngắn
        // ngày hơn giảng viên, bạn đọc khách chỉ đọc tại chỗ.
        var seeds = new (string Code, string Name, int MaxItems, int LoanDays, int Renewals,
            decimal Fine, bool TakeHome)[]
        {
            ("SV", "Sinh viên", 3, 14, 2, 2000, true),
            ("HV", "Học viên cao học", 5, 21, 2, 2000, true),
            ("NCS", "Nghiên cứu sinh", 7, 30, 3, 2000, true),
            ("GV", "Giảng viên", 10, 60, 3, 1000, true),
            ("CBNV", "Cán bộ, nhân viên", 5, 30, 2, 1000, true),
            ("KHACH", "Bạn đọc khách", 2, 3, 0, 5000, false)
        };

        var priority = 100;

        foreach (var seed in seeds)
        {
            if (!readerTypes.TryGetValue(seed.Code, out var readerTypeId))
            {
                continue;
            }

            _db.CirculationPolicies.Add(new CirculationPolicy
            {
                Name = $"Chính sách mượn — {seed.Name}",
                ReaderTypeId = readerTypeId,
                MaxItems = seed.MaxItems,
                LoanDays = seed.LoanDays,
                MaxRenewals = seed.Renewals,
                RenewalDays = seed.LoanDays,
                FinePerDay = seed.Fine,
                GraceDays = 1,
                MaxHolds = 3,
                HoldExpireDays = 3,
                AllowLoan = true,
                AllowRenew = seed.Renewals > 0,
                AllowHold = true,
                AllowTakeHome = seed.TakeHome,
                RequireRenewalApproval = false,
                Priority = priority,
                IsActive = true,
                CreatedAt = _clock.Now
            });

            priority += 10;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nạp {Count} chính sách lưu thông mặc định", seeds.Length);
    }

    private async Task SeedHolidaysAsync(CancellationToken ct)
    {
        if (await _db.Holidays.AnyAsync(ct))
        {
            return;
        }

        // Ngày nghỉ lễ theo dương lịch, lặp hằng năm. Tết Nguyên đán theo âm lịch nên mỗi năm cán bộ
        // tự khai một dòng — không viết cứng được vào code.
        var seeds = new (string Name, int Month, int Day, int Days)[]
        {
            ("Tết Dương lịch", 1, 1, 1),
            ("Ngày Giải phóng miền Nam", 4, 30, 1),
            ("Ngày Quốc tế Lao động", 5, 1, 1),
            ("Quốc khánh", 9, 2, 2)
        };

        var year = _clock.Today.Year;

        foreach (var seed in seeds)
        {
            var from = new DateOnly(year, seed.Month, seed.Day);

            _db.Holidays.Add(new Holiday
            {
                Name = seed.Name,
                FromDate = from,
                ToDate = from.AddDays(seed.Days - 1),
                IsRecurringYearly = true,
                IsActive = true,
                CreatedAt = _clock.Now
            });
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nạp {Count} kỳ nghỉ lễ hằng năm", seeds.Length);
    }

    private async Task SeedLockersAsync(CancellationToken ct)
    {
        if (await _db.Lockers.AnyAsync(ct))
        {
            return;
        }

        var library = await _db.Libraries
            .OrderBy(entity => entity.SortOrder)
            .Select(entity => (Guid?)entity.Id)
            .FirstOrDefaultAsync(ct);

        // Hai dãy tủ mười ngăn, đánh số theo cách thư viện hay dán nhãn: A01…A10, B01…B10.
        foreach (var area in new[] { "A", "B" })
        {
            for (var index = 1; index <= 10; index++)
            {
                _db.Lockers.Add(new Locker
                {
                    Code = $"{area}{index:00}",
                    LibraryId = library,
                    Area = $"Dãy {area}",
                    Size = index <= 6 ? "Nhỏ" : "Lớn",
                    Status = LockerStatus.Free,
                    MapRow = area == "A" ? 1 : 2,
                    MapColumn = index,
                    CreatedAt = _clock.Now
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nạp 20 tủ gửi đồ mẫu");
    }

    /// <summary>Bốn mẫu biểu của quầy lưu thông (VII.4), dùng chung trình thiết kế của Phase 6.</summary>
    private async Task SeedCirculationFormTemplatesAsync(CancellationToken ct)
    {
        var existing = await _db.FormTemplates
            .Where(template => template.FormType == "LOAN_SLIP"
                               || template.FormType == "RETURN_SLIP"
                               || template.FormType == "FINE_RECEIPT"
                               || template.FormType == "CLEARANCE")
            .AnyAsync(ct);

        if (existing)
        {
            return;
        }

        var organisation = new[] { "{libraryName}" };

        _db.FormTemplates.AddRange(
            FormTemplate("PM-QUAY", "Phiếu mượn tài liệu", "LOAN_SLIP", Json(new
            {
                showLogo = true,
                organisationLines = organisation,
                showNationalHeading = false,
                title = "Phiếu mượn tài liệu",
                subtitle = "Số: {code} — Ngày {loanDate}",
                introLines = Array.Empty<string>(),
                fields = new object[]
                {
                    new { label = "Bạn đọc", key = "readerName", fullWidth = false },
                    new { label = "Số thẻ", key = "cardNumber", fullWidth = false },
                    new { label = "Mã sinh viên", key = "studentCode", fullWidth = false },
                    new { label = "Lớp", key = "className", fullWidth = false },
                    new { label = "Khoa", key = "faculty", fullWidth = true }
                },
                columns = new object[]
                {
                    new { header = "TT", key = "index", width = 0.5, align = "center", sum = false },
                    new { header = "Mã vạch", key = "barcode", width = 1.5, align = "left", sum = false },
                    new { header = "Nhan đề", key = "title", width = 4.5, align = "left", sum = false },
                    new { header = "Ký hiệu xếp giá", key = "callNumber", width = 1.6, align = "left", sum = false },
                    new { header = "Hạn trả", key = "dueDate", width = 1.3, align = "center", sum = false }
                },
                showTotals = false,
                closingLines = new[]
                {
                    "Tổng số: {totalItems} tài liệu. Hạn trả: {dueDate}.",
                    "Bạn đọc có trách nhiệm giữ gìn và trả tài liệu đúng hạn."
                },
                signatures = new object[]
                {
                    new { role = "Bạn đọc", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Cán bộ thư viện", note = "{staffName}" }
                },
                fontSize = 10.0
            })),

            FormTemplate("PT-QUAY", "Phiếu trả tài liệu", "RETURN_SLIP", Json(new
            {
                showLogo = true,
                organisationLines = organisation,
                showNationalHeading = false,
                title = "Phiếu trả tài liệu",
                subtitle = "Số: {code} — Ngày {returnDate}",
                introLines = Array.Empty<string>(),
                fields = new object[]
                {
                    new { label = "Bạn đọc", key = "readerName", fullWidth = false },
                    new { label = "Số thẻ", key = "cardNumber", fullWidth = false },
                    new { label = "Lớp", key = "className", fullWidth = false },
                    new { label = "Khoa", key = "faculty", fullWidth = false }
                },
                columns = new object[]
                {
                    new { header = "TT", key = "index", width = 0.5, align = "center", sum = false },
                    new { header = "Mã vạch", key = "barcode", width = 1.5, align = "left", sum = false },
                    new { header = "Nhan đề", key = "title", width = 4.0, align = "left", sum = false },
                    new { header = "Hạn trả", key = "dueDate", width = 1.2, align = "center", sum = false },
                    new { header = "Ngày trả", key = "returnDate", width = 1.2, align = "center", sum = false },
                    new { header = "Quá hạn", key = "overdueDays", width = 1.0, align = "right", sum = false },
                    new { header = "Tiền phạt", key = "fine", width = 1.3, align = "right", sum = true }
                },
                showTotals = true,
                closingLines = new[]
                {
                    "Tổng số: {totalItems} tài liệu, tiền phạt {totalFine} đồng."
                },
                signatures = new object[]
                {
                    new { role = "Bạn đọc", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Cán bộ thư viện", note = "{staffName}" }
                },
                fontSize = 10.0
            })),

            FormTemplate("BL-PHAT", "Biên lai thu tiền phạt", "FINE_RECEIPT", Json(new
            {
                showLogo = true,
                organisationLines = organisation,
                showNationalHeading = true,
                title = "Biên lai thu tiền phạt",
                subtitle = "Số: {code} — Ngày {paidAt}",
                introLines = new[] { "Thư viện đã thu của bạn đọc số tiền phạt như sau:" },
                fields = new object[]
                {
                    new { label = "Bạn đọc", key = "readerName", fullWidth = false },
                    new { label = "Số thẻ", key = "cardNumber", fullWidth = false },
                    new { label = "Lớp", key = "className", fullWidth = false },
                    new { label = "Khoa", key = "faculty", fullWidth = false },
                    new { label = "Loại phạt", key = "fineType", fullWidth = false },
                    new { label = "Lý do", key = "reason", fullWidth = true }
                },
                columns = new object[]
                {
                    new { header = "TT", key = "index", width = 0.5, align = "center", sum = false },
                    new { header = "Nội dung", key = "title", width = 5.0, align = "left", sum = false },
                    new { header = "Mã vạch", key = "barcode", width = 1.5, align = "left", sum = false },
                    new { header = "Số tiền", key = "fine", width = 1.5, align = "right", sum = true }
                },
                showTotals = true,
                closingLines = new[]
                {
                    "Số tiền đã thu: {paidAmount} đồng. Còn nợ: {outstanding} đồng.",
                    "Bằng chữ: {amountInWords}."
                },
                signatures = new object[]
                {
                    new { role = "Người nộp tiền", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Người thu tiền", note = "{staffName}" }
                },
                fontSize = 10.0
            })),

            FormTemplate("GXN-TRASACH", "Giấy xác nhận trả sách", "CLEARANCE", Json(new
            {
                showLogo = true,
                organisationLines = organisation,
                showNationalHeading = true,
                title = "Giấy xác nhận trả tài liệu",
                subtitle = "",
                introLines = new[] { "Thư viện xác nhận tình trạng mượn trả của bạn đọc như sau:" },
                fields = new object[]
                {
                    new { label = "Họ và tên", key = "readerName", fullWidth = false },
                    new { label = "Ngày sinh", key = "dateOfBirth", fullWidth = false },
                    new { label = "Số thẻ", key = "cardNumber", fullWidth = false },
                    new { label = "Mã sinh viên", key = "studentCode", fullWidth = false },
                    new { label = "Lớp", key = "className", fullWidth = false },
                    new { label = "Khóa", key = "courseYear", fullWidth = false },
                    new { label = "Khoa", key = "faculty", fullWidth = true },
                    new { label = "Tổng lượt đã mượn", key = "totalLoans", fullWidth = false },
                    new { label = "Tài liệu chưa trả", key = "outstandingLoans", fullWidth = false },
                    new { label = "Tiền phạt còn nợ", key = "outstandingFines", fullWidth = false }
                },
                columns = new object[]
                {
                    new { header = "TT", key = "index", width = 0.5, align = "center", sum = false },
                    new { header = "Mã vạch", key = "barcode", width = 1.5, align = "left", sum = false },
                    new { header = "Nhan đề", key = "title", width = 5.0, align = "left", sum = false },
                    new { header = "Hạn trả", key = "dueDate", width = 1.3, align = "center", sum = false }
                },
                showTotals = false,
                closingLines = new[]
                {
                    "{conclusion}",
                    "Giấy này được cấp để bạn đọc hoàn tất thủ tục theo quy định của nhà trường."
                },
                signatures = new object[]
                {
                    new { role = "Bạn đọc", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Cán bộ thư viện", note = "{staffName}" }
                },
                fontSize = 10.0
            })));

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nạp 4 mẫu biểu của quầy lưu thông");
    }
}
