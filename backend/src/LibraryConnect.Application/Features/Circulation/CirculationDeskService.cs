using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Entities.Cir;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Circulation;

/// <summary>Một lượt ghi mượn cần ghi những gì.</summary>
public class CheckoutRequest
{
    public Guid ReaderId { get; set; }
    public List<string> Barcodes { get; set; } = new();
    public LoanType LoanType { get; set; } = LoanType.TakeHome;
    public LoanChannel Channel { get; set; } = LoanChannel.Desk;
    public string? Note { get; set; }
    /// <summary>Bỏ qua cảnh báo không chặn (nợ phí dưới ngưỡng, thẻ sắp hết hạn…).</summary>
    public bool Force { get; set; }
}

/// <summary>
/// Toàn bộ nghiệp vụ quầy lưu thông (VII.2).
///
/// Đặt ở tầng Application chứ không nằm trong controller vì cùng một nghiệp vụ có ba lối vào: quầy
/// của cán bộ, trang tra cứu của bạn đọc và ứng dụng di động đợt sau. Frontend không được tự tính
/// hạn trả, tiền phạt hay điều kiện gia hạn — mọi con số trên màn hình đều do đây trả về.
/// </summary>
public interface ICirculationDeskService
{
    Task<DeskReaderDto> GetReaderAsync(Guid readerId, CancellationToken ct = default);

    Task<Reader> FindReaderByCardAsync(string cardNumber, CancellationToken ct = default);

    Task<ScanForLoanDto> ScanForLoanAsync(
        Guid readerId, string barcode, IReadOnlyCollection<string>? pending = null,
        CancellationToken ct = default);

    Task<CheckoutResultDto> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default);

    Task<ReturnResultDto> ReturnAsync(
        IReadOnlyList<string> barcodes, string? note, CancellationToken ct = default);

    Task<LoanRowDto> RenewAsync(
        Guid loanId, LoanChannel channel, Guid? requestedByReader, CancellationToken ct = default);
}

public class CirculationDeskService : ICirculationDeskService
{
    /// <summary>Nợ phí vượt ngưỡng này thì chặn mượn tiếp; khai trong Tham số hệ thống.</summary>
    public const string DebtThresholdParameter = "CIRCULATION.DEBT_BLOCK_THRESHOLD";

    /// <summary>Đang giữ tài liệu quá hạn thì có chặn mượn tiếp không.</summary>
    public const string BlockOnOverdueParameter = "CIRCULATION.BLOCK_ON_OVERDUE";

    /// <summary>Cắt hạn trả về ngày hết hạn thẻ.</summary>
    public const string ClampDueToCardParameter = "CIRCULATION.CLAMP_DUE_TO_CARD";

    /// <summary>Cho gia hạn khi tài liệu đã quá hạn hay không.</summary>
    public const string AllowRenewOverdueParameter = "CIRCULATION.ALLOW_RENEW_OVERDUE";

    private readonly IApplicationDbContext _db;
    private readonly ICirculationPolicyResolver _policies;
    private readonly ICirculationCalendarProvider _calendars;
    private readonly ISystemParameterService _parameters;
    private readonly ICodeGenerator _codes;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationSender _notifications;

    public CirculationDeskService(
        IApplicationDbContext db,
        ICirculationPolicyResolver policies,
        ICirculationCalendarProvider calendars,
        ISystemParameterService parameters,
        ICodeGenerator codes,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        INotificationSender notifications)
    {
        _db = db;
        _policies = policies;
        _calendars = calendars;
        _parameters = parameters;
        _codes = codes;
        _currentUser = currentUser;
        _clock = clock;
        _notifications = notifications;
    }

    // -----------------------------------------------------------------------------------------
    // Bạn đọc ở quầy
    // -----------------------------------------------------------------------------------------

    public async Task<Reader> FindReaderByCardAsync(string cardNumber, CancellationToken ct = default)
    {
        var code = cardNumber.Trim();

        // Máy quét đọc được số thẻ, nhưng cán bộ cũng hay gõ tay mã sinh viên — nhận cả hai.
        var reader = await _db.Readers
            .Include(entity => entity.ReaderType)
            .FirstOrDefaultAsync(entity => entity.CardNumber == code || entity.StudentCode == code, ct);

        return reader ?? throw new NotFoundException($"Không tìm thấy bạn đọc mang số thẻ hoặc mã '{code}'.");
    }

    public async Task<DeskReaderDto> GetReaderAsync(Guid readerId, CancellationToken ct = default)
    {
        var today = _clock.Today;

        var reader = await _db.Readers
            .AsNoTracking()
            .Include(entity => entity.ReaderType)
            .Include(entity => entity.Faculty)
            .FirstOrDefaultAsync(entity => entity.Id == readerId, ct)
            ?? throw new NotFoundException("bạn đọc", readerId);

        var policy = await _policies.ResolveAsync(reader.ReaderTypeId, null, null, ct);
        var calendar = await _calendars.GetAsync(null, ct);

        var loans = await LoanQuery.Base(_db)
            .Where(loan => loan.ReaderId == readerId
                           && (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue))
            .OrderBy(loan => loan.DueDate)
            .Select(LoanQuery.Projection)
            .ToListAsync(ct);

        foreach (var row in loans)
        {
            Enrich(row, policy, calendar, today);
        }

        var debt = await OutstandingFinesAsync(readerId, ct);

        var holds = await HoldQuery.Base(_db)
            .Where(hold => hold.ReaderId == readerId && hold.Status == HoldStatus.Ready)
            .Select(HoldQuery.Projection)
            .ToListAsync(ct);

        var dto = new DeskReaderDto
        {
            Id = reader.Id,
            CardNumber = reader.CardNumber,
            StudentCode = reader.StudentCode,
            FullName = reader.FullName,
            ReaderTypeId = reader.ReaderTypeId,
            ReaderTypeName = reader.ReaderType?.Name,
            FacultyName = reader.Faculty?.Name,
            ClassName = reader.ClassName,
            HasPhoto = !string.IsNullOrWhiteSpace(reader.PhotoUrl),
            Status = reader.Status,
            CardExpireDate = reader.CardExpireDate,
            CanBorrow = reader.CanBorrow(today),
            CurrentLoanCount = loans.Count,
            OverdueCount = loans.Count(row => row.Status == LoanStatus.Overdue),
            OutstandingFines = debt,
            MaxItems = policy.MaxItems,
            RemainingQuota = policy.MaxItems - loans.Count,
            CurrentLoans = loans,
            ReadyHolds = holds
        };

        dto.Warnings = await BuildReaderWarningsAsync(reader, dto, today, ct);
        return dto;
    }

    /// <summary>Cảnh báo hiện ngay khi quét thẻ — thứ quyết định cán bộ có cho mượn tiếp hay không.</summary>
    private async Task<List<CirculationWarningDto>> BuildReaderWarningsAsync(
        Reader reader, DeskReaderDto dto, DateOnly today, CancellationToken ct)
    {
        var warnings = new List<CirculationWarningDto>();

        switch (reader.Status)
        {
            case ReaderStatus.Suspended:
            case ReaderStatus.Locked:
                warnings.Add(new CirculationWarningDto(CirculationWarnings.ReaderLocked,
                    $"Thẻ đang bị khóa: {reader.StatusReason ?? "không ghi lý do"}.", true));
                break;
            case ReaderStatus.Graduated:
                warnings.Add(new CirculationWarningDto(CirculationWarnings.ReaderGraduated,
                    "Bạn đọc đã ra trường, không còn quyền mượn tài liệu.", true));
                break;
        }

        if (reader.CardExpireDate < today)
        {
            warnings.Add(new CirculationWarningDto(CirculationWarnings.CardExpired,
                $"Thẻ hết hạn ngày {reader.CardExpireDate:dd/MM/yyyy}.", true));
        }
        else if (reader.CardExpireDate <= today.AddDays(30))
        {
            warnings.Add(new CirculationWarningDto(CirculationWarnings.CardExpiringSoon,
                $"Thẻ sắp hết hạn ngày {reader.CardExpireDate:dd/MM/yyyy}.", false));
        }

        if (dto.OutstandingFines > 0)
        {
            var threshold = await _parameters.GetAsync(DebtThresholdParameter, 50_000m, ct);

            warnings.Add(new CirculationWarningDto(CirculationWarnings.Debt,
                $"Bạn đọc còn nợ {dto.OutstandingFines:#,##0} đ tiền phạt.",
                threshold > 0 && dto.OutstandingFines > threshold));
        }

        if (dto.OverdueCount > 0)
        {
            var block = await _parameters.GetAsync(BlockOnOverdueParameter, true, ct);

            warnings.Add(new CirculationWarningDto(CirculationWarnings.OverdueLoans,
                $"Đang giữ {dto.OverdueCount} tài liệu quá hạn.", block));
        }

        if (dto.RemainingQuota <= 0)
        {
            warnings.Add(new CirculationWarningDto(CirculationWarnings.LimitReached,
                $"Đã mượn đủ {dto.MaxItems} tài liệu theo chính sách.", true));
        }

        foreach (var hold in dto.ReadyHolds)
        {
            warnings.Add(new CirculationWarningDto(CirculationWarnings.HoldReady,
                $"Có tài liệu đặt giữ đang chờ nhận: {hold.Title}.", false));
        }

        return warnings;
    }

    private async Task<decimal> OutstandingFinesAsync(Guid readerId, CancellationToken ct) =>
        await _db.Fines
            .Where(fine => fine.ReaderId == readerId && !fine.Waived)
            .SumAsync(fine => (decimal?)(fine.Amount - fine.PaidAmount), ct) ?? 0;

    // -----------------------------------------------------------------------------------------
    // Quét mã vạch để ghi mượn
    // -----------------------------------------------------------------------------------------

    public async Task<ScanForLoanDto> ScanForLoanAsync(
        Guid readerId,
        string barcode,
        IReadOnlyCollection<string>? pending = null,
        CancellationToken ct = default)
    {
        var reader = await GetReaderAsync(readerId, ct);
        var code = barcode.Trim();

        var result = new ScanForLoanDto { Barcode = code };

        var item = await _db.Items
            .AsNoTracking()
            .Where(entity => entity.Barcode == code || entity.RegisterNumber == code)
            .Select(entity => new
            {
                entity.Id,
                entity.Barcode,
                entity.RegisterNumber,
                entity.Status,
                entity.IsLocked,
                entity.LockReason,
                entity.CallNumber,
                entity.WarehouseId,
                WarehouseName = entity.Warehouse!.Name,
                WarehouseClosed = entity.Warehouse!.IsClosedForInventory,
                entity.BibId,
                Title = entity.Bib!.Title,
                Author = entity.Bib!.AuthorMain,
                DocumentTypeId = entity.Bib!.DocumentTypeId,
                DocumentTypeName = entity.Bib!.DocumentType!.Name
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
        {
            result.Warnings.Add(new CirculationWarningDto(CirculationWarnings.ItemNotFound,
                $"Không tìm thấy ấn phẩm có mã vạch '{code}'.", true));
            return result;
        }

        result.ItemId = item.Id;
        result.Barcode = item.Barcode;
        result.RegisterNumber = item.RegisterNumber;
        result.Title = item.Title;
        result.Author = item.Author;
        result.CallNumber = item.CallNumber;
        result.WarehouseName = item.WarehouseName;
        result.DocumentTypeName = item.DocumentTypeName;
        result.ItemStatus = item.Status;

        var policy = await _policies.ResolveAsync(
            reader.ReaderTypeId, item.DocumentTypeId, item.WarehouseId, ct);

        result.PolicyName = policy.Name;
        result.AllowTakeHome = policy.AllowTakeHome;

        // Cảnh báo của bạn đọc đi kèm luôn, vì cán bộ quét mã vạch mới là lúc quyết định.
        result.Warnings.AddRange(reader.Warnings.Where(warning => warning.Blocking));

        if (pending is not null
            && pending.Any(existing => string.Equals(existing, item.Barcode, StringComparison.OrdinalIgnoreCase)))
        {
            result.Warnings.Add(new CirculationWarningDto(CirculationWarnings.AlreadyInList,
                "Mã vạch này đã có trong danh sách đang ghi mượn.", true));
        }

        if (item.IsLocked)
        {
            result.Warnings.Add(new CirculationWarningDto(CirculationWarnings.ItemLocked,
                $"Ấn phẩm đang bị khóa lưu thông: {item.LockReason ?? "không ghi lý do"}.", true));
        }

        // III.4 bước 1: kho đã đóng để kiểm kê thì mọi bản trong kho đứng yên tới khi chốt kỳ.
        if (item.WarehouseClosed)
        {
            result.Warnings.Add(new CirculationWarningDto(CirculationWarnings.WarehouseClosed,
                $"Kho {item.WarehouseName} đang đóng để kiểm kê, không ghi mượn được.", true));
        }

        if (!policy.AllowLoan)
        {
            result.Warnings.Add(new CirculationWarningDto(CirculationWarnings.PolicyForbidsLoan,
                $"Chính sách \"{policy.Name}\" không cho mượn loại tài liệu này.", true));
        }

        switch (item.Status)
        {
            case ItemStatus.InStock:
                break;

            case ItemStatus.OnHoldShelf:
                // Bản đang giữ cho người khác thì không được đưa cho người đang đứng trước quầy.
                var holder = await _db.Holds
                    .AsNoTracking()
                    .Where(hold => hold.ItemId == item.Id && hold.Status == HoldStatus.Ready)
                    .Select(hold => new { hold.ReaderId, hold.Reader!.FullName })
                    .FirstOrDefaultAsync(ct);

                if (holder is not null && holder.ReaderId != readerId)
                {
                    result.Warnings.Add(new CirculationWarningDto(CirculationWarnings.ItemHeldForOther,
                        $"Bản này đang giữ cho bạn đọc {holder.FullName}.", true));
                }

                break;

            case ItemStatus.OnLoan:
                result.Warnings.Add(new CirculationWarningDto(CirculationWarnings.ItemOnLoan,
                    "Ấn phẩm đang có người mượn, chưa ghi trả.", true));
                break;

            default:
                result.Warnings.Add(new CirculationWarningDto(CirculationWarnings.ItemNotAvailable,
                    $"Ấn phẩm đang ở trạng thái {ItemStatusText(item.Status)}, không cho mượn được.", true));
                break;
        }

        var clamp = await _parameters.GetAsync(ClampDueToCardParameter, true, ct);
        var calendar = await _calendars.GetAsync(null, ct);

        result.DueDate = CirculationRules.DueDate(
            _clock.Today, policy.LoanDays, calendar, clamp ? reader.CardExpireDate : null);

        result.Allowed = !result.Warnings.Any(warning => warning.Blocking);
        return result;
    }

    private static string ItemStatusText(ItemStatus status) => status switch
    {
        ItemStatus.PendingInspection => "chưa kiểm nhận",
        ItemStatus.InStock => "trong kho",
        ItemStatus.OnLoan => "đang mượn",
        ItemStatus.OnHoldShelf => "đặt giữ",
        ItemStatus.Lost => "mất",
        ItemStatus.Damaged => "hỏng",
        ItemStatus.Discarded => "thanh lý",
        ItemStatus.UnderInventory => "đang kiểm kê",
        _ => status.ToString()
    };

    // -----------------------------------------------------------------------------------------
    // Ghi mượn
    // -----------------------------------------------------------------------------------------

    public async Task<CheckoutResultDto> CheckoutAsync(
        CheckoutRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _clock.Now;
        var today = _clock.Today;

        var reader = await _db.Readers
            .Include(entity => entity.ReaderType)
            .FirstOrDefaultAsync(entity => entity.Id == request.ReaderId, ct)
            ?? throw new NotFoundException("bạn đọc", request.ReaderId);

        var snapshot = await GetReaderAsync(reader.Id, ct);

        var blocking = snapshot.Warnings
            .Where(warning => warning.Blocking && warning.Code != CirculationWarnings.LimitReached)
            .ToList();

        if (blocking.Count > 0 && !request.Force)
        {
            throw new ConflictException(string.Join(" ", blocking.Select(warning => warning.Message)));
        }

        var barcodes = request.Barcodes
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (barcodes.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("barcodes", "Chưa quét mã vạch nào.");
        }

        var calendar = await _calendars.GetAsync(null, ct);
        var clamp = await _parameters.GetAsync(ClampDueToCardParameter, true, ct);

        var result = new CheckoutResultDto { ReaderId = reader.Id, ReaderName = reader.FullName };
        var remaining = snapshot.RemainingQuota;
        var created = new List<Loan>();

        foreach (var barcode in barcodes)
        {
            var item = await _db.Items
                .Include(entity => entity.Bib)
                .FirstOrDefaultAsync(entity => entity.Barcode == barcode || entity.RegisterNumber == barcode, ct);

            if (item is null)
            {
                result.Failures.Add(new CirculationFailureDto(barcode,
                    $"Không tìm thấy ấn phẩm có mã vạch '{barcode}'."));
                continue;
            }

            var scan = await ScanForLoanAsync(reader.Id, barcode, ct: ct);

            // Cảnh báo về hạn mức được xét lại theo số bản đã ghi trong chính lượt này.
            var blockers = scan.Warnings
                .Where(warning => warning.Blocking && warning.Code != CirculationWarnings.LimitReached)
                .ToList();

            if (blockers.Count > 0)
            {
                result.Failures.Add(new CirculationFailureDto(barcode,
                    string.Join(" ", blockers.Select(warning => warning.Message))));
                continue;
            }

            if (remaining <= 0)
            {
                result.Failures.Add(new CirculationFailureDto(barcode,
                    $"Bạn đọc đã mượn đủ {snapshot.MaxItems} tài liệu theo chính sách."));
                continue;
            }

            var policy = await _policies.ResolveAsync(
                reader.ReaderTypeId, item.Bib?.DocumentTypeId, item.WarehouseId, ct);

            var loanType = request.LoanType;

            if (loanType == LoanType.TakeHome && !policy.AllowTakeHome)
            {
                // Kho đọc tại chỗ vẫn ghi mượn được, nhưng phải ghi đúng là mượn tại chỗ.
                loanType = LoanType.InHouse;
            }

            var loan = new Loan
            {
                Code = await _codes.NextAsync("LOAN", ct),
                ReaderId = reader.Id,
                ItemId = item.Id,
                BibId = item.BibId,
                BibTitle = item.Bib?.Title,
                Barcode = item.Barcode,
                LoanDate = now,
                DueDate = CirculationRules.DueDate(
                    today, policy.LoanDays, calendar, clamp ? reader.CardExpireDate : null),
                Status = LoanStatus.Active,
                LoanType = loanType,
                Channel = request.Channel,
                LoanBy = _currentUser.UserId,
                LoanByName = _currentUser.FullName,
                PolicyId = policy.PolicyId,
                Note = request.Note
            };

            _db.Loans.Add(loan);
            created.Add(loan);

            item.Status = ItemStatus.OnLoan;
            item.LoanCount++;

            // Đếm cả ở biểu ghi, không chỉ ở từng bản in: trang tra cứu xếp "sách được mượn nhiều"
            // và tính độ liên quan theo con số của biểu ghi, nên chỉ tăng ở bản in thì khối ấy trống
            // mãi dù thư viện cho mượn hàng nghìn lượt.
            if (item.Bib is not null)
            {
                item.Bib.LoanCount++;
            }

            reader.CurrentLoanCount++;
            reader.TotalLoanCount++;
            remaining--;

            await FulfilHoldAsync(reader.Id, item.Id, item.BibId, now, ct);
        }

        if (created.Count == 0)
        {
            throw new ConflictException(result.Failures.Count > 0
                ? string.Join(" ", result.Failures.Select(failure => $"{failure.Barcode}: {failure.Message}"))
                : "Không ghi mượn được tài liệu nào.");
        }

        await _db.SaveChangesAsync(ct);

        var ids = created.Select(loan => loan.Id).ToList();

        result.Loans = await LoanQuery.Base(_db)
            .Where(loan => ids.Contains(loan.Id))
            .Select(LoanQuery.Projection)
            .ToListAsync(ct);

        result.SlipCode = created[0].Code;
        return result;
    }

    /// <summary>Bạn đọc nhận đúng bản mình đã đặt giữ thì phiếu đặt giữ coi như hoàn tất.</summary>
    private async Task FulfilHoldAsync(
        Guid readerId, Guid itemId, Guid bibId, DateTimeOffset now, CancellationToken ct)
    {
        var hold = await _db.Holds
            .Where(entity => entity.ReaderId == readerId
                             && entity.BibId == bibId
                             && (entity.ItemId == null || entity.ItemId == itemId)
                             && (entity.Status == HoldStatus.Ready || entity.Status == HoldStatus.Waiting))
            .OrderByDescending(entity => entity.Status == HoldStatus.Ready)
            .ThenBy(entity => entity.QueuePosition)
            .FirstOrDefaultAsync(ct);

        if (hold is null)
        {
            return;
        }

        hold.Status = HoldStatus.Fulfilled;
        hold.ItemId = itemId;
        hold.FulfilledAt = now;

        await HoldReader.ResequenceAsync(_db, hold.BibId, ct);
    }

    // -----------------------------------------------------------------------------------------
    // Ghi trả
    // -----------------------------------------------------------------------------------------

    public async Task<ReturnResultDto> ReturnAsync(
        IReadOnlyList<string> barcodes, string? note, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(barcodes);

        var now = _clock.Now;
        var today = _clock.Today;
        var calendar = await _calendars.GetAsync(null, ct);

        var result = new ReturnResultDto();

        foreach (var raw in barcodes.Select(value => value.Trim()).Where(value => value.Length > 0))
        {
            var item = await _db.Items
                .Include(entity => entity.Bib)
                .Include(entity => entity.Warehouse)
                .FirstOrDefaultAsync(entity => entity.Barcode == raw || entity.RegisterNumber == raw, ct);

            if (item is null)
            {
                result.Failures.Add(new CirculationFailureDto(raw,
                    $"Không tìm thấy ấn phẩm có mã vạch '{raw}'."));
                continue;
            }

            var loan = await _db.Loans
                .Include(entity => entity.Reader)
                .Where(entity => entity.ItemId == item.Id
                                 && (entity.Status == LoanStatus.Active || entity.Status == LoanStatus.Overdue))
                .OrderByDescending(entity => entity.LoanDate)
                .FirstOrDefaultAsync(ct);

            if (loan is null)
            {
                result.Failures.Add(new CirculationFailureDto(raw,
                    $"Ấn phẩm '{item.Barcode}' không có lượt mượn nào đang mở."));
                continue;
            }

            var policy = await _policies.ResolveAsync(
                loan.Reader?.ReaderTypeId, item.Bib?.DocumentTypeId, item.WarehouseId, ct);

            var fine = CirculationRules.OverdueFine(loan.DueDate, today, policy, calendar);
            var overdueDays = CirculationRules.ChargeableOverdueDays(
                loan.DueDate, today, policy.GraceDays, calendar);

            loan.ReturnDate = now;
            loan.Status = LoanStatus.Returned;
            loan.ReturnBy = _currentUser.UserId;
            loan.ReturnByName = _currentUser.FullName;
            loan.FineAmount = fine;

            if (!string.IsNullOrWhiteSpace(note))
            {
                loan.Note = string.IsNullOrWhiteSpace(loan.Note) ? note : $"{loan.Note}\n{note}";
            }

            if (loan.Reader is not null && loan.Reader.CurrentLoanCount > 0)
            {
                loan.Reader.CurrentLoanCount--;
            }

            var row = new ReturnedItemDto
            {
                LoanId = loan.Id,
                LoanCode = loan.Code,
                Barcode = item.Barcode,
                Title = item.Bib?.Title,
                ReaderId = loan.ReaderId,
                ReaderName = loan.Reader?.FullName ?? string.Empty,
                ReaderCardNumber = loan.Reader?.CardNumber ?? string.Empty,
                DueDate = loan.DueDate,
                OverdueDays = overdueDays,
                Fine = fine
            };

            if (fine > 0)
            {
                var fineRow = new Fine
                {
                    Code = await _codes.NextAsync("FINE", ct),
                    ReaderId = loan.ReaderId,
                    LoanId = loan.Id,
                    Type = FineType.Overdue,
                    Amount = fine,
                    Note = $"Quá hạn {overdueDays} ngày, tài liệu {item.Barcode}."
                };

                _db.Fines.Add(fineRow);
                row.FineCode = fineRow.Code;
                result.TotalFine += fine;
            }

            // Có người đang đợi thì bản này ở lại quầy chứ không lên giá.
            var nextHold = await NextWaitingHoldAsync(item, ct);

            if (nextHold is not null)
            {
                item.Status = ItemStatus.OnHoldShelf;
                nextHold.Status = HoldStatus.Ready;
                nextHold.ItemId = item.Id;
                nextHold.NotifiedAt = now;
                nextHold.ExpireDate = now.AddDays(Math.Max(1, policy.HoldExpireDays));

                row.HoldWaiting = true;
                row.HoldForReaderName = await _db.Readers
                    .Where(reader => reader.Id == nextHold.ReaderId)
                    .Select(reader => reader.FullName)
                    .FirstOrDefaultAsync(ct);

                row.HoldPickupWarehouse = await _db.Warehouses
                    .Where(warehouse => warehouse.Id == (nextHold.PickupWarehouseId ?? item.WarehouseId))
                    .Select(warehouse => warehouse.Name)
                    .FirstOrDefaultAsync(ct);

                row.Warnings.Add(new CirculationWarningDto(CirculationWarnings.HoldReady,
                    $"Giữ lại tại quầy cho bạn đọc {row.HoldForReaderName}, hạn nhận đến " +
                    $"{nextHold.ExpireDate:dd/MM/yyyy}.", false));
            }
            else
            {
                item.Status = ItemStatus.InStock;
            }

            // Kho đang kiểm kê vẫn nhận trả — bạn đọc mang sách tới thì không đuổi về được — nhưng bản
            // này ở lại quầy chứ không lên giá, để danh sách kỳ vọng của kỳ kiểm kê không bị xáo.
            if (item.Warehouse?.IsClosedForInventory == true)
            {
                row.Warnings.Add(new CirculationWarningDto(CirculationWarnings.WarehouseClosed,
                    $"Kho {item.Warehouse.Name} đang đóng để kiểm kê: giữ bản này ở quầy, chưa xếp lên giá.",
                    false));
            }

            result.Items.Add(row);

            if (nextHold is not null)
            {
                await _db.SaveChangesAsync(ct);

                await _notifications.SendAsync(nextHold.ReaderId,
                    NotificationKinds.HoldReady,
                    "Tài liệu đặt giữ đã sẵn sàng",
                    $"Tài liệu \"{item.Bib?.Title}\" bạn đặt giữ đã có tại {row.HoldPickupWarehouse}. " +
                    $"Vui lòng đến nhận trước ngày {nextHold.ExpireDate:dd/MM/yyyy}.",
                    "/tai-khoan",
                    new Dictionary<string, string> { ["holdId"] = nextHold.Id.ToString() },
                    ct);
            }
        }

        if (result.Items.Count == 0)
        {
            throw new ConflictException(result.Failures.Count > 0
                ? string.Join(" ", result.Failures.Select(failure => $"{failure.Barcode}: {failure.Message}"))
                : "Không ghi trả được tài liệu nào.");
        }

        await _db.SaveChangesAsync(ct);

        result.SlipCode = result.Items[0].LoanCode;
        return result;
    }

    private async Task<Hold?> NextWaitingHoldAsync(Item item, CancellationToken ct) =>
        await _db.Holds
            .Where(hold => hold.Status == HoldStatus.Waiting
                           && hold.BibId == item.BibId
                           && (hold.ItemId == null || hold.ItemId == item.Id))
            .OrderBy(hold => hold.QueuePosition)
            .ThenBy(hold => hold.HoldDate)
            .FirstOrDefaultAsync(ct);

    // -----------------------------------------------------------------------------------------
    // Gia hạn
    // -----------------------------------------------------------------------------------------

    public async Task<LoanRowDto> RenewAsync(
        Guid loanId, LoanChannel channel, Guid? requestedByReader, CancellationToken ct = default)
    {
        var today = _clock.Today;
        var now = _clock.Now;

        var loan = await _db.Loans
            .Include(entity => entity.Reader)
            .Include(entity => entity.Item)
            .ThenInclude(item => item!.Bib)
            .FirstOrDefaultAsync(entity => entity.Id == loanId, ct)
            ?? throw new NotFoundException("lượt mượn", loanId);

        if (requestedByReader is not null && loan.ReaderId != requestedByReader)
        {
            throw new ForbiddenException("Lượt mượn này không thuộc về bạn đọc đang đăng nhập.");
        }

        if (loan.Status is not (LoanStatus.Active or LoanStatus.Overdue))
        {
            throw new ConflictException("Lượt mượn đã kết thúc nên không gia hạn được.");
        }

        var policy = await _policies.ResolveAsync(
            loan.Reader?.ReaderTypeId, loan.Item?.Bib?.DocumentTypeId, loan.Item?.WarehouseId, ct);

        if (!policy.AllowRenew)
        {
            throw new ConflictException($"Chính sách \"{policy.Name}\" không cho gia hạn loại tài liệu này.");
        }

        if (loan.RenewedCount >= policy.MaxRenewals)
        {
            throw new ConflictException(
                $"Đã gia hạn đủ {policy.MaxRenewals} lần theo chính sách \"{policy.Name}\".");
        }

        var allowOverdue = await _parameters.GetAsync(AllowRenewOverdueParameter, false, ct);

        if (!allowOverdue && loan.DueDate < today)
        {
            throw new ConflictException(
                $"Tài liệu đã quá hạn từ ngày {loan.DueDate:dd/MM/yyyy}, phải trả rồi mượn lại.");
        }

        var waiting = await _db.Holds.AnyAsync(hold =>
            hold.BibId == loan.BibId
            && hold.ReaderId != loan.ReaderId
            && (hold.Status == HoldStatus.Waiting || hold.Status == HoldStatus.Ready), ct);

        if (waiting)
        {
            throw new ConflictException("Có bạn đọc khác đang đặt giữ tài liệu này nên không gia hạn được.");
        }

        if (loan.Reader is not null && !loan.Reader.CanBorrow(today))
        {
            throw new ConflictException("Thẻ bạn đọc đang hết hạn hoặc bị khóa nên không gia hạn được.");
        }

        var calendar = await _calendars.GetAsync(null, ct);
        var clamp = await _parameters.GetAsync(ClampDueToCardParameter, true, ct);

        // Gia hạn tính từ hôm nay chứ không nối vào hạn cũ: bạn đọc gia hạn sớm hay muộn đều được
        // đúng số ngày mà chính sách cho.
        var newDue = CirculationRules.DueDate(
            today, policy.RenewalDays, calendar, clamp ? loan.Reader?.CardExpireDate : null);

        if (newDue <= loan.DueDate)
        {
            throw new ConflictException(
                $"Hạn trả mới ({newDue:dd/MM/yyyy}) không dài hơn hạn hiện tại ({loan.DueDate:dd/MM/yyyy}).");
        }

        var renewal = new LoanRenewal
        {
            LoanId = loan.Id,
            RenewalDate = now,
            OldDueDate = loan.DueDate,
            NewDueDate = newDue,
            RequestedBy = requestedByReader ?? _currentUser.UserId,
            Channel = channel
        };

        // Thư viện có thể bắt duyệt gia hạn từ xa; ở quầy thì cán bộ đã là người duyệt.
        //
        // Gửi yêu cầu là một việc **thành công**, nên trả về dòng phiếu (hạn trả giữ nguyên) kèm cờ
        // RenewalPending, không ném ConflictException. Trước 04/09/2026 chỗ này ném lỗi nên máy chủ
        // trả 409 và trang tra cứu hiện thông báo đỏ cho một thao tác đã chạy đúng.
        if (policy.RequireRenewalApproval && channel != LoanChannel.Desk)
        {
            renewal.Status = AccessRequestStatus.Pending;
            _db.LoanRenewals.Add(renewal);
            await _db.SaveChangesAsync(ct);

            var pending = await LoanQuery.Base(_db)
                .Where(entity => entity.Id == loan.Id)
                .Select(LoanQuery.Projection)
                .FirstAsync(ct);

            Enrich(pending, policy, calendar, today);
            pending.RenewalPending = true;
            return pending;
        }

        renewal.Status = AccessRequestStatus.Approved;
        renewal.ApprovedBy = _currentUser.UserId;
        _db.LoanRenewals.Add(renewal);

        loan.DueDate = newDue;
        loan.RenewedCount++;
        loan.Status = LoanStatus.Active;

        await _db.SaveChangesAsync(ct);

        var row = await LoanQuery.Base(_db)
            .Where(entity => entity.Id == loan.Id)
            .Select(LoanQuery.Projection)
            .FirstAsync(ct);

        Enrich(row, policy, calendar, today);
        return row;
    }

    // -----------------------------------------------------------------------------------------
    // Bổ sung số liệu tính toán vào một dòng mượn
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Điền các con số phải tính: trạng thái theo hôm nay, số ngày quá hạn và tiền phạt dự kiến.
    /// </summary>
    public static void Enrich(
        LoanRowDto row, EffectivePolicy policy, CirculationCalendar calendar, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(row);

        var end = row.ReturnDate is null
            ? today
            : DateOnly.FromDateTime(row.ReturnDate.Value.LocalDateTime);

        row.MaxRenewals = policy.MaxRenewals;

        if (row.Status is LoanStatus.Active or LoanStatus.Overdue)
        {
            row.Status = row.DueDate < today ? LoanStatus.Overdue : LoanStatus.Active;
        }

        row.OverdueDays = CirculationRules.ChargeableOverdueDays(
            row.DueDate, end, policy.GraceDays, calendar);

        row.EstimatedFine = row.ReturnDate is null
            ? CirculationRules.OverdueFine(row.DueDate, today, policy, calendar)
            : row.FineAmount;
    }
}
