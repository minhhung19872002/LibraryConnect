using System.Text.Json;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Application.Features.Marc;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Danh sách mẫu phích đã thiết kế (II.10).</summary>
public record GetCardTemplatesQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<CardTemplateDto>>;

public class GetCardTemplatesQueryHandler : IRequestHandler<GetCardTemplatesQuery, IReadOnlyList<CardTemplateDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCardTemplatesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CardTemplateDto>> Handle(GetCardTemplatesQuery query, CancellationToken ct)
    {
        var templates = await _db.CardTemplates
            .AsNoTracking()
            .WhereIf(!query.IncludeInactive, template => template.IsActive)
            .OrderBy(template => template.CardType)
            .ThenBy(template => template.Name)
            .ToListAsync(ct);

        return templates.Select(CardTemplateMapper.ToDto).ToList();
    }
}

/// <summary>Thêm hoặc sửa một mẫu phích.</summary>
public class SaveCardTemplateCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CardType { get; set; } = CardTypes.Main;
    public double WidthMm { get; set; } = 125;
    public double HeightMm { get; set; } = 75;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public CardLayoutDto Layout { get; set; } = new();
}

public class SaveCardTemplateCommandValidator : AbstractValidator<SaveCardTemplateCommand>
{
    public SaveCardTemplateCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên mẫu phích.")
            .MaximumLength(200).WithMessage("Tên mẫu tối đa 200 ký tự.");

        RuleFor(command => command.CardType)
            .Must(type => CardTypes.Labels.ContainsKey(type))
            .WithMessage("Loại phích không hợp lệ.");

        // The standard Vietnamese catalogue card is 12.5 × 7.5 cm; the range allows a library to use
        // a different stock without allowing a size that cannot be printed.
        RuleFor(command => command.WidthMm)
            .InclusiveBetween(50, 210).WithMessage("Chiều rộng phích phải từ 50 đến 210 mm.");

        RuleFor(command => command.HeightMm)
            .InclusiveBetween(40, 297).WithMessage("Chiều cao phích phải từ 40 đến 297 mm.");
    }
}

public class SaveCardTemplateCommandHandler : IRequestHandler<SaveCardTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveCardTemplateCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveCardTemplateCommand request, CancellationToken ct)
    {
        foreach (var box in request.Layout.Boxes)
        {
            if (box.X + box.Width > request.WidthMm + 0.01 || box.Y + box.Height > request.HeightMm + 0.01)
            {
                throw new Common.Exceptions.ValidationException("Layout",
                    $"Ô \"{box.Source}\" nằm ngoài khổ phích {request.WidthMm}×{request.HeightMm} mm. " +
                    "Hãy thu nhỏ ô hoặc tăng khổ phích.");
            }
        }

        var entity = request.Id is null
            ? null
            : await _db.CardTemplates.FirstOrDefaultAsync(template => template.Id == request.Id, ct);

        if (request.Id is not null && entity is null)
        {
            throw new NotFoundException("Không tìm thấy mẫu phích cần sửa.");
        }

        var code = string.IsNullOrWhiteSpace(request.Code)
            ? VietnameseText.Slugify(request.Name).ToUpperInvariant()
            : request.Code.Trim().ToUpperInvariant();

        var taken = await _db.CardTemplates.AnyAsync(template => template.Code == code && template.Id != request.Id, ct);

        if (taken)
        {
            throw new ConflictException($"Mã mẫu \"{code}\" đã được dùng cho một mẫu phích khác.");
        }

        if (entity is null)
        {
            entity = new CardTemplate { Id = Guid.NewGuid() };
            _db.CardTemplates.Add(entity);
        }

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.CardType = request.CardType;
        entity.WidthMm = request.WidthMm;
        entity.HeightMm = request.HeightMm;
        entity.IsActive = request.IsActive;
        entity.Layout = JsonSerializer.Serialize(request.Layout, CardTemplateMapper.JsonOptions);

        if (request.IsDefault)
        {
            // One default per card type, otherwise "the default" depends on row order.
            var siblings = await _db.CardTemplates
                .Where(template => template.CardType == request.CardType && template.Id != entity.Id)
                .ToListAsync(ct);

            foreach (var sibling in siblings)
            {
                sibling.IsDefault = false;
            }
        }

        entity.IsDefault = request.IsDefault;

        await _db.SaveChangesAsync(ct);

        return entity.Id;
    }
}

public record DeleteCardTemplateCommand(Guid Id) : IRequest;

public class DeleteCardTemplateCommandHandler : IRequestHandler<DeleteCardTemplateCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCardTemplateCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCardTemplateCommand request, CancellationToken ct)
    {
        var entity = await _db.CardTemplates.FirstOrDefaultAsync(template => template.Id == request.Id, ct)
                     ?? throw new NotFoundException("Không tìm thấy mẫu phích cần xóa.");

        _db.CardTemplates.Remove(entity);

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Kết xuất phích ra PDF (II.10).</summary>
public record PrintCardsCommand(PrintCardsRequestDto Request) : IRequest<MarcExportFileDto>;

public class PrintCardsCommandHandler : IRequestHandler<PrintCardsCommand, MarcExportFileDto>
{
    /// <summary>Giới hạn một lần in, để một thao tác lỡ tay không dựng vài nghìn trang PDF.</summary>
    private const int MaxRecords = 2_000;

    private readonly IApplicationDbContext _db;
    private readonly ICardPrintService _printer;
    private readonly IDateTimeProvider _clock;

    public PrintCardsCommandHandler(
        IApplicationDbContext db,
        ICardPrintService printer,
        IDateTimeProvider clock)
    {
        _db = db;
        _printer = printer;
        _clock = clock;
    }

    public async Task<MarcExportFileDto> Handle(PrintCardsCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (request.CardTypes.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("CardTypes", "Chưa chọn loại phích cần in.");
        }

        var template = await FindTemplateAsync(request, ct);

        var query = request.BibIds.Count > 0
            ? _db.BibRecords.AsNoTracking().Where(bib => request.BibIds.Contains(bib.Id))
            : GetBibRecordsQueryHandler.Filter(_db.BibRecords.AsNoTracking(), request.Filter ?? new BibListRequest());

        var total = await query.CountAsync(ct);

        if (total == 0)
        {
            throw new Common.Exceptions.ValidationException("BibIds",
                "Không có biểu ghi nào khớp với lựa chọn, nên không in được phích.");
        }

        if (!request.Preview && total > MaxRecords)
        {
            throw new Common.Exceptions.ValidationException("BibIds",
                $"Bộ lọc đang khớp {total:N0} biểu ghi, vượt giới hạn {MaxRecords:N0} biểu ghi một lần in. " +
                "Hãy thu hẹp bộ lọc.");
        }

        var ordered = query.OrderBy(bib => bib.Title);

        var records = await (request.Preview
                ? ordered.Take(Math.Clamp(request.PreviewRecords, 1, 20))
                : ordered)
            .Select(bib => new { bib.Id, bib.MarcData })
            .ToListAsync(ct);

        var ids = records.Select(record => record.Id).ToList();

        // The shelf mark lives on the copies, not on the record, so it is read from the first copy
        // of each title — which is what a card carries.
        var callNumbers = await _db.Items
            .AsNoTracking()
            .Where(item => ids.Contains(item.BibId) && item.CallNumber != null)
            .GroupBy(item => item.BibId)
            .Select(group => new { BibId = group.Key, CallNumber = group.Min(item => item.CallNumber) })
            .ToDictionaryAsync(item => item.BibId, item => item.CallNumber, ct);

        var cards = new List<CardToPrint>();

        foreach (var record in records)
        {
            var marc = MarcJson.Deserialize(record.MarcData);
            callNumbers.TryGetValue(record.Id, out var callNumber);

            foreach (var content in CardContentBuilder.Build(marc, request.CardTypes, callNumber))
            {
                cards.Add(new CardToPrint(content, marc));
            }
        }

        if (cards.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("CardTypes",
                "Các biểu ghi đã chọn không có dữ liệu cho loại phích này. Ví dụ phích chủ đề cần " +
                "biểu ghi có đề mục chủ đề.");
        }

        var pdf = _printer.Render(template, cards, request.MultiplePerPage);

        return new MarcExportFileDto
        {
            Content = pdf,
            FileName = $"phich-{_clock.Now:yyyyMMdd-HHmmss}.pdf",
            ContentType = "application/pdf"
        };
    }

    private async Task<CardTemplateDto> FindTemplateAsync(PrintCardsRequestDto request, CancellationToken ct)
    {
        if (request.TemplateId is not null)
        {
            var chosen = await _db.CardTemplates.AsNoTracking()
                             .FirstOrDefaultAsync(template => template.Id == request.TemplateId, ct)
                         ?? throw new NotFoundException("Không tìm thấy mẫu phích đã chọn.");

            return CardTemplateMapper.ToDto(chosen);
        }

        var cardType = request.CardTypes.FirstOrDefault() ?? CardTypes.Main;

        var fallback = await _db.CardTemplates.AsNoTracking()
            .Where(template => template.IsActive)
            .OrderByDescending(template => template.CardType == cardType)
            .ThenByDescending(template => template.IsDefault)
            .FirstOrDefaultAsync(ct);

        return fallback is null
            ? CardTemplateMapper.Fallback()
            : CardTemplateMapper.ToDto(fallback);
    }
}

internal static class CardTemplateMapper
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static CardTemplateDto ToDto(CardTemplate entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        CardType = entity.CardType,
        CardTypeName = CardTypes.Labels.TryGetValue(entity.CardType, out var label) ? label : entity.CardType,
        WidthMm = entity.WidthMm,
        HeightMm = entity.HeightMm,
        IsDefault = entity.IsDefault,
        IsActive = entity.IsActive,
        Layout = ReadLayout(entity.Layout)
    };

    private static CardLayoutDto ReadLayout(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new CardLayoutDto();
        }

        try
        {
            return JsonSerializer.Deserialize<CardLayoutDto>(json, JsonOptions) ?? new CardLayoutDto();
        }
        catch (JsonException)
        {
            // A template whose layout has been corrupted still prints, as an empty card; refusing to
            // print at all would be worse for the librarian standing at the printer.
            return new CardLayoutDto();
        }
    }

    /// <summary>
    /// Mẫu phích dùng khi thư viện chưa thiết kế mẫu nào: khổ chuẩn 12,5 × 7,5 cm với bố cục quen
    /// thuộc — ký hiệu xếp giá bên trái, tiêu đề trên cùng, mô tả ISBD ở giữa, dòng truy hồi ở chân.
    /// </summary>
    public static CardTemplateDto Fallback() => new()
    {
        Code = "MACDINH",
        Name = "Mẫu phích chuẩn 12,5 × 7,5 cm",
        CardType = CardTypes.Main,
        CardTypeName = CardTypes.Labels[CardTypes.Main],
        WidthMm = 125,
        HeightMm = 75,
        IsDefault = true,
        IsActive = true,
        Layout = new CardLayoutDto
        {
            Padding = 5,
            ShowBorder = true,
            Boxes = new List<CardBoxDto>
            {
                new() { X = 0, Y = 0, Width = 28, Height = 20, Source = "callNumber", FontSize = 9, Bold = true },
                new() { X = 30, Y = 0, Width = 85, Height = 8, Source = "heading", FontSize = 10, Bold = true },
                new() { X = 30, Y = 9, Width = 85, Height = 32, Source = "isbd", FontSize = 8 },
                new() { X = 0, Y = 45, Width = 115, Height = 14, Source = "tracings", FontSize = 7 },
                new() { X = 0, Y = 60, Width = 60, Height = 5, Source = "controlNumber", FontSize = 7, Prefix = "SKS: " }
            }
        }
    };
}
