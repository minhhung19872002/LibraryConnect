using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Acquisition;

// ---------------------------------------------------------------------------------------------
// III.4 — Quản lý kiểm kê. Đúng thứ tự nghiệp vụ: đóng kho, tạo kỳ và chốt danh sách kỳ vọng, quét,
// chốt kỳ, ra kết quả.
// ---------------------------------------------------------------------------------------------

/// <summary>Đóng hoặc mở kho. Kho đóng thì ngưng cho mượn, trả và chuyển kho tại kho đó.</summary>
public record SetWarehouseClosedCommand(Guid WarehouseId, bool Closed) : IRequest;

public class SetWarehouseClosedCommandHandler : IRequestHandler<SetWarehouseClosedCommand>
{
    private readonly IApplicationDbContext _db;

    public SetWarehouseClosedCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(SetWarehouseClosedCommand command, CancellationToken ct)
    {
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(entity => entity.Id == command.WarehouseId, ct)
            ?? throw new NotFoundException("kho", command.WarehouseId);

        if (!command.Closed)
        {
            var running = await _db.InventoryPeriods.AnyAsync(
                period => period.WarehouseId == warehouse.Id
                          && period.Status != InventoryPeriodStatus.Closed, ct);

            if (running)
            {
                throw new ConflictException(
                    $"Kho {warehouse.Name} còn kỳ kiểm kê chưa chốt. Hãy chốt kỳ kiểm kê trước khi mở kho.");
            }
        }

        warehouse.IsClosedForInventory = command.Closed;
        await _db.SaveChangesAsync(ct);
    }
}

public class InventoryPeriodDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string ScopeType { get; set; } = "ALL";
    public string? ScopeFrom { get; set; }
    public string? ScopeTo { get; set; }
    public Guid? ScopeDocumentTypeId { get; set; }
    public string? ScopeDocumentTypeName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public InventoryPeriodStatus Status { get; set; }
    /// <summary>Tên cán bộ viết liền một dòng, dùng cho bản in.</summary>
    public string? AssignedStaff { get; set; }
    /// <summary>Cán bộ được phân công, theo tài khoản. Rỗng với kỳ lập trước 04/09/2026.</summary>
    public List<AssignedStaffDto> AssignedUsers { get; set; } = new();
    public int ExpectedCount { get; set; }
    public int ScannedCount { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? Note { get; set; }
    /// <summary>Kho có đang bị đóng để kiểm kê hay không.</summary>
    public bool WarehouseClosed { get; set; }
}

public record AssignedStaffDto(Guid UserId, string FullName);

public class InventoryPeriodListRequest : PagedRequest
{
    public Guid? WarehouseId { get; set; }
    public InventoryPeriodStatus? Status { get; set; }
}

public record SearchInventoryPeriodsQuery(InventoryPeriodListRequest Request)
    : IRequest<PagedResult<InventoryPeriodDto>>;

public class SearchInventoryPeriodsQueryHandler
    : IRequestHandler<SearchInventoryPeriodsQuery, PagedResult<InventoryPeriodDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchInventoryPeriodsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<InventoryPeriodDto>> Handle(
        SearchInventoryPeriodsQuery query, CancellationToken ct)
    {
        var request = query.Request;

        var periods = _db.InventoryPeriods
            .AsNoTracking()
            .WhereIf(request.WarehouseId is not null, period => period.WarehouseId == request.WarehouseId)
            .WhereIf(request.Status is not null, period => period.Status == request.Status);

        if (request.HasKeyword())
        {
            var keyword = request.Keyword!.Trim().ToLowerInvariant();

            periods = periods.Where(period =>
                period.Code.ToLower().Contains(keyword)
                || DatabaseFunctions.Unaccent(period.Name).Contains(
                    Common.Text.VietnameseText.RemoveDiacritics(keyword)));
        }

        var total = await periods.CountAsync(ct);

        var page = await periods
            .OrderByDescending(period => period.StartDate)
            .ThenByDescending(period => period.Code)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(Projection)
            .ToListAsync(ct);

        return new PagedResult<InventoryPeriodDto>(page, total, request.Page, request.PageSize);
    }

    internal static System.Linq.Expressions.Expression<Func<InventoryPeriod, InventoryPeriodDto>> Projection =>
        period => new InventoryPeriodDto
        {
            Id = period.Id,
            Code = period.Code,
            Name = period.Name,
            WarehouseId = period.WarehouseId,
            WarehouseName = period.Warehouse!.Name,
            ScopeType = period.ScopeType,
            ScopeFrom = period.ScopeFrom,
            ScopeTo = period.ScopeTo,
            ScopeDocumentTypeId = period.ScopeDocumentTypeId,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            Status = period.Status,
            AssignedStaff = period.AssignedStaff,
            ExpectedCount = period.ExpectedCount,
            ScannedCount = period.ScannedCount,
            ClosedAt = period.ClosedAt,
            Note = period.Note,
            WarehouseClosed = period.Warehouse!.IsClosedForInventory
        };
}

public record GetInventoryPeriodQuery(Guid Id) : IRequest<InventoryPeriodDto>;

public class GetInventoryPeriodQueryHandler : IRequestHandler<GetInventoryPeriodQuery, InventoryPeriodDto>
{
    private readonly IApplicationDbContext _db;

    public GetInventoryPeriodQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<InventoryPeriodDto> Handle(GetInventoryPeriodQuery query, CancellationToken ct)
    {
        var period = await _db.InventoryPeriods
            .AsNoTracking()
            .Where(entity => entity.Id == query.Id)
            .Select(SearchInventoryPeriodsQueryHandler.Projection)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("kỳ kiểm kê", query.Id);

        if (period.ScopeDocumentTypeId is not null)
        {
            period.ScopeDocumentTypeName = await _db.DocumentTypes
                .AsNoTracking()
                .Where(type => type.Id == period.ScopeDocumentTypeId)
                .Select(type => type.Name)
                .FirstOrDefaultAsync(ct);
        }

        // Nối tên cán bộ ở một câu hỏi riêng chứ không nhét vào phép chiếu dùng chung: danh sách kỳ
        // kiểm kê hiện chuỗi tên đã chép sẵn, chỉ màn hình chi tiết mới cần từng tài khoản.
        period.AssignedUsers = await _db.InventoryPeriodStaff
            .AsNoTracking()
            .Where(row => row.PeriodId == query.Id)
            .Join(_db.Users, row => row.UserId, user => user.Id, (row, user) => user)
            // Sắp xếp trên cột thật rồi mới dựng bản ghi: sắp trên thuộc tính của một kiểu vừa dựng
            // ra thì bộ dịch LINQ không đưa xuống SQL được.
            .OrderBy(user => user.FullName)
            .Select(user => new AssignedStaffDto(user.Id, user.FullName))
            .ToListAsync(ct);

        return period;
    }
}

/// <summary>Tạo kỳ kiểm kê và chốt danh sách ĐKCB kỳ vọng (III.4 bước 2).</summary>
public class CreateInventoryPeriodCommand : IRequest<Guid>
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    /// <summary>ALL | RANGE | DOCTYPE</summary>
    public string ScopeType { get; set; } = "ALL";
    public string? ScopeFrom { get; set; }
    public string? ScopeTo { get; set; }
    public Guid? ScopeDocumentTypeId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    /// <summary>Tài khoản cán bộ được phân công. Tên của họ được chép sang ô in ra biên bản.</summary>
    public List<Guid> AssignedUserIds { get; set; } = new();
    /// <summary>Chỉ dùng khi phân công người ngoài danh sách tài khoản (cán bộ đơn vị khác đến hỗ trợ).</summary>
    public string? AssignedStaff { get; set; }
    public string? Note { get; set; }
    /// <summary>Đóng kho ngay khi tạo kỳ. Bỏ qua thì phải đóng tay trước khi quét.</summary>
    public bool CloseWarehouse { get; set; } = true;
}

public class CreateInventoryPeriodCommandValidator : AbstractValidator<CreateInventoryPeriodCommand>
{
    public CreateInventoryPeriodCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().WithMessage("Chưa nhập tên kỳ kiểm kê.").MaximumLength(300);
        RuleFor(command => command.WarehouseId).NotEmpty().WithMessage("Chưa chọn kho kiểm kê.");

        RuleFor(command => command.ScopeType)
            .Must(type => type is "ALL" or "RANGE" or "DOCTYPE")
            .WithMessage("Phạm vi kiểm kê phải là toàn kho, theo khoảng ĐKCB hoặc theo dạng tài liệu.");

        RuleFor(command => command)
            .Must(command => command.ScopeType != "RANGE"
                             || (!string.IsNullOrWhiteSpace(command.ScopeFrom)
                                 && !string.IsNullOrWhiteSpace(command.ScopeTo)))
            .WithMessage("Kiểm kê theo khoảng ĐKCB thì phải nhập số ĐKCB từ và đến.")
            .Must(command => command.ScopeType != "DOCTYPE" || command.ScopeDocumentTypeId is not null)
            .WithMessage("Kiểm kê theo dạng tài liệu thì phải chọn dạng tài liệu.")
            .Must(command => command.EndDate is null || command.StartDate is null
                             || command.EndDate >= command.StartDate)
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
    }
}

public class CreateInventoryPeriodCommandHandler : IRequestHandler<CreateInventoryPeriodCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly IDateTimeProvider _clock;
    private readonly IStaffNotifier _notifier;

    public CreateInventoryPeriodCommandHandler(
        IApplicationDbContext db,
        ICodeGenerator codes,
        IDateTimeProvider clock,
        IStaffNotifier notifier)
    {
        _db = db;
        _codes = codes;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task<Guid> Handle(CreateInventoryPeriodCommand command, CancellationToken ct)
    {
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(entity => entity.Id == command.WarehouseId, ct)
            ?? throw new NotFoundException("kho", command.WarehouseId);

        var running = await _db.InventoryPeriods.AnyAsync(
            period => period.WarehouseId == warehouse.Id && period.Status != InventoryPeriodStatus.Closed, ct);

        if (running)
        {
            throw new ConflictException(
                $"Kho {warehouse.Name} đang có một kỳ kiểm kê chưa chốt. Hãy chốt kỳ đó trước.");
        }

        var code = string.IsNullOrWhiteSpace(command.Code)
            ? await _codes.NextAsync("INVENTORY", ct)
            : command.Code.Trim().ToUpperInvariant();

        if (await _db.InventoryPeriods.AnyAsync(period => period.Code == code, ct))
        {
            throw new Common.Exceptions.ValidationException("code", $"Mã kỳ kiểm kê '{code}' đã được dùng.");
        }

        var period = new InventoryPeriod
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = command.Name.Trim(),
            WarehouseId = warehouse.Id,
            ScopeType = command.ScopeType,
            ScopeFrom = command.ScopeFrom?.Trim(),
            ScopeTo = command.ScopeTo?.Trim(),
            ScopeDocumentTypeId = command.ScopeDocumentTypeId,
            StartDate = command.StartDate ?? _clock.Today,
            EndDate = command.EndDate,
            Status = InventoryPeriodStatus.InProgress,
            AssignedStaff = command.AssignedStaff?.Trim(),
            Note = command.Note?.Trim()
        };

        _db.InventoryPeriods.Add(period);

        // Chốt danh sách kỳ vọng ngay lúc tạo kỳ: sách nhập thêm sau đó không được tính vào kỳ này,
        // nếu không thì kỳ kiểm kê chạy một tuần sẽ không bao giờ khớp.
        var expected = await ExpectedItems(period).ToListAsync(ct);

        foreach (var item in expected)
        {
            _db.InventoryResults.Add(new InventoryResult
            {
                Id = Guid.NewGuid(),
                PeriodId = period.Id,
                ItemId = item.Id,
                Barcode = item.Barcode,
                ExpectedStatus = item.Status,
                ExpectedWarehouseId = item.WarehouseId,
                // Chưa quét thì coi là thiếu; mỗi lần quét sẽ lật dòng tương ứng sang khớp.
                Result = InventoryResultType.Missing
            });
        }

        period.ExpectedCount = expected.Count;

        if (command.CloseWarehouse)
        {
            warehouse.IsClosedForInventory = true;
        }

        var assigned = await InventoryStaff.ReplaceAsync(_db, period, command.AssignedUserIds, ct);

        await _db.SaveChangesAsync(ct);

        await InventoryStaff.NotifyAsync(_notifier, period, warehouse.Name, assigned, ct);

        return period.Id;
    }

    internal IQueryable<Item> ExpectedItemsOf(InventoryPeriod period) => ExpectedItems(period);

    private IQueryable<Item> ExpectedItems(InventoryPeriod period)
    {
        var items = _db.Items
            .AsNoTracking()
            .Where(item => item.WarehouseId == period.WarehouseId)
            // Bản đã thanh lý hoặc ghi mất không còn nằm trên giá nên không thuộc danh sách kỳ vọng.
            .Where(item => item.Status != ItemStatus.Discarded && item.Status != ItemStatus.Lost)
            // Bản đang ở tay bạn đọc cũng không nằm trên giá. Kiểm kê đếm cái **trên giá**; xếp một
            // cuốn đang mượn vào "thiếu" là sai hai lần: cán bộ đi tìm một cuốn không thể có ở đó, và
            // danh sách thiếu — thứ dùng để lập quyết định mất (Chương V III.4 bước 5) — mang theo cả
            // những cuốn có người đang giữ hợp lệ. Một kỳ kiểm kê toàn kho trên kho phát triển đếm
            // nhầm 157 cuốn như thế (K17, 06/09/2026).
            //
            // Cuốn nào sổ ghi đang mượn mà thật ra nằm trên giá thì lúc quét sẽ hiện "thừa" — đúng
            // thứ cần biết, vì nghĩa là có lượt trả chưa được ghi.
            .Where(item => !_db.Loans.Any(loan =>
                loan.ItemId == item.Id && loan.ReturnDate == null && loan.DeletedAt == null));

        return period.ScopeType switch
        {
            "RANGE" => items.Where(item =>
                string.Compare(item.RegisterNumber, period.ScopeFrom!) >= 0
                && string.Compare(item.RegisterNumber, period.ScopeTo!) <= 0),
            "DOCTYPE" => items.Where(item => item.Bib!.DocumentTypeId == period.ScopeDocumentTypeId),
            _ => items
        };
    }
}

/// <summary>Kết quả một lần quét, trả về ngay cho màn hình quét liên tục.</summary>
public class InventoryScanResultDto
{
    public string Barcode { get; set; } = string.Empty;
    public InventoryResultType Outcome { get; set; }
    public string OutcomeName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? RegisterNumber { get; set; }
    public string? ActualWarehouseName { get; set; }
    /// <summary>Mã vạch này đã quét trước đó trong cùng kỳ.</summary>
    public bool AlreadyScanned { get; set; }
    public int ScannedCount { get; set; }
    public int ExpectedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>Ghi nhận một lần quét mã vạch trong kỳ kiểm kê (III.4 bước 3).</summary>
public class ScanInventoryCommand : IRequest<InventoryScanResultDto>
{
    public Guid PeriodId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    /// <summary>Web | Mobile | Offline</summary>
    public string Device { get; set; } = "Web";
}

public class ScanInventoryCommandValidator : AbstractValidator<ScanInventoryCommand>
{
    public ScanInventoryCommandValidator()
    {
        RuleFor(command => command.Barcode).NotEmpty().WithMessage("Chưa có mã vạch để ghi nhận.");
    }
}

public class ScanInventoryCommandHandler : IRequestHandler<ScanInventoryCommand, InventoryScanResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ScanInventoryCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<InventoryScanResultDto> Handle(ScanInventoryCommand command, CancellationToken ct)
    {
        var period = await _db.InventoryPeriods.FirstOrDefaultAsync(entity => entity.Id == command.PeriodId, ct)
            ?? throw new NotFoundException("kỳ kiểm kê", command.PeriodId);

        if (period.Status == InventoryPeriodStatus.Closed)
        {
            throw new ConflictException($"Kỳ kiểm kê {period.Code} đã chốt nên không quét thêm được.");
        }

        var outcome = await InventoryScanner.RecordAsync(
            _db, period, command.Barcode.Trim(), command.Device, _currentUser.UserId, _clock.Now, ct);

        await _db.SaveChangesAsync(ct);

        outcome.ExpectedCount = period.ExpectedCount;
        outcome.ScannedCount = period.ScannedCount;

        return outcome;
    }
}

/// <summary>
/// Nạp tệp quét từ máy đọc rời: mỗi dòng một mã vạch (III.4 bước 3).
/// </summary>
public class ImportInventoryScansCommand : IRequest<ImportInventoryScansResultDto>
{
    public Guid PeriodId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class ImportInventoryScansResultDto
{
    public int Total { get; set; }
    public int Match { get; set; }
    public int Unexpected { get; set; }
    public int WrongWarehouse { get; set; }
    public int Duplicate { get; set; }
    public int ScannedCount { get; set; }
    public int ExpectedCount { get; set; }
}

public class ImportInventoryScansCommandHandler
    : IRequestHandler<ImportInventoryScansCommand, ImportInventoryScansResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ImportInventoryScansCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<ImportInventoryScansResultDto> Handle(
        ImportInventoryScansCommand command, CancellationToken ct)
    {
        var period = await _db.InventoryPeriods.FirstOrDefaultAsync(entity => entity.Id == command.PeriodId, ct)
            ?? throw new NotFoundException("kỳ kiểm kê", command.PeriodId);

        if (period.Status == InventoryPeriodStatus.Closed)
        {
            throw new ConflictException($"Kỳ kiểm kê {period.Code} đã chốt nên không nạp thêm được.");
        }

        // Máy đọc rời xuất ra đủ kiểu: mỗi dòng một mã, hoặc CSV có thêm cột thời gian. Lấy cột đầu
        // của mỗi dòng là cách đọc được cả hai mà không bắt cán bộ sửa tệp.
        var barcodes = command.Content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(new[] { ',', ';', '\t' }, 2)[0].Trim().Trim('"'))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        var result = new ImportInventoryScansResultDto { Total = barcodes.Count };
        var now = _clock.Now;

        foreach (var barcode in barcodes)
        {
            var outcome = await InventoryScanner.RecordAsync(
                _db, period, barcode, "Offline", _currentUser.UserId, now, ct);

            if (outcome.AlreadyScanned)
            {
                result.Duplicate++;
                continue;
            }

            switch (outcome.Outcome)
            {
                case InventoryResultType.Match:
                    result.Match++;
                    break;
                case InventoryResultType.Unexpected:
                    result.Unexpected++;
                    break;
                case InventoryResultType.WrongWarehouse:
                    result.WrongWarehouse++;
                    break;
            }
        }

        await _db.SaveChangesAsync(ct);

        result.ScannedCount = period.ScannedCount;
        result.ExpectedCount = period.ExpectedCount;

        return result;
    }
}

/// <summary>
/// Ghi nhận một mã vạch vào kỳ kiểm kê.
///
/// Dùng chung cho quét trực tiếp và nạp tệp quét rời để hai đường cho ra cùng một kết quả — bản quét
/// bằng máy đọc rời không được kết luận khác bản quét tại chỗ.
/// </summary>
internal static class InventoryScanner
{
    public static async Task<InventoryScanResultDto> RecordAsync(
        IApplicationDbContext db,
        InventoryPeriod period,
        string barcode,
        string device,
        Guid? userId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var item = await db.Items
            .Include(entity => entity.Bib)
            .Include(entity => entity.Warehouse)
            .FirstOrDefaultAsync(entity => entity.Barcode == barcode, ct);

        var existing = await db.InventoryResults
            .FirstOrDefaultAsync(result => result.PeriodId == period.Id && result.Barcode == barcode, ct);

        var alreadyScanned = existing is not null && existing.Result != InventoryResultType.Missing;

        InventoryResultType outcome;
        string message;

        if (item is null)
        {
            outcome = InventoryResultType.Unexpected;
            message = "Mã vạch không có trong hệ thống.";
        }
        else if (existing is not null)
        {
            if (item.WarehouseId != period.WarehouseId)
            {
                outcome = InventoryResultType.WrongWarehouse;
                message = $"Bản này thuộc kho {item.Warehouse?.Name}, không thuộc kho đang kiểm kê.";
            }
            else
            {
                outcome = InventoryResultType.Match;
                message = alreadyScanned ? "Mã vạch này đã quét rồi." : "Khớp.";
            }
        }
        else if (item.WarehouseId != period.WarehouseId)
        {
            outcome = InventoryResultType.WrongWarehouse;
            message = $"Bản này thuộc kho {item.Warehouse?.Name}, không thuộc kho đang kiểm kê.";
        }
        else
        {
            // Có trong kho nhưng không có trong danh sách kỳ vọng: nhập sau khi chốt kỳ, hoặc nằm
            // ngoài phạm vi kiểm kê. Vẫn phải ghi lại để cán bộ đối chiếu.
            outcome = InventoryResultType.Unexpected;
            message = "Bản này không nằm trong danh sách kỳ vọng của kỳ kiểm kê.";
        }

        db.InventoryScans.Add(new InventoryScan
        {
            Id = Guid.NewGuid(),
            PeriodId = period.Id,
            ItemId = item?.Id,
            Barcode = barcode,
            ScannedAt = now,
            ScannedBy = userId,
            Device = device,
            Outcome = outcome
        });

        if (existing is not null)
        {
            existing.Result = outcome;
            existing.ItemId = item?.Id;
            existing.ActualStatus = item?.Status;
            existing.ActualWarehouseId = item?.WarehouseId;
        }
        else
        {
            db.InventoryResults.Add(new InventoryResult
            {
                Id = Guid.NewGuid(),
                PeriodId = period.Id,
                ItemId = item?.Id,
                Barcode = barcode,
                ExpectedStatus = null,
                ExpectedWarehouseId = null,
                ActualStatus = item?.Status,
                ActualWarehouseId = item?.WarehouseId,
                Result = outcome
            });
        }

        // Tiến độ đếm số bản trong danh sách kỳ vọng đã quét được. Một mã lạ hay một bản của kho khác
        // vẫn được ghi nhận vào kết quả, nhưng không làm thanh tiến độ nhích lên — nếu không, màn
        // hình quét sẽ hiện "đã quét 12/10" và cán bộ mất luôn cách biết còn phải tìm bao nhiêu bản.
        if (!alreadyScanned && existing is not null && outcome == InventoryResultType.Match)
        {
            period.ScannedCount++;
        }

        return new InventoryScanResultDto
        {
            Barcode = barcode,
            Outcome = outcome,
            OutcomeName = InventoryResultLabels.Of(outcome),
            Title = item?.Bib?.Title,
            RegisterNumber = item?.RegisterNumber,
            ActualWarehouseName = item?.Warehouse?.Name,
            AlreadyScanned = alreadyScanned,
            Message = message
        };
    }
}

/// <summary>Chốt kỳ kiểm kê và mở lại kho (III.4 bước 4).</summary>
/// <summary>
/// Phân công cán bộ cho kỳ kiểm kê (III.4 bước 2). Tách riêng vì người ốm, người bận, kỳ kiểm kê
/// chạy cả tuần — danh sách phải sửa được giữa chừng chứ không chỉ lúc lập kỳ.
/// </summary>
public class AssignInventoryStaffCommand : IRequest<int>
{
    public Guid PeriodId { get; set; }
    public List<Guid> UserIds { get; set; } = new();
    /// <summary>Người ngoài danh sách tài khoản, ghi thêm để in lên biên bản.</summary>
    public string? OtherStaff { get; set; }
}

public class AssignInventoryStaffCommandHandler : IRequestHandler<AssignInventoryStaffCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly IStaffNotifier _notifier;

    public AssignInventoryStaffCommandHandler(IApplicationDbContext db, IStaffNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task<int> Handle(AssignInventoryStaffCommand command, CancellationToken ct)
    {
        var period = await _db.InventoryPeriods
            .Include(entity => entity.Staff)
            .FirstOrDefaultAsync(entity => entity.Id == command.PeriodId, ct)
            ?? throw new NotFoundException("kỳ kiểm kê", command.PeriodId);

        if (period.Status == InventoryPeriodStatus.Closed)
        {
            throw new ConflictException($"Kỳ kiểm kê {period.Code} đã chốt, không đổi được phân công.");
        }

        var already = period.Staff.Select(row => row.UserId).ToHashSet();
        var assigned = await InventoryStaff.ReplaceAsync(_db, period, command.UserIds, ct, command.OtherStaff);

        await _db.SaveChangesAsync(ct);

        var warehouseName = await _db.Warehouses
            .Where(warehouse => warehouse.Id == period.WarehouseId)
            .Select(warehouse => warehouse.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        // Chỉ báo cho người **mới** được thêm vào: phân công lại vì một người rút ra không phải lý do
        // để cả nhóm nhận lại một thông báo họ đã đọc từ đầu tuần.
        await InventoryStaff.NotifyAsync(
            _notifier, period, warehouseName,
            assigned.Where(row => !already.Contains(row.UserId)).ToList(), ct);

        return assigned.Count;
    }
}

/// <summary>Phần dùng chung của việc phân công cán bộ kiểm kê.</summary>
internal static class InventoryStaff
{
    /// <summary>
    /// Thay trọn danh sách cán bộ của kỳ, trả về danh sách sau khi thay. Tên của họ được chép vào
    /// <c>AssignedStaff</c> để bản in không phải nối bảng.
    /// </summary>
    public static async Task<List<AssignedStaffDto>> ReplaceAsync(
        IApplicationDbContext db,
        InventoryPeriod period,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken ct,
        string? otherStaff = null)
    {
        var wanted = userIds.Distinct().ToList();

        var users = wanted.Count == 0
            ? new List<AssignedStaffDto>()
            : await db.Users
                .AsNoTracking()
                .Where(user => wanted.Contains(user.Id))
                .Select(user => new AssignedStaffDto(user.Id, user.FullName))
                .ToListAsync(ct);

        if (users.Count != wanted.Count)
        {
            throw new NotFoundException("Có tài khoản cán bộ không còn tồn tại trong danh sách phân công.");
        }

        var existing = await db.InventoryPeriodStaff
            .Where(row => row.PeriodId == period.Id)
            .ToListAsync(ct);

        foreach (var row in existing.Where(row => !wanted.Contains(row.UserId)))
        {
            db.InventoryPeriodStaff.Remove(row);
        }

        var kept = existing.Select(row => row.UserId).ToHashSet();

        foreach (var userId in wanted.Where(id => !kept.Contains(id)))
        {
            // Liên kết mới thì thêm thẳng vào tập hợp chứ không qua navigation của thực thể đang
            // Unchanged — bài học 20 trong CLAUDE.md.
            db.InventoryPeriodStaff.Add(new InventoryPeriodStaff
            {
                Id = Guid.NewGuid(),
                PeriodId = period.Id,
                UserId = userId
            });
        }

        var names = users.Select(user => user.FullName).ToList();

        if (!string.IsNullOrWhiteSpace(otherStaff))
        {
            names.Add(otherStaff.Trim());
        }

        if (names.Count > 0)
        {
            period.AssignedStaff = string.Join(", ", names);
        }
        else if (otherStaff is not null)
        {
            // Gửi lên chuỗi rỗng là cố ý xoá, khác hẳn với không gửi trường ấy.
            period.AssignedStaff = null;
        }

        return users;
    }

    public static async Task NotifyAsync(
        IStaffNotifier notifier,
        InventoryPeriod period,
        string warehouseName,
        IReadOnlyCollection<AssignedStaffDto> assigned,
        CancellationToken ct)
    {
        if (assigned.Count == 0)
        {
            return;
        }

        var deadline = period.EndDate is { } end ? $" Hạn kiểm kê: {end:dd/MM/yyyy}." : string.Empty;

        await notifier.NotifyUsersAsync(
            assigned.Select(row => row.UserId),
            StaffNotificationTypes.System,
            $"Bạn được phân công kiểm kê {warehouseName}",
            $"Kỳ {period.Code} — {period.Name}, {period.ExpectedCount} ĐKCB cần đối chiếu.{deadline}",
            "/inventory",
            ct);
    }
}

public class CloseInventoryPeriodCommand : IRequest<InventorySummaryDto>
{
    public Guid Id { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Note { get; set; }
    /// <summary>Mở lại kho ngay sau khi chốt.</summary>
    public bool ReopenWarehouse { get; set; } = true;
}

public class CloseInventoryPeriodCommandHandler
    : IRequestHandler<CloseInventoryPeriodCommand, InventorySummaryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CloseInventoryPeriodCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<InventorySummaryDto> Handle(CloseInventoryPeriodCommand command, CancellationToken ct)
    {
        var period = await _db.InventoryPeriods
            .Include(entity => entity.Warehouse)
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("kỳ kiểm kê", command.Id);

        if (period.Status == InventoryPeriodStatus.Closed)
        {
            throw new ConflictException($"Kỳ kiểm kê {period.Code} đã chốt rồi.");
        }

        period.Status = InventoryPeriodStatus.Closed;
        period.EndDate = command.EndDate ?? _clock.Today;
        period.ClosedBy = _currentUser.UserId;
        period.ClosedAt = _clock.Now;

        if (!string.IsNullOrWhiteSpace(command.Note))
        {
            period.Note = string.IsNullOrWhiteSpace(period.Note)
                ? command.Note.Trim()
                : $"{period.Note}\n{command.Note.Trim()}";
        }

        if (command.ReopenWarehouse && period.Warehouse is not null)
        {
            period.Warehouse.IsClosedForInventory = false;
        }

        await _db.SaveChangesAsync(ct);

        return await InventorySummaryLoader.LoadAsync(_db, period.Id, ct);
    }
}

/// <summary>Số liệu tổng hợp của một kỳ kiểm kê.</summary>
public class InventorySummaryDto
{
    public Guid PeriodId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public InventoryPeriodStatus Status { get; set; }
    public int ExpectedCount { get; set; }
    public int ScannedCount { get; set; }
    public int MatchCount { get; set; }
    public int MissingCount { get; set; }
    public int UnexpectedCount { get; set; }
    public int WrongWarehouseCount { get; set; }
    public decimal MissingValue { get; set; }
    /// <summary>Phần trăm đã quét trên tổng kỳ vọng, để hiện thanh tiến độ.</summary>
    public double ProgressPercent { get; set; }
}

public record GetInventorySummaryQuery(Guid PeriodId) : IRequest<InventorySummaryDto>;

public class GetInventorySummaryQueryHandler
    : IRequestHandler<GetInventorySummaryQuery, InventorySummaryDto>
{
    private readonly IApplicationDbContext _db;

    public GetInventorySummaryQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<InventorySummaryDto> Handle(GetInventorySummaryQuery query, CancellationToken ct) =>
        InventorySummaryLoader.LoadAsync(_db, query.PeriodId, ct);
}

internal static class InventorySummaryLoader
{
    public static async Task<InventorySummaryDto> LoadAsync(
        IApplicationDbContext db, Guid periodId, CancellationToken ct)
    {
        var period = await db.InventoryPeriods
            .AsNoTracking()
            .Where(entity => entity.Id == periodId)
            .Select(entity => new
            {
                entity.Id,
                entity.Code,
                entity.Name,
                entity.Status,
                entity.ExpectedCount,
                entity.ScannedCount,
                WarehouseName = entity.Warehouse!.Name
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("kỳ kiểm kê", periodId);

        var results = db.InventoryResults.AsNoTracking().Where(result => result.PeriodId == periodId);

        var summary = new InventorySummaryDto
        {
            PeriodId = period.Id,
            Code = period.Code,
            Name = period.Name,
            WarehouseName = period.WarehouseName,
            Status = period.Status,
            ExpectedCount = period.ExpectedCount,
            ScannedCount = period.ScannedCount,
            MatchCount = await results.CountAsync(result => result.Result == InventoryResultType.Match, ct),
            MissingCount = await results.CountAsync(result => result.Result == InventoryResultType.Missing, ct),
            UnexpectedCount = await results.CountAsync(result => result.Result == InventoryResultType.Unexpected, ct),
            WrongWarehouseCount =
                await results.CountAsync(result => result.Result == InventoryResultType.WrongWarehouse, ct),
            MissingValue = await results
                .Where(result => result.Result == InventoryResultType.Missing && result.Item != null)
                .SumAsync(result => (decimal?)result.Item!.Price, ct) ?? 0
        };

        summary.ProgressPercent = period.ExpectedCount == 0
            ? 0
            : Math.Round(Math.Min(100, period.ScannedCount * 100.0 / period.ExpectedCount), 1);

        return summary;
    }
}

public class InventoryResultRowDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string? RegisterNumber { get; set; }
    public string? Title { get; set; }
    public string? AuthorMain { get; set; }
    public string? CallNumber { get; set; }
    public decimal Price { get; set; }
    public InventoryResultType Result { get; set; }
    public string ResultName { get; set; } = string.Empty;
    public string? ExpectedWarehouseName { get; set; }
    public string? ActualWarehouseName { get; set; }
    public bool IsResolved { get; set; }
    public string? Note { get; set; }
}

public class InventoryResultRequest : PagedRequest
{
    public Guid PeriodId { get; set; }
    public InventoryResultType? Result { get; set; }
    public bool? UnresolvedOnly { get; set; }
}

public record GetInventoryResultsQuery(InventoryResultRequest Request)
    : IRequest<PagedResult<InventoryResultRowDto>>;

public class GetInventoryResultsQueryHandler
    : IRequestHandler<GetInventoryResultsQuery, PagedResult<InventoryResultRowDto>>
{
    private readonly IApplicationDbContext _db;

    public GetInventoryResultsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<InventoryResultRowDto>> Handle(
        GetInventoryResultsQuery query, CancellationToken ct)
    {
        var request = query.Request;

        var results = _db.InventoryResults
            .AsNoTracking()
            .Where(result => result.PeriodId == request.PeriodId)
            .WhereIf(request.Result is not null, result => result.Result == request.Result)
            .WhereIf(request.UnresolvedOnly == true, result => !result.IsResolved);

        var total = await results.CountAsync(ct);

        var page = await results
            .OrderBy(result => result.Result)
            .ThenBy(result => result.Barcode)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(result => new InventoryResultRowDto
            {
                Id = result.Id,
                ItemId = result.ItemId,
                Barcode = result.Barcode,
                RegisterNumber = result.Item == null ? null : result.Item.RegisterNumber,
                Title = result.Item == null ? null : result.Item.Bib!.Title,
                AuthorMain = result.Item == null ? null : result.Item.Bib!.AuthorMain,
                CallNumber = result.Item == null ? null : result.Item.CallNumber,
                // Dòng "thừa" của một mã lạ không gắn với ĐKCB nào nên không có giá để hiện.
                Price = result.Item == null ? 0 : result.Item.Price,
                Result = result.Result,
                ExpectedWarehouseName = _db.Warehouses
                    .Where(warehouse => warehouse.Id == result.ExpectedWarehouseId)
                    .Select(warehouse => warehouse.Name)
                    .FirstOrDefault(),
                ActualWarehouseName = _db.Warehouses
                    .Where(warehouse => warehouse.Id == result.ActualWarehouseId)
                    .Select(warehouse => warehouse.Name)
                    .FirstOrDefault(),
                IsResolved = result.IsResolved,
                Note = result.Note
            })
            .ToListAsync(ct);

        foreach (var row in page)
        {
            row.ResultName = InventoryResultLabels.Of(row.Result);
        }

        return new PagedResult<InventoryResultRowDto>(page, total, request.Page, request.PageSize);
    }
}

/// <summary>
/// Từ danh sách thiếu, lập luôn quyết định thanh lý hoặc ghi mất (III.4 bước 5).
/// </summary>
public class ResolveMissingItemsCommand : IRequest<BulkItemResultDto>
{
    public Guid PeriodId { get; set; }
    /// <summary>Bỏ trống thì xử lý toàn bộ dòng thiếu chưa xử lý của kỳ.</summary>
    public List<Guid> ResultIds { get; set; } = new();
    /// <summary>Thanh lý | Mất | Hỏng không phục hồi.</summary>
    public string DisposalType { get; set; } = "Mất";
    public string? Reason { get; set; }
    public string? DecisionNo { get; set; }
}

public class ResolveMissingItemsCommandHandler
    : IRequestHandler<ResolveMissingItemsCommand, BulkItemResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IMediator _mediator;

    public ResolveMissingItemsCommandHandler(IApplicationDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<BulkItemResultDto> Handle(ResolveMissingItemsCommand command, CancellationToken ct)
    {
        var period = await _db.InventoryPeriods.FirstOrDefaultAsync(entity => entity.Id == command.PeriodId, ct)
            ?? throw new NotFoundException("kỳ kiểm kê", command.PeriodId);

        var rows = await _db.InventoryResults
            .Where(result => result.PeriodId == period.Id
                             && result.Result == InventoryResultType.Missing
                             && !result.IsResolved
                             && result.ItemId != null)
            // Chốt chặn thứ hai cho K17: kỳ lập trước bản sửa vẫn còn dòng của sách đang mượn trong
            // danh sách thiếu. Không bao giờ ghi mất một cuốn mà phiếu mượn còn đang mở — bạn đọc vẫn
            // phải trả nó, và bản ghi "mất" sẽ chặn chính lượt trả ấy.
            .Where(result => !_db.Loans.Any(loan =>
                loan.ItemId == result.ItemId && loan.ReturnDate == null && loan.DeletedAt == null))
            .WhereIf(command.ResultIds.Count > 0, result => command.ResultIds.Contains(result.Id))
            .ToListAsync(ct);

        if (rows.Count == 0)
        {
            throw new ConflictException("Không có dòng thiếu nào chưa xử lý trong kỳ kiểm kê này.");
        }

        // Đi qua đúng lệnh thanh lý của III.5 chứ không tự sửa trạng thái: quyết định thanh lý, lịch
        // sử và việc đếm lại số bản đều nằm ở đó, viết lại ở đây là chắc chắn lệch.
        var outcome = await _mediator.Send(new DisposeItemsCommand
        {
            ItemIds = rows.Select(row => row.ItemId!.Value).ToList(),
            DisposalType = command.DisposalType,
            Reason = string.IsNullOrWhiteSpace(command.Reason)
                ? $"Thiếu khi kiểm kê kỳ {period.Code}"
                : command.Reason.Trim(),
            DecisionNo = command.DecisionNo
        }, ct);

        foreach (var row in rows)
        {
            row.IsResolved = true;
            row.Note = string.IsNullOrWhiteSpace(row.Note)
                ? $"{command.DisposalType} theo quyết định {outcome.DocumentCode}"
                : $"{row.Note}\n{command.DisposalType} theo quyết định {outcome.DocumentCode}";
        }

        await _db.SaveChangesAsync(ct);
        return outcome;
    }
}
