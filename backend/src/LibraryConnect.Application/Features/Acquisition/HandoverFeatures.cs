using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Acq;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Acquisition;

// ---------------------------------------------------------------------------------------------
// III.1 — Biên bản bàn giao.
// ---------------------------------------------------------------------------------------------

public class HandoverDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public string? OrderCode { get; set; }
    public string? SupplierName { get; set; }
    public DateOnly HandoverDate { get; set; }
    public string PartyA { get; set; } = string.Empty;
    public string PartyB { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalAmount { get; set; }
    /// <summary>Đã đính kèm bản scan có chữ ký hay chưa.</summary>
    public bool HasScan { get; set; }
    public string? Note { get; set; }
}

public class HandoverListRequest : PagedRequest
{
    public Guid? OrderId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public record SearchHandoversQuery(HandoverListRequest Request) : IRequest<PagedResult<HandoverDto>>;

public class SearchHandoversQueryHandler : IRequestHandler<SearchHandoversQuery, PagedResult<HandoverDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchHandoversQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<HandoverDto>> Handle(SearchHandoversQuery query, CancellationToken ct)
    {
        var request = query.Request;

        var records = _db.HandoverRecords
            .AsNoTracking()
            .WhereIf(request.OrderId is not null, record => record.OrderId == request.OrderId)
            .WhereIf(request.From is not null, record => record.HandoverDate >= request.From)
            .WhereIf(request.To is not null, record => record.HandoverDate <= request.To);

        if (request.HasKeyword())
        {
            var keyword = request.Keyword!.Trim().ToLowerInvariant();

            records = records.Where(record =>
                record.Code.ToLower().Contains(keyword)
                || record.Order!.Code.ToLower().Contains(keyword));
        }

        var total = await records.CountAsync(ct);

        var page = await records
            .OrderByDescending(record => record.HandoverDate)
            .ThenByDescending(record => record.Code)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(record => new HandoverDto
            {
                Id = record.Id,
                Code = record.Code,
                OrderId = record.OrderId,
                OrderCode = record.Order!.Code,
                SupplierName = record.Order!.Supplier!.Name,
                HandoverDate = record.HandoverDate,
                PartyA = record.PartyA,
                PartyB = record.PartyB,
                Content = record.Content,
                TotalItems = record.TotalItems,
                TotalAmount = record.TotalAmount,
                HasScan = record.FileUrl != null,
                Note = record.Note
            })
            .ToListAsync(ct);

        return new PagedResult<HandoverDto>(page, total, request.Page, request.PageSize);
    }
}

public record GetHandoverQuery(Guid Id) : IRequest<HandoverDto>;

public class GetHandoverQueryHandler : IRequestHandler<GetHandoverQuery, HandoverDto>
{
    private readonly IApplicationDbContext _db;

    public GetHandoverQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<HandoverDto> Handle(GetHandoverQuery query, CancellationToken ct) =>
        await _db.HandoverRecords
            .AsNoTracking()
            .Where(record => record.Id == query.Id)
            .Select(record => new HandoverDto
            {
                Id = record.Id,
                Code = record.Code,
                OrderId = record.OrderId,
                OrderCode = record.Order!.Code,
                SupplierName = record.Order!.Supplier!.Name,
                HandoverDate = record.HandoverDate,
                PartyA = record.PartyA,
                PartyB = record.PartyB,
                Content = record.Content,
                TotalItems = record.TotalItems,
                TotalAmount = record.TotalAmount,
                HasScan = record.FileUrl != null,
                Note = record.Note
            })
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("biên bản bàn giao", query.Id);
}

/// <summary>Lập hoặc sửa biên bản bàn giao. Tạo từ đơn đặt thì số lượng và giá trị tự tính.</summary>
public class SaveHandoverCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public Guid? OrderId { get; set; }
    public DateOnly? HandoverDate { get; set; }
    public string PartyA { get; set; } = string.Empty;
    public string PartyB { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Note { get; set; }
    /// <summary>Chỉ dùng khi không gắn với đơn đặt nào.</summary>
    public int? TotalItems { get; set; }
    public decimal? TotalAmount { get; set; }
}

public class SaveHandoverCommandValidator : AbstractValidator<SaveHandoverCommand>
{
    public SaveHandoverCommandValidator()
    {
        RuleFor(command => command.PartyA)
            .NotEmpty().WithMessage("Chưa nhập bên giao.").MaximumLength(500);

        RuleFor(command => command.PartyB)
            .NotEmpty().WithMessage("Chưa nhập bên nhận.").MaximumLength(500);

        RuleFor(command => command)
            .Must(command => command.OrderId is not null || command.TotalItems is not null)
            .WithMessage("Biên bản không gắn đơn đặt thì phải nhập tổng số bản bàn giao.");
    }
}

public class SaveHandoverCommandHandler : IRequestHandler<SaveHandoverCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly IDateTimeProvider _clock;

    public SaveHandoverCommandHandler(IApplicationDbContext db, ICodeGenerator codes, IDateTimeProvider clock)
    {
        _db = db;
        _codes = codes;
        _clock = clock;
    }

    public async Task<Guid> Handle(SaveHandoverCommand command, CancellationToken ct)
    {
        var handover = command.Id is null
            ? new HandoverRecord { Id = Guid.NewGuid(), Code = await _codes.NextAsync("HANDOVER", ct) }
            : await _db.HandoverRecords.FirstOrDefaultAsync(record => record.Id == command.Id, ct)
              ?? throw new NotFoundException("biên bản bàn giao", command.Id);

        handover.OrderId = command.OrderId;
        handover.HandoverDate = command.HandoverDate ?? _clock.Today;
        handover.PartyA = command.PartyA.Trim();
        handover.PartyB = command.PartyB.Trim();
        handover.Content = command.Content?.Trim();
        handover.Note = command.Note?.Trim();

        if (command.OrderId is not null)
        {
            var order = await _db.PurchaseOrders
                .Include(entity => entity.Items)
                .FirstOrDefaultAsync(entity => entity.Id == command.OrderId, ct)
                ?? throw new NotFoundException("đơn đặt", command.OrderId);

            // Số liệu trên biên bản lấy từ số thực nhận của đơn chứ không cho gõ tay: biên bản là
            // thứ mang đi ký, nên nó phải khớp với cái hệ thống ghi nhận đã nhận.
            //
            // Đơn chưa ghi nhận giao hàng lần nào thì biên bản đang được lập trước lúc nhận, lúc đó
            // số đã đặt là số duy nhất có. Đã ghi nhận rồi thì chỉ tính các dòng thực nhận — cùng
            // đúng bộ dòng mà bảng chi tiết in ra, nếu không tổng tiền sẽ không khớp bảng ngay trên nó.
            var anyReceived = order.Items.Any(line => line.ReceivedQuantity > 0);

            handover.TotalItems = anyReceived
                ? order.Items.Sum(line => line.ReceivedQuantity)
                : order.Items.Sum(line => line.Quantity);

            handover.TotalAmount = anyReceived
                ? order.Items.Sum(line => line.ReceivedQuantity * line.UnitPrice)
                : order.Items.Sum(line => line.Quantity * line.UnitPrice);
        }
        else
        {
            handover.TotalItems = command.TotalItems ?? 0;
            handover.TotalAmount = command.TotalAmount ?? 0;
        }

        if (command.Id is null)
        {
            _db.HandoverRecords.Add(handover);
        }

        await _db.SaveChangesAsync(ct);
        return handover.Id;
    }
}

public record DeleteHandoverCommand(Guid Id) : IRequest;

public class DeleteHandoverCommandHandler : IRequestHandler<DeleteHandoverCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteHandoverCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteHandoverCommand command, CancellationToken ct)
    {
        var handover = await _db.HandoverRecords.FirstOrDefaultAsync(record => record.Id == command.Id, ct)
            ?? throw new NotFoundException("biên bản bàn giao", command.Id);

        _db.HandoverRecords.Remove(handover);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Bản scan biên bản đã ký được lưu ở kho đối tượng, không lưu dưới thư mục web.</summary>
public static class HandoverFiles
{
    public const string Bucket = "handovers";
    public const long MaxSizeBytes = 20 * 1024 * 1024;

    public static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/pdf"] = ".pdf",
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/tiff"] = ".tif"
        };
}

/// <summary>Đính kèm bản scan biên bản đã ký.</summary>
public record AttachHandoverScanCommand(Guid Id, string ContentType, byte[] Content) : IRequest<string>;

public class AttachHandoverScanCommandHandler : IRequestHandler<AttachHandoverScanCommand, string>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public AttachHandoverScanCommandHandler(IApplicationDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<string> Handle(AttachHandoverScanCommand command, CancellationToken ct)
    {
        var handover = await _db.HandoverRecords.FirstOrDefaultAsync(record => record.Id == command.Id, ct)
            ?? throw new NotFoundException("biên bản bàn giao", command.Id);

        if (command.Content.Length == 0)
        {
            throw new Common.Exceptions.ValidationException("file", "Tệp rỗng.");
        }

        if (command.Content.Length > HandoverFiles.MaxSizeBytes)
        {
            throw new Common.Exceptions.ValidationException(
                "file", $"Tệp scan tối đa {HandoverFiles.MaxSizeBytes / 1024 / 1024} MB.");
        }

        if (!HandoverFiles.AllowedTypes.TryGetValue(command.ContentType, out var extension))
        {
            throw new Common.Exceptions.ValidationException(
                "file", "Chỉ nhận tệp PDF hoặc ảnh PNG, JPG, TIFF.");
        }

        var objectName = $"{handover.Code}{extension}";
        using var stream = new MemoryStream(command.Content);

        await _storage.UploadAsync(HandoverFiles.Bucket, objectName, stream, command.ContentType, ct);

        handover.FileUrl = objectName;
        await _db.SaveChangesAsync(ct);

        return objectName;
    }
}

public record GetHandoverScanQuery(Guid Id) : IRequest<PrintedFileDto>;

public class GetHandoverScanQueryHandler : IRequestHandler<GetHandoverScanQuery, PrintedFileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public GetHandoverScanQueryHandler(IApplicationDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<PrintedFileDto> Handle(GetHandoverScanQuery query, CancellationToken ct)
    {
        var handover = await _db.HandoverRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(record => record.Id == query.Id, ct)
            ?? throw new NotFoundException("biên bản bàn giao", query.Id);

        if (string.IsNullOrWhiteSpace(handover.FileUrl))
        {
            throw new NotFoundException($"Biên bản {handover.Code} chưa đính kèm bản scan nào.");
        }

        await using var stream = await _storage.DownloadAsync(HandoverFiles.Bucket, handover.FileUrl, ct);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);

        var extension = Path.GetExtension(handover.FileUrl).ToLowerInvariant();

        var contentType = HandoverFiles.AllowedTypes
            .FirstOrDefault(pair => pair.Value == extension).Key ?? "application/octet-stream";

        return new PrintedFileDto(buffer.ToArray(), handover.FileUrl, contentType);
    }
}
