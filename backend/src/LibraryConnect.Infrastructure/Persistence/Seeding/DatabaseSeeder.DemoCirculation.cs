using System.Globalization;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Entities.Cir;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    // ---------------------------------------------------------------------------------------------
    // Danh mục đi kèm dữ liệu minh họa
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Nguồn kinh phí. Thiếu bảng này thì báo cáo bổ sung theo nguồn kinh phí không có gì để chia,
    /// mà đây lại là báo cáo phòng kế hoạch tài chính hỏi đầu tiên.
    /// </summary>
    private async Task EnsureFundingSourcesAsync(CancellationToken ct)
    {
        if (await _db.FundingSources.AnyAsync(ct))
        {
            return;
        }

        var year = _clock.Today.Year;

        _db.FundingSources.AddRange(
            new FundingSource
            {
                Code = "NSNN",
                Name = "Ngân sách nhà nước",
                Description = "Kinh phí mua tài liệu do ngân sách cấp hằng năm",
                Year = year,
                Budget = 500_000_000,
                SortOrder = 10
            },
            new FundingSource
            {
                Code = "TXUYEN",
                Name = "Kinh phí thường xuyên của trường",
                Description = "Nguồn chi thường xuyên của nhà trường",
                Year = year,
                Budget = 200_000_000,
                SortOrder = 20
            },
            new FundingSource
            {
                Code = "TAITRO",
                Name = "Tài trợ, biếu tặng",
                Description = "Tài liệu do tổ chức, cá nhân trao tặng",
                SortOrder = 30
            },
            new FundingSource
            {
                Code = "DUAN",
                Name = "Dự án, đề tài nghiên cứu",
                Description = "Tài liệu mua bằng kinh phí đề tài, dự án",
                Year = year,
                Budget = 120_000_000,
                SortOrder = 40
            });

        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureSuppliersAsync(CancellationToken ct)
    {
        if (await _db.Suppliers.AnyAsync(ct))
        {
            return;
        }

        _db.Suppliers.AddRange(
            new Supplier
            {
                Code = "NCC-GD",
                Name = "Công ty Sách và Thiết bị trường học",
                TaxCode = "0100109106",
                Address = "187B Giảng Võ, Đống Đa, Hà Nội",
                Phone = "024 3821 4441",
                Email = "kinhdoanh@sachthietbi.example.vn",
                ContactPerson = "Phòng Kinh doanh",
                SortOrder = 10
            },
            new Supplier
            {
                Code = "NCC-FAHASA",
                Name = "Công ty Phát hành sách miền Nam",
                TaxCode = "0300436472",
                Address = "60-62 Lê Lợi, Quận 1, TP. Hồ Chí Minh",
                Phone = "028 3822 5796",
                Email = "hopdong@phathanhsach.example.vn",
                ContactPerson = "Bộ phận Hợp đồng",
                SortOrder = 20
            },
            new Supplier
            {
                Code = "NCC-NGOAIVAN",
                Name = "Công ty Xuất nhập khẩu Sách báo",
                TaxCode = "0100108848",
                Address = "44 Tràng Tiền, Hoàn Kiếm, Hà Nội",
                Phone = "024 3825 7043",
                Email = "import@sachbao.example.vn",
                ContactPerson = "Phòng Nhập khẩu",
                SortOrder = 30
            });

        await _db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------------------------------
    // ĐKCB
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// 500 ĐKCB cho 200 biểu ghi: mỗi đầu hai bản, 100 đầu đầu tiên thêm một bản thứ ba.
    ///
    /// Có đầu nhiều bản mới demo được chuyện thư viện thường gặp: một bản đang có người mượn mà trên
    /// giá vẫn còn bản khác. Một phần nhỏ để ở trạng thái chờ kiểm nhận để màn hình kiểm nhận và
    /// báo cáo tình trạng kho không rỗng.
    /// </summary>
    private async Task<IReadOnlyList<Item>> SeedDemoItemsAsync(
        IReadOnlyList<DemoBib> bibs, CancellationToken ct)
    {
        var warehouses = await _db.Warehouses
            .OrderBy(warehouse => warehouse.Code)
            .ToListAsync(ct);

        if (warehouses.Count == 0)
        {
            return Array.Empty<Item>();
        }

        var shelves = await _db.Shelves.ToListAsync(ct);

        var funding = await _db.FundingSources
            .OrderBy(source => source.SortOrder)
            .Select(source => source.Id)
            .ToListAsync(ct);

        var suppliers = await _db.Suppliers
            .OrderBy(supplier => supplier.SortOrder)
            .Select(supplier => supplier.Id)
            .ToListAsync(ct);

        var today = _clock.Today;

        for (var index = 0; index < bibs.Count; index++)
        {
            var bib = bibs[index];
            var warehouse = warehouses[index % warehouses.Count];

            var shelf = shelves
                .Where(entity => entity.WarehouseId == warehouse.Id)
                .OrderBy(entity => entity.Code)
                .Skip(index % Math.Max(1, shelves.Count(entity => entity.WarehouseId == warehouse.Id)))
                .Select(entity => (Guid?)entity.Id)
                .FirstOrDefault();

            var quantity = index < 100 ? 3 : 2;

            // Chưa kiểm nhận: cứ 25 đầu thì một đầu, đủ để màn hình kiểm nhận có việc mà không làm
            // hụt số bản phục vụ bạn đọc.
            var pending = index % 25 == 24;

            await ItemCreator.CreateAsync(_db, _codes, _parameters, _clock, new ItemCreator.Request(
                Bib: bib.Record,
                Quantity: quantity,
                WarehouseId: warehouse.Id,
                ShelfId: shelf,
                Price: 85_000 + (index % 12) * 15_000,
                FundingSourceId: funding.Count > 0 ? funding[index % funding.Count] : null,
                AcquisitionType: index % 11 == 0 ? AcquisitionType.Donation : AcquisitionType.Purchase,
                OrderId: null,
                Note: null,
                AcquisitionDate: today.AddDays(-(30 + index * 3 % 700)),
                SupplierId: suppliers.Count > 0 ? suppliers[index % suppliers.Count] : null,
                UnlockImmediately: !pending), ct);
        }

        return await _db.Items
            .Where(item => item.Status == ItemStatus.InStock && !item.IsLocked)
            .OrderBy(item => item.Barcode)
            .ToListAsync(ct);
    }

    // ---------------------------------------------------------------------------------------------
    // Bạn đọc
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// 50 bạn đọc trải trên các loại và các khoa, thẻ còn hạn để mượn được ngay.
    ///
    /// Mật khẩu đặt chung là <see cref="DemoReaderPassword"/> và không bắt đổi ở lần đăng nhập đầu:
    /// người demo cần đăng nhập vào trang cá nhân để xem sách đang mượn, mà bắt đổi mật khẩu ngay thì
    /// bước đầu tiên của buổi demo đã là một màn hình đổi mật khẩu.
    /// </summary>
    private async Task<IReadOnlyList<Reader>> SeedDemoReadersAsync(
        CancellationToken ct, int soLuong = 50)
    {
        var readerTypes = await _db.ReaderTypes
            .OrderBy(type => type.SortOrder)
            .ToDictionaryAsync(type => type.Code, type => type.Id, ct);

        var faculties = await _db.Faculties.OrderBy(entity => entity.SortOrder).ToListAsync(ct);
        var majors = await _db.Majors.OrderBy(entity => entity.SortOrder).ToListAsync(ct);

        var today = _clock.Today;
        var passwordHash = _hasher.Hash(DemoReaderPassword);
        var cardNumbers = await _codes.NextBatchAsync("CARD", soLuong, ct);

        var readers = new List<Reader>();

        for (var index = 0; index < soLuong; index++)
        {
            // Tỉ lệ giữ nguyên dù bộ dữ liệu to hay nhỏ: 70% sinh viên, 10% học viên cao học, 6%
            // nghiên cứu sinh, 8% giảng viên, còn lại là cán bộ — đúng hình dáng bạn đọc của một
            // trường đại học.
            var phanTram = index * 100 / soLuong;

            var typeCode = phanTram switch
            {
                < 70 => "SV",
                < 80 => "HV",
                < 86 => "NCS",
                < 94 => "GV",
                _ => "CBNV"
            };

            var major = majors.Count > 0 ? majors[index % majors.Count] : null;

            var faculty = major?.FacultyId is not null
                ? faculties.FirstOrDefault(entity => entity.Id == major.FacultyId)
                : faculties.Count > 0 ? faculties[index % faculties.Count] : null;

            var name = DemoNames.Person(index);

            var enrolmentYear = DemoReaderCohort.EnrolmentYear(today, index);

            // Thẻ hết hạn và thẻ tạm khóa: mỗi loại một người, để cán bộ demo được cảnh báo ở quầy
            // ghi mượn mà không phải tự tay sửa dữ liệu trước buổi nghiệm thu.
            var expired = index == soLuong - 2;
            var suspended = index == soLuong - 1;

            var reader = new Reader
            {
                Id = Guid.NewGuid(),
                CardNumber = cardNumbers[index],
                StudentCode = typeCode == "SV"
                    ? $"{enrolmentYear % 100}{(index + 1) * 7 % 1000:000}{index + 1:00}"
                    : $"CB{index + 1:000}",
                FullName = name,
                Gender = index % 2 == 0 ? "Nam" : "Nữ",
                DateOfBirth = new DateOnly(enrolmentYear - 19, 1 + index % 12, 1 + index % 28),
                Email = $"bandoc{index + 1:00}@example.edu.vn",
                Phone = $"09{index % 10}{(1234567 + index * 13) % 10000000:0000000}",
                Address = $"Số {index + 1}, đường Lê Văn Sỹ, Quận Tân Bình, TP. Hồ Chí Minh",
                ReaderTypeId = readerTypes[typeCode],
                FacultyId = faculty?.Id,
                MajorId = typeCode == "SV" ? major?.Id : null,
                ClassName = typeCode == "SV" && major is not null
                    ? $"{major.Code}{enrolmentYear % 100}{(char)('A' + index % 3)}"
                    : null,
                CourseYear = typeCode == "SV" ? $"K{enrolmentYear % 100}" : null,
                CardIssueDate = DemoReaderCohort.CardIssueDate(enrolmentYear),
                CardExpireDate = expired ? today.AddDays(-20) : DemoReaderCohort.CardExpireDate(enrolmentYear),
                Status = expired
                    ? ReaderStatus.Expired
                    : suspended ? ReaderStatus.Suspended : ReaderStatus.Active,
                StatusReason = suspended ? "Tạm khóa do làm hư hỏng tài liệu" : null,
                PasswordHash = passwordHash,
                MustChangePassword = false,
                CreatedAt = _clock.Now
            };

            readers.Add(reader);
            _db.Readers.Add(reader);

            _db.ReaderCards.Add(new ReaderCard
            {
                ReaderId = reader.Id,
                CardNumber = reader.CardNumber,
                IssueDate = reader.CardIssueDate,
                ExpireDate = reader.CardExpireDate,
                IsCurrent = true
            });
        }

        await _db.SaveChangesAsync(ct);
        return readers;
    }

    // ---------------------------------------------------------------------------------------------
    // Lưu thông
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// 100 lượt mượn trả trải trên sáu tháng gần nhất, gồm cả lượt đã trả, đang mượn và quá hạn.
    ///
    /// Hạn trả và tiền phạt không viết tay: gọi đúng <see cref="CirculationRules"/> mà quầy dùng, nên
    /// số liệu trong bộ minh họa và số liệu hệ thống tự tính về sau luôn cùng một quy tắc — nếu chính
    /// sách lưu thông của thư viện khác mặc định, dữ liệu mẫu cũng đổi theo.
    /// </summary>
    private async Task SeedDemoCirculationAsync(
        IReadOnlyList<Reader> readers,
        IReadOnlyList<Item> items,
        CancellationToken ct,
        int soLuot = 100,
        int soThangTraiDeu = 6)
    {
        if (readers.Count == 0 || items.Count == 0)
        {
            return;
        }

        var borrowers = readers.Where(reader => reader.Status == ReaderStatus.Active).ToList();
        var calendar = await _calendars.GetAsync(null, ct);
        var today = _clock.Today;

        var bibs = await _db.BibRecords
            .Select(record => new { record.Id, record.DocumentTypeId, record.Title })
            .ToDictionaryAsync(entry => entry.Id, entry => entry, ct);

        var loanCodes = await _codes.NextBatchAsync("LOAN", soLuot, ct);
        var fineCodes = await _codes.NextBatchAsync("FINE", Math.Max(30, soLuot / 3), ct);

        // Bốn nhóm giữ nguyên tỉ lệ 60/20/15/5 dù bộ dữ liệu to hay nhỏ.
        var moc1 = soLuot * 60 / 100;
        var moc2 = soLuot * 80 / 100;
        var moc3 = soLuot * 95 / 100;
        var soNgayTrai = soThangTraiDeu * 30;

        var loans = new List<Loan>();
        var fines = new List<Fine>();
        var fineIndex = 0;

        for (var index = 0; index < soLuot; index++)
        {
            var reader = borrowers[index % borrowers.Count];

            // Bước nhảy phải nguyên tố cùng nhau với số bản in, nếu không hai lượt mượn rơi vào
            // cùng một bản và cơ sở dữ liệu từ chối ngay — đúng ràng buộc "một bản in một phiếu
            // đang mở" dựng ra sau lỗi D1. Bước 7 đúng với hầu hết số bản, nhưng kho nào có số bản
            // chia hết cho 7 thì cả lượt nạp đổ.
            var item = items[(index * BuocNhay(items.Count)) % items.Count];

            var bib = bibs.GetValueOrDefault(item.BibId);
            var documentTypeId = bib?.DocumentTypeId;

            var policy = await _policies.ResolveAsync(
                reader.ReaderTypeId, documentTypeId, item.WarehouseId, ct);

            // Bốn nhóm: 60 lượt đã trả đúng hạn, 20 lượt trả muộn có phạt, 15 lượt đang mượn còn
            // hạn, 5 lượt đang quá hạn — đúng hình dáng dữ liệu của một thư viện đang hoạt động.
            var loanDate = index < moc1
                ? today.AddDays(-soNgayTrai + index * soNgayTrai / Math.Max(1, moc1))
                : index < moc2
                    ? today.AddDays(-soNgayTrai * 5 / 6
                                    + (index - moc1) * soNgayTrai / Math.Max(1, moc2 - moc1))
                    : index < moc3
                        ? today.AddDays(-(policy.LoanDays / 2) - (index - moc2) % 5)
                        : today.AddDays(-policy.LoanDays - 12 - (index - moc3) % 20 * 3);

            var dueDate = CirculationRules.DueDate(
                loanDate, policy.LoanDays, calendar, reader.CardExpireDate);

            var loan = new Loan
            {
                Id = Guid.NewGuid(),
                Code = loanCodes[index],
                ReaderId = reader.Id,
                ItemId = item.Id,
                BibId = item.BibId,
                // Chép nhan đề đúng như lối ghi mượn thật ở quầy: thiếu nó thì danh sách sách đang
                // mượn của bản demo hiện toàn dấu gạch ngang.
                BibTitle = bib?.Title,
                Barcode = item.Barcode,
                LoanDate = ToMoment(loanDate, 9),
                DueDate = dueDate,
                PolicyId = policy.PolicyId,
                LoanType = LoanType.TakeHome,
                Channel = index % 9 == 0 ? LoanChannel.Opac : LoanChannel.Desk,
                CreatedAt = _clock.Now
            };

            if (index < moc2)
            {
                // Trả muộn 3–12 ngày với nhóm thứ hai, đủ vượt số ngày ân hạn để sinh phạt thật.
                var returnDate = index < moc1
                    ? dueDate.AddDays(-(index % 4))
                    : dueDate.AddDays(3 + (index - moc1) % 10);

                if (returnDate > today)
                {
                    returnDate = today;
                }

                loan.ReturnDate = ToMoment(returnDate, 15);
                loan.Status = LoanStatus.Returned;
                loan.FineAmount = CirculationRules.OverdueFine(dueDate, returnDate, policy, calendar);

                if (loan.FineAmount > 0 && fineIndex < fineCodes.Count)
                {
                    // Một nửa số phạt đã thu, một nửa còn nợ: màn hình thu tiền phạt và báo cáo công
                    // nợ đều cần cả hai trạng thái.
                    var paid = fineIndex % 2 == 0;

                    fines.Add(new Fine
                    {
                        Code = fineCodes[fineIndex++],
                        ReaderId = reader.Id,
                        LoanId = loan.Id,
                        Type = FineType.Overdue,
                        Amount = loan.FineAmount,
                        PaidAmount = paid ? loan.FineAmount : 0,
                        PaidAt = paid ? ToMoment(returnDate, 15) : null,
                        Note = $"Quá hạn {CirculationRules.ChargeableOverdueDays(dueDate, returnDate, policy.GraceDays, calendar)} ngày"
                    });

                    loan.FinePaid = paid ? loan.FineAmount : 0;
                }
            }
            else
            {
                loan.Status = dueDate < today ? LoanStatus.Overdue : LoanStatus.Active;
                item.Status = ItemStatus.OnLoan;

                if (loan.Status == LoanStatus.Overdue)
                {
                    loan.FineAmount = CirculationRules.OverdueFine(dueDate, today, policy, calendar);
                }
            }

            loans.Add(loan);
        }

        _db.Loans.AddRange(loans);
        _db.Fines.AddRange(fines);
        await _db.SaveChangesAsync(ct);

        await RefreshDemoCountersAsync(loans, fines, ct);
        await SeedDemoHoldsAsync(borrowers, loans, ct);
        await SeedDemoVisitsAsync(borrowers, ct);
    }

    /// <summary>
    /// Cập nhật lại các con số tổng hợp mà mọi màn hình đọc: số bản rảnh của biểu ghi, số sách đang
    /// mượn và tiền nợ của bạn đọc. Đếm lại từ dữ liệu vừa sinh chứ không cộng dần, vì một con số
    /// lệch ở đây sẽ hiện ngay trên trang tra cứu và trên quầy.
    /// </summary>
    private async Task RefreshDemoCountersAsync(
        IReadOnlyList<Loan> loans, IReadOnlyList<Fine> fines, CancellationToken ct)
    {
        var bibIds = loans
            .Where(loan => loan.BibId is not null)
            .Select(loan => loan.BibId!.Value)
            .Distinct()
            .ToList();

        await BibItemCounter.RefreshAsync(_db, bibIds, ct);

        // Số lượt mượn của từng bản in và của biểu ghi: trang tra cứu xếp "sách được mượn nhiều"
        // theo con số ở biểu ghi, nên bỏ qua bước này thì khối ấy trống trơn dù bộ minh họa có sẵn
        // một trăm lượt mượn.
        foreach (var group in loans.GroupBy(loan => loan.ItemId))
        {
            var item = await _db.Items.FirstAsync(entity => entity.Id == group.Key, ct);
            item.LoanCount = group.Count();
        }

        foreach (var group in loans.Where(loan => loan.BibId is not null).GroupBy(loan => loan.BibId!.Value))
        {
            var bib = await _db.BibRecords.FirstAsync(entity => entity.Id == group.Key, ct);
            bib.LoanCount = group.Count();
        }

        foreach (var group in loans.GroupBy(loan => loan.ReaderId))
        {
            var reader = await _db.Readers.FirstAsync(entity => entity.Id == group.Key, ct);

            reader.TotalLoanCount = group.Count();
            reader.CurrentLoanCount = group.Count(
                loan => loan.Status is LoanStatus.Active or LoanStatus.Overdue);

            reader.DebtAmount = fines
                .Where(fine => fine.ReaderId == group.Key && !fine.Waived)
                .Sum(fine => fine.Amount - fine.PaidAmount);
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Vài phiếu đặt giữ để hàng đợi và màn hình đặt giữ có dữ liệu thật.</summary>
    private async Task SeedDemoHoldsAsync(
        IReadOnlyList<Reader> readers, IReadOnlyList<Loan> loans, CancellationToken ct)
    {
        var onLoan = loans
            .Where(loan => loan.Status is LoanStatus.Active or LoanStatus.Overdue && loan.BibId is not null)
            .Take(4)
            .ToList();

        if (onLoan.Count == 0)
        {
            return;
        }

        var warehouse = await _db.Warehouses.OrderBy(entity => entity.Code).FirstOrDefaultAsync(ct);
        var now = _clock.Now;

        for (var index = 0; index < onLoan.Count; index++)
        {
            var loan = onLoan[index];

            // Người đặt giữ phải khác người đang giữ sách, nếu không phiếu trở nên vô nghĩa.
            var reader = readers
                .Skip(index % readers.Count)
                .Concat(readers)
                .First(entity => entity.Id != loan.ReaderId);

            _db.Holds.Add(new Hold
            {
                ReaderId = reader.Id,
                BibId = loan.BibId!.Value,
                HoldDate = now.AddDays(-2 - index),
                ExpireDate = now.AddDays(5 - index),
                PickupWarehouseId = warehouse?.Id,
                Status = HoldStatus.Waiting,
                QueuePosition = 1,
                Channel = LoanChannel.Opac
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Lượt bạn đọc vào thư viện trong hai tuần gần nhất, để báo cáo ra vào và biểu đồ giờ cao điểm
    /// có số liệu ngay khi cài xong.
    /// </summary>
    private async Task SeedDemoVisitsAsync(IReadOnlyList<Reader> readers, CancellationToken ct)
    {
        var today = _clock.Today;

        for (var day = 1; day <= 14; day++)
        {
            var date = today.AddDays(-day);

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                continue;
            }

            var visitors = 6 + day % 5;

            for (var index = 0; index < visitors; index++)
            {
                var reader = readers[(day * 7 + index) % readers.Count];
                var hour = 8 + (index * 2 + day) % 9;

                _db.LibraryVisits.Add(new LibraryVisit
                {
                    ReaderId = reader.Id,
                    CheckinAt = ToMoment(date, hour),
                    CheckoutAt = ToMoment(date, Math.Min(20, hour + 1 + index % 3)),
                    Gate = index % 2 == 0 ? "Cổng chính" : "Cổng phụ",
                    Purpose = index % 3 == 0 ? "Đọc tại chỗ" : null
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------------------------------
    // Tài liệu môn học
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Nối tài liệu minh họa vào môn học theo ký hiệu phân loại (Phân hệ X).
    ///
    /// Đây là cách thư viện thật làm khi dựng danh mục lần đầu: lấy ký hiệu DDC của môn rồi quét kho
    /// theo đúng nhánh đó. Giáo trình đứng đầu là giáo trình chính, các cuốn còn lại là tham khảo.
    /// </summary>
    private async Task SeedDemoCourseDocumentsAsync(
        IReadOnlyList<DemoBib> bibs, CancellationToken ct)
    {
        if (await _db.BibCourses.AnyAsync(ct))
        {
            return;
        }

        var courses = await _db.Courses.ToDictionaryAsync(course => course.Code, ct);

        // Mỗi môn học lấy tài liệu ở một nhánh phân loại; danh sách này bám theo bộ môn học mẫu nạp
        // cùng hệ thống, môn nào của thư viện khác thì cán bộ tự gán trên màn hình Gán tài liệu.
        var mapping = new (string Course, string Ddc)[]
        {
            ("DC101", "004"),
            ("DC201", "519.2"),
            ("CS201", "005.73"),
            ("CS202", "005.74"),
            ("CS301", "004.6"),
            ("CS302", "005.1"),
            ("CS401", "006.3"),
            ("QT201", "658"),
            ("QT301", "658.8"),
            ("MT201", "628"),
            ("MT301", "333.91"),
            ("LU201", "346.597"),
            ("LU301", "346.048")
        };

        var links = 0;

        foreach (var entry in mapping)
        {
            if (!courses.TryGetValue(entry.Course, out var course))
            {
                continue;
            }

            var matching = bibs
                .Where(bib => bib.Ddc == entry.Ddc)
                .OrderBy(bib => bib.Variant)
                .Take(4)
                .ToList();

            foreach (var bib in matching)
            {
                _db.BibCourses.Add(new BibCourse
                {
                    BibId = bib.Record.Id,
                    CourseId = course.Id,
                    RelationType = bib.Variant switch
                    {
                        0 => CourseRelationType.MainTextbook,
                        1 => CourseRelationType.RequiredReference,
                        _ => CourseRelationType.AdditionalReference
                    },
                    Note = bib.Variant == 0 ? "Giáo trình do khoa chỉ định" : null
                });

                links++;
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nối {Count} tài liệu vào môn học", links);
    }

    /// <summary>Ghép một ngày với một giờ trong ngày thành mốc thời gian có múi giờ của hệ thống.</summary>
    /// <summary>
    /// Bước nhảy khi rải lượt mượn lên danh sách bản in.
    ///
    /// Phải nguyên tố cùng nhau với số bản in thì phép chia lấy dư mới quét hết danh sách mà không
    /// lặp lại bản nào.
    /// </summary>
    private static int BuocNhay(int soBan)
    {
        foreach (var buoc in new[] { 7, 11, 13, 17, 19, 23 })
        {
            if (UocChungLonNhat(buoc, soBan) == 1)
            {
                return buoc;
            }
        }

        return 1;
    }

    private static int UocChungLonNhat(int a, int b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a;
    }

    private DateTimeOffset ToMoment(DateOnly date, int hour) =>
        new(date.ToDateTime(new TimeOnly(Math.Clamp(hour, 0, 23), 0)), _clock.Now.Offset);
}
