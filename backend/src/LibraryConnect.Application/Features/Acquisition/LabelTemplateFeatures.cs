using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Acquisition;

/// <summary>Bố cục tem được lưu dạng JSON; các tùy chọn đọc/ghi dùng chung một chỗ.</summary>
internal static class LabelLayoutJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static LabelLayoutDto Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new LabelLayoutDto();
        }

        try
        {
            return JsonSerializer.Deserialize<LabelLayoutDto>(json, Options) ?? new LabelLayoutDto();
        }
        catch (JsonException)
        {
            // Mẫu hỏng thì trả về bố cục rỗng: cán bộ sửa lại được trên màn hình thiết kế, còn ném
            // lỗi ở đây sẽ khóa luôn cả danh sách mẫu.
            return new LabelLayoutDto();
        }
    }

    public static string Write(LabelLayoutDto layout) => JsonSerializer.Serialize(layout, Options);
}

// ---------------------------------------------------------------------------------------------
// Mẫu tem mã vạch
// ---------------------------------------------------------------------------------------------

public record GetBarcodeTemplatesQuery(bool IncludeInactive = false)
    : IRequest<IReadOnlyList<BarcodeTemplateDto>>;

public class GetBarcodeTemplatesQueryHandler
    : IRequestHandler<GetBarcodeTemplatesQuery, IReadOnlyList<BarcodeTemplateDto>>
{
    private readonly IApplicationDbContext _db;

    public GetBarcodeTemplatesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<BarcodeTemplateDto>> Handle(
        GetBarcodeTemplatesQuery query, CancellationToken ct)
    {
        var templates = await _db.BarcodeTemplates
            .AsNoTracking()
            .WhereIf(!query.IncludeInactive, template => template.IsActive)
            .OrderByDescending(template => template.IsDefault)
            .ThenBy(template => template.Name)
            .ToListAsync(ct);

        return templates.Select(Map).ToList();
    }

    internal static BarcodeTemplateDto Map(BarcodeTemplate template) => new()
    {
        Id = template.Id,
        Code = template.Code,
        Name = template.Name,
        WidthMm = template.WidthMm,
        HeightMm = template.HeightMm,
        BarcodeType = template.BarcodeType,
        ColumnsPerPage = template.ColumnsPerPage,
        RowsPerPage = template.RowsPerPage,
        MarginTopMm = template.MarginTopMm,
        MarginLeftMm = template.MarginLeftMm,
        IsDefault = template.IsDefault,
        IsActive = template.IsActive,
        Layout = LabelLayoutJson.Read(template.Layout)
    };
}

public class SaveBarcodeTemplateCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double WidthMm { get; set; } = 50;
    public double HeightMm { get; set; } = 25;
    public BarcodeType BarcodeType { get; set; } = BarcodeType.Code128;
    public int ColumnsPerPage { get; set; } = 4;
    public int RowsPerPage { get; set; } = 10;
    public double MarginTopMm { get; set; } = 10;
    public double MarginLeftMm { get; set; } = 8;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public LabelLayoutDto Layout { get; set; } = new();
}

public class SaveBarcodeTemplateCommandValidator : AbstractValidator<SaveBarcodeTemplateCommand>
{
    public SaveBarcodeTemplateCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().WithMessage("Chưa nhập mã mẫu tem.").MaximumLength(50);
        RuleFor(command => command.Name).NotEmpty().WithMessage("Chưa nhập tên mẫu tem.").MaximumLength(300);
        RuleFor(command => command.WidthMm).InclusiveBetween(10, 210).WithMessage("Chiều rộng tem phải từ 10 đến 210 mm.");
        RuleFor(command => command.HeightMm).InclusiveBetween(8, 297).WithMessage("Chiều cao tem phải từ 8 đến 297 mm.");
        RuleFor(command => command.ColumnsPerPage).InclusiveBetween(1, 20).WithMessage("Số tem mỗi hàng phải từ 1 đến 20.");
        RuleFor(command => command.RowsPerPage).InclusiveBetween(1, 40).WithMessage("Số hàng tem mỗi trang phải từ 1 đến 40.");

        RuleFor(command => command)
            .Must(command => command.MarginLeftMm + command.ColumnsPerPage * command.WidthMm <= 210)
            .WithMessage("Lề trái cộng chiều rộng các tem vượt quá khổ giấy A4 (210 mm).")
            .Must(command => command.MarginTopMm + command.RowsPerPage * command.HeightMm <= 297)
            .WithMessage("Lề trên cộng chiều cao các tem vượt quá khổ giấy A4 (297 mm).");
    }
}

public class SaveBarcodeTemplateCommandHandler : IRequestHandler<SaveBarcodeTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveBarcodeTemplateCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveBarcodeTemplateCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim().ToUpperInvariant();

        if (await _db.BarcodeTemplates.AnyAsync(t => t.Code == code && t.Id != command.Id, ct))
        {
            throw new Common.Exceptions.ValidationException("code", $"Mã mẫu tem '{code}' đã được dùng.");
        }

        var template = command.Id is null
            ? new BarcodeTemplate()
            : await _db.BarcodeTemplates.FirstOrDefaultAsync(t => t.Id == command.Id, ct)
              ?? throw new NotFoundException("mẫu tem mã vạch", command.Id);

        template.Code = code;
        template.Name = command.Name.Trim();
        template.WidthMm = command.WidthMm;
        template.HeightMm = command.HeightMm;
        template.BarcodeType = command.BarcodeType;
        template.ColumnsPerPage = command.ColumnsPerPage;
        template.RowsPerPage = command.RowsPerPage;
        template.MarginTopMm = command.MarginTopMm;
        template.MarginLeftMm = command.MarginLeftMm;
        template.IsDefault = command.IsDefault;
        template.IsActive = command.IsActive;
        template.Layout = LabelLayoutJson.Write(command.Layout);

        if (command.Id is null)
        {
            _db.BarcodeTemplates.Add(template);
        }

        if (command.IsDefault)
        {
            await _db.BarcodeTemplates
                .Where(other => other.IsDefault && other.Id != template.Id)
                .ExecuteUpdateAsync(setter => setter.SetProperty(other => other.IsDefault, false), ct);
        }

        await _db.SaveChangesAsync(ct);
        return template.Id;
    }
}

public record DeleteBarcodeTemplateCommand(Guid Id) : IRequest;

public class DeleteBarcodeTemplateCommandHandler : IRequestHandler<DeleteBarcodeTemplateCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteBarcodeTemplateCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteBarcodeTemplateCommand command, CancellationToken ct)
    {
        var template = await _db.BarcodeTemplates.FirstOrDefaultAsync(t => t.Id == command.Id, ct)
            ?? throw new NotFoundException("mẫu tem mã vạch", command.Id);

        _db.BarcodeTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }
}

// ---------------------------------------------------------------------------------------------
// Mẫu nhãn gáy
// ---------------------------------------------------------------------------------------------

public record GetLabelTemplatesQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<LabelTemplateDto>>;

public class GetLabelTemplatesQueryHandler
    : IRequestHandler<GetLabelTemplatesQuery, IReadOnlyList<LabelTemplateDto>>
{
    private readonly IApplicationDbContext _db;

    public GetLabelTemplatesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<LabelTemplateDto>> Handle(GetLabelTemplatesQuery query, CancellationToken ct)
    {
        var templates = await _db.LabelTemplates
            .AsNoTracking()
            .WhereIf(!query.IncludeInactive, template => template.IsActive)
            .OrderByDescending(template => template.IsDefault)
            .ThenBy(template => template.Name)
            .ToListAsync(ct);

        return templates.Select(Map).ToList();
    }

    internal static LabelTemplateDto Map(LabelTemplate template) => new()
    {
        Id = template.Id,
        Code = template.Code,
        Name = template.Name,
        WidthMm = template.WidthMm,
        HeightMm = template.HeightMm,
        ColumnsPerPage = template.ColumnsPerPage,
        RowsPerPage = template.RowsPerPage,
        MarginTopMm = template.MarginTopMm,
        MarginLeftMm = template.MarginLeftMm,
        IsDefault = template.IsDefault,
        IsActive = template.IsActive,
        Layout = LabelLayoutJson.Read(template.Layout)
    };
}

public class SaveLabelTemplateCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double WidthMm { get; set; } = 35;
    public double HeightMm { get; set; } = 45;
    public int ColumnsPerPage { get; set; } = 5;
    public int RowsPerPage { get; set; } = 6;
    public double MarginTopMm { get; set; } = 10;
    public double MarginLeftMm { get; set; } = 8;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public LabelLayoutDto Layout { get; set; } = new();
}

public class SaveLabelTemplateCommandValidator : AbstractValidator<SaveLabelTemplateCommand>
{
    public SaveLabelTemplateCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().WithMessage("Chưa nhập mã mẫu nhãn.").MaximumLength(50);
        RuleFor(command => command.Name).NotEmpty().WithMessage("Chưa nhập tên mẫu nhãn.").MaximumLength(300);
        RuleFor(command => command.WidthMm).InclusiveBetween(10, 210).WithMessage("Chiều rộng nhãn phải từ 10 đến 210 mm.");
        RuleFor(command => command.HeightMm).InclusiveBetween(8, 297).WithMessage("Chiều cao nhãn phải từ 8 đến 297 mm.");
        RuleFor(command => command.ColumnsPerPage).InclusiveBetween(1, 20).WithMessage("Số nhãn mỗi hàng phải từ 1 đến 20.");
        RuleFor(command => command.RowsPerPage).InclusiveBetween(1, 40).WithMessage("Số hàng nhãn mỗi trang phải từ 1 đến 40.");

        RuleFor(command => command)
            .Must(command => command.MarginLeftMm + command.ColumnsPerPage * command.WidthMm <= 210)
            .WithMessage("Lề trái cộng chiều rộng các nhãn vượt quá khổ giấy A4 (210 mm).")
            .Must(command => command.MarginTopMm + command.RowsPerPage * command.HeightMm <= 297)
            .WithMessage("Lề trên cộng chiều cao các nhãn vượt quá khổ giấy A4 (297 mm).");
    }
}

public class SaveLabelTemplateCommandHandler : IRequestHandler<SaveLabelTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveLabelTemplateCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveLabelTemplateCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim().ToUpperInvariant();

        if (await _db.LabelTemplates.AnyAsync(t => t.Code == code && t.Id != command.Id, ct))
        {
            throw new Common.Exceptions.ValidationException("code", $"Mã mẫu nhãn '{code}' đã được dùng.");
        }

        var template = command.Id is null
            ? new LabelTemplate()
            : await _db.LabelTemplates.FirstOrDefaultAsync(t => t.Id == command.Id, ct)
              ?? throw new NotFoundException("mẫu nhãn gáy", command.Id);

        template.Code = code;
        template.Name = command.Name.Trim();
        template.WidthMm = command.WidthMm;
        template.HeightMm = command.HeightMm;
        template.ColumnsPerPage = command.ColumnsPerPage;
        template.RowsPerPage = command.RowsPerPage;
        template.MarginTopMm = command.MarginTopMm;
        template.MarginLeftMm = command.MarginLeftMm;
        template.IsDefault = command.IsDefault;
        template.IsActive = command.IsActive;
        template.Layout = LabelLayoutJson.Write(command.Layout);

        if (command.Id is null)
        {
            _db.LabelTemplates.Add(template);
        }

        if (command.IsDefault)
        {
            await _db.LabelTemplates
                .Where(other => other.IsDefault && other.Id != template.Id)
                .ExecuteUpdateAsync(setter => setter.SetProperty(other => other.IsDefault, false), ct);
        }

        await _db.SaveChangesAsync(ct);
        return template.Id;
    }
}

public record DeleteLabelTemplateCommand(Guid Id) : IRequest;

public class DeleteLabelTemplateCommandHandler : IRequestHandler<DeleteLabelTemplateCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteLabelTemplateCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteLabelTemplateCommand command, CancellationToken ct)
    {
        var template = await _db.LabelTemplates.FirstOrDefaultAsync(t => t.Id == command.Id, ct)
            ?? throw new NotFoundException("mẫu nhãn gáy", command.Id);

        _db.LabelTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }
}

// ---------------------------------------------------------------------------------------------
// In tem
// ---------------------------------------------------------------------------------------------

/// <summary>Một tệp đã kết xuất, kèm tên tệp gợi ý cho trình duyệt.</summary>
public record PrintedFileDto(byte[] Content, string FileName, string ContentType);

/// <summary>
/// In tem mã vạch cho các ĐKCB đã chọn hoặc cho toàn bộ kết quả lọc (III.2).
/// </summary>
public class PrintBarcodesCommand : BulkItemCommand, IRequest<PrintedFileDto>
{
    public Guid? TemplateId { get; set; }
    /// <summary>In nhiều bản cho mỗi ĐKCB, dùng khi tem hay bị bong.</summary>
    public int Copies { get; set; } = 1;
}

public class PrintBarcodesCommandValidator : AbstractValidator<PrintBarcodesCommand>
{
    public PrintBarcodesCommandValidator()
    {
        RuleFor(command => command.Copies).InclusiveBetween(1, 10)
            .WithMessage("Số bản mỗi tem phải từ 1 đến 10.");
    }
}

public class PrintBarcodesCommandHandler : IRequestHandler<PrintBarcodesCommand, PrintedFileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ILabelPrintService _printer;
    private readonly IDateTimeProvider _clock;

    public PrintBarcodesCommandHandler(
        IApplicationDbContext db, ILabelPrintService printer, IDateTimeProvider clock)
    {
        _db = db;
        _printer = printer;
        _clock = clock;
    }

    public async Task<PrintedFileDto> Handle(PrintBarcodesCommand command, CancellationToken ct)
    {
        var template = command.TemplateId is null
            ? await _db.BarcodeTemplates.AsNoTracking()
                .OrderByDescending(t => t.IsDefault)
                .FirstOrDefaultAsync(t => t.IsActive, ct)
            : await _db.BarcodeTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == command.TemplateId, ct);

        if (template is null)
        {
            throw new NotFoundException(
                "Chưa có mẫu tem mã vạch nào. Hãy tạo mẫu ở màn hình Mẫu tem và nhãn trước khi in.");
        }

        var items = await LabelDataLoader.LoadAsync(_db, command.ItemIds, command.Filter, ct);
        var expanded = LabelDataLoader.Repeat(items, command.Copies);

        var pdf = _printer.RenderBarcodes(GetBarcodeTemplatesQueryHandler.Map(template), expanded);

        return new PrintedFileDto(
            pdf,
            $"tem-ma-vach-{_clock.Today:yyyyMMdd}.pdf",
            "application/pdf");
    }
}

/// <summary>In nhãn gáy sách (III.2).</summary>
public class PrintSpineLabelsCommand : BulkItemCommand, IRequest<PrintedFileDto>
{
    public Guid? TemplateId { get; set; }
    public int Copies { get; set; } = 1;
}

public class PrintSpineLabelsCommandValidator : AbstractValidator<PrintSpineLabelsCommand>
{
    public PrintSpineLabelsCommandValidator()
    {
        RuleFor(command => command.Copies).InclusiveBetween(1, 10)
            .WithMessage("Số bản mỗi nhãn phải từ 1 đến 10.");
    }
}

public class PrintSpineLabelsCommandHandler : IRequestHandler<PrintSpineLabelsCommand, PrintedFileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ILabelPrintService _printer;
    private readonly IDateTimeProvider _clock;

    public PrintSpineLabelsCommandHandler(
        IApplicationDbContext db, ILabelPrintService printer, IDateTimeProvider clock)
    {
        _db = db;
        _printer = printer;
        _clock = clock;
    }

    public async Task<PrintedFileDto> Handle(PrintSpineLabelsCommand command, CancellationToken ct)
    {
        var template = command.TemplateId is null
            ? await _db.LabelTemplates.AsNoTracking()
                .OrderByDescending(t => t.IsDefault)
                .FirstOrDefaultAsync(t => t.IsActive, ct)
            : await _db.LabelTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == command.TemplateId, ct);

        if (template is null)
        {
            throw new NotFoundException(
                "Chưa có mẫu nhãn gáy nào. Hãy tạo mẫu ở màn hình Mẫu tem và nhãn trước khi in.");
        }

        var items = await LabelDataLoader.LoadAsync(_db, command.ItemIds, command.Filter, ct);
        var expanded = LabelDataLoader.Repeat(items, command.Copies);

        var pdf = _printer.RenderLabels(GetLabelTemplatesQueryHandler.Map(template), expanded);

        return new PrintedFileDto(
            pdf,
            $"nhan-gay-{_clock.Today:yyyyMMdd}.pdf",
            "application/pdf");
    }
}

/// <summary>Đọc dữ liệu ĐKCB cần thiết để đổ lên tem.</summary>
internal static class LabelDataLoader
{
    /// <summary>Chặn trên số tem một lần in: tệp PDF quá lớn thì trình duyệt không mở nổi.</summary>
    private const int MaxLabels = 5000;

    public static async Task<IReadOnlyList<LabelDataDto>> LoadAsync(
        IApplicationDbContext db, IReadOnlyCollection<Guid> ids, StockItemFilter? filter, CancellationToken ct)
    {
        var items = await StockItemQuery.Selection(db, ids, filter)
            .AsNoTracking()
            .OrderBy(item => item.Barcode)
            .Take(MaxLabels + 1)
            .Select(item => new LabelDataDto
            {
                ItemId = item.Id,
                Barcode = item.Barcode,
                RegisterNumber = item.RegisterNumber,
                CallNumber = item.CallNumber,
                Ddc = item.Bib!.Ddc,
                Title = item.Bib!.Title,
                Author = item.Bib!.AuthorMain,
                LibraryName = item.Warehouse!.Library!.Name,
                WarehouseName = item.Warehouse!.Name,
                Isbn = item.Bib!.Isbn,
                PublishYear = item.Bib!.PublishYear,
                Price = item.Price,
                CopyNumber = item.CopyNumber
            })
            .ToListAsync(ct);

        if (items.Count == 0)
        {
            throw new Common.Exceptions.ValidationException(
                "itemIds", "Không có ấn phẩm nào để in. Hãy chọn ấn phẩm hoặc đặt lại bộ lọc.");
        }

        if (items.Count > MaxLabels)
        {
            throw new ConflictException(
                $"Một lần in tối đa {MaxLabels:N0} tem. Hãy thu hẹp bộ lọc rồi in làm nhiều lần.");
        }

        return items;
    }

    public static IReadOnlyList<LabelDataDto> Repeat(IReadOnlyList<LabelDataDto> items, int copies)
    {
        if (copies <= 1)
        {
            return items;
        }

        // Các bản của cùng một ĐKCB nằm liền nhau: cán bộ bóc tem xong dán luôn cả cụm lên một cuốn.
        var expanded = new List<LabelDataDto>(items.Count * copies);

        foreach (var item in items)
        {
            for (var index = 0; index < copies; index++)
            {
                expanded.Add(item);
            }
        }

        return expanded;
    }
}
