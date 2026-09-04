using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Domain.Entities.Rdr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Readers;

// ---------------------------------------------------------------------------------------------
// VI.2 — Quản lý mẫu thẻ và in thẻ bạn đọc.
// ---------------------------------------------------------------------------------------------

/// <summary>Bố cục mặt thẻ lưu dạng JSON; tùy chọn đọc/ghi gom về một chỗ.</summary>
internal static class CardLayoutJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static CardFaceLayoutDto Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new CardFaceLayoutDto();
        }

        try
        {
            return JsonSerializer.Deserialize<CardFaceLayoutDto>(json, Options) ?? new CardFaceLayoutDto();
        }
        catch (JsonException)
        {
            // Mẫu hỏng thì trả bố cục rỗng để cán bộ sửa lại được trên màn hình thiết kế; ném lỗi ở
            // đây sẽ khóa luôn cả danh sách mẫu thẻ.
            return new CardFaceLayoutDto();
        }
    }

    public static string Write(CardFaceLayoutDto layout) => JsonSerializer.Serialize(layout, Options);
}

public record GetReaderCardTemplatesQuery(bool IncludeInactive = false)
    : IRequest<IReadOnlyList<ReaderCardTemplateDto>>;

public class GetReaderCardTemplatesQueryHandler
    : IRequestHandler<GetReaderCardTemplatesQuery, IReadOnlyList<ReaderCardTemplateDto>>
{
    private readonly IApplicationDbContext _db;

    public GetReaderCardTemplatesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReaderCardTemplateDto>> Handle(
        GetReaderCardTemplatesQuery query, CancellationToken ct)
    {
        var templates = await _db.ReaderCardTemplates
            .AsNoTracking()
            .WhereIf(!query.IncludeInactive, template => template.IsActive)
            .OrderByDescending(template => template.IsDefault)
            .ThenBy(template => template.Name)
            .ToListAsync(ct);

        return templates.Select(Map).ToList();
    }

    internal static ReaderCardTemplateDto Map(ReaderCardTemplate template) => new()
    {
        Id = template.Id,
        Code = template.Code,
        Name = template.Name,
        WidthMm = template.WidthMm,
        HeightMm = template.HeightMm,
        CardsPerPage = template.CardsPerPage,
        IsDefault = template.IsDefault,
        IsActive = template.IsActive,
        PrintBack = template.PrintBack,
        Front = CardLayoutJson.Read(template.FrontLayout),
        Back = CardLayoutJson.Read(template.BackLayout)
    };
}

public class SaveReaderCardTemplateCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double WidthMm { get; set; } = 85.6;
    public double HeightMm { get; set; } = 54;
    public int CardsPerPage { get; set; } = 10;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool PrintBack { get; set; }
    public CardFaceLayoutDto Front { get; set; } = new();
    public CardFaceLayoutDto Back { get; set; } = new();
}

public class SaveReaderCardTemplateCommandValidator : AbstractValidator<SaveReaderCardTemplateCommand>
{
    public SaveReaderCardTemplateCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("Chưa nhập mã mẫu thẻ.").MaximumLength(100);
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên mẫu thẻ.").MaximumLength(300);

        RuleFor(command => command.WidthMm)
            .InclusiveBetween(40, 210).WithMessage("Chiều rộng thẻ phải từ 40 đến 210 mm.");
        RuleFor(command => command.HeightMm)
            .InclusiveBetween(30, 297).WithMessage("Chiều cao thẻ phải từ 30 đến 297 mm.");
        RuleFor(command => command.CardsPerPage)
            .InclusiveBetween(1, 24).WithMessage("Số thẻ trên một tờ A4 phải từ 1 đến 24.");

        RuleFor(command => command.Front)
            .Must(HasContent).WithMessage("Mặt trước thẻ chưa có nội dung nào.");

        RuleFor(command => command)
            .Must(command => Fits(command.Front, command.WidthMm, command.HeightMm))
            .WithMessage("Có nội dung nằm ngoài khổ thẻ ở mặt trước.")
            .OverridePropertyName(nameof(SaveReaderCardTemplateCommand.Front));

        RuleFor(command => command)
            .Must(command => Fits(command.Back, command.WidthMm, command.HeightMm))
            .WithMessage("Có nội dung nằm ngoài khổ thẻ ở mặt sau.")
            .OverridePropertyName(nameof(SaveReaderCardTemplateCommand.Back));
    }

    private static bool HasContent(CardFaceLayoutDto face) =>
        face.Boxes.Count > 0 || face.Images.Count > 0 || face.Barcode is not null;

    /// <summary>
    /// Mọi khối phải nằm trọn trong khổ thẻ. Máy in thẻ nhựa cắt phăng phần tràn ra ngoài, nên bắt
    /// lỗi lúc lưu mẫu rẻ hơn nhiều so với lúc phát hiện ra cả hộp phôi thẻ đã in hỏng.
    /// </summary>
    private static bool Fits(CardFaceLayoutDto face, double width, double height)
    {
        const double tolerance = 0.5;

        foreach (var box in face.Boxes)
        {
            if (box.X < -tolerance || box.Y < -tolerance
                || box.X + box.Width > width + tolerance
                || box.Y + box.Height > height + tolerance)
            {
                return false;
            }
        }

        foreach (var image in face.Images)
        {
            if (image.X < -tolerance || image.Y < -tolerance
                || image.X + image.Width > width + tolerance
                || image.Y + image.Height > height + tolerance)
            {
                return false;
            }
        }

        if (face.Barcode is { } barcode)
        {
            if (barcode.X < -tolerance || barcode.Y < -tolerance
                || barcode.X + barcode.Width > width + tolerance
                || barcode.Y + barcode.Height > height + tolerance)
            {
                return false;
            }
        }

        return true;
    }
}

public class SaveReaderCardTemplateCommandHandler : IRequestHandler<SaveReaderCardTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveReaderCardTemplateCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveReaderCardTemplateCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim();

        ReaderCardTemplate template;

        if (command.Id is null)
        {
            template = new ReaderCardTemplate();
            _db.ReaderCardTemplates.Add(template);
        }
        else
        {
            template = await _db.ReaderCardTemplates
                .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                ?? throw new NotFoundException("mẫu thẻ", command.Id);
        }

        if (await _db.ReaderCardTemplates.AnyAsync(other => other.Id != template.Id && other.Code == code, ct))
        {
            throw new Common.Exceptions.ValidationException(
                nameof(command.Code), $"Mã mẫu thẻ {code} đã tồn tại.");
        }

        template.Code = code;
        template.Name = command.Name.Trim();
        template.WidthMm = command.WidthMm;
        template.HeightMm = command.HeightMm;
        template.CardsPerPage = command.CardsPerPage;
        template.IsActive = command.IsActive;
        template.IsDefault = command.IsDefault;
        template.PrintBack = command.PrintBack;
        template.FrontLayout = CardLayoutJson.Write(command.Front);
        template.BackLayout = CardLayoutJson.Write(command.Back);

        if (command.IsDefault)
        {
            foreach (var other in await _db.ReaderCardTemplates
                         .Where(entity => entity.Id != template.Id && entity.IsDefault)
                         .ToListAsync(ct))
            {
                other.IsDefault = false;
            }
        }

        await _db.SaveChangesAsync(ct);
        return template.Id;
    }
}

public record DeleteReaderCardTemplateCommand(Guid Id) : IRequest;

public class DeleteReaderCardTemplateCommandHandler : IRequestHandler<DeleteReaderCardTemplateCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteReaderCardTemplateCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteReaderCardTemplateCommand command, CancellationToken ct)
    {
        var template = await _db.ReaderCardTemplates
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("mẫu thẻ", command.Id);

        var printed = await _db.ReaderCards.CountAsync(card => card.TemplateId == template.Id, ct);

        if (printed > 0)
        {
            throw new ConflictException(
                $"Mẫu thẻ {template.Name} đã dùng để in {printed} thẻ nên chỉ ngừng sử dụng được, không xóa được.");
        }

        _db.ReaderCardTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Danh sách trường kéo được lên thẻ, đổ vào bảng chọn của màn hình thiết kế.</summary>
/// <summary>Tải ảnh nền cho mẫu thẻ lên kho đối tượng (VI.2); trả về khoá để ghi vào bố cục mặt thẻ.</summary>
public record UploadCardArtworkCommand(byte[] Content) : IRequest<string>;

public class UploadCardArtworkCommandHandler : IRequestHandler<UploadCardArtworkCommand, string>
{
    private readonly IFileStorage _storage;

    public UploadCardArtworkCommandHandler(IFileStorage storage) => _storage = storage;

    public async Task<string> Handle(UploadCardArtworkCommand command, CancellationToken ct)
    {
        if (command.Content.Length == 0)
        {
            throw new Common.Exceptions.ValidationException("file", "Tệp ảnh rỗng.");
        }

        if (command.Content.Length > CardArtwork.MaxSizeBytes)
        {
            throw new Common.Exceptions.ValidationException(
                "file", $"Ảnh nền tối đa {CardArtwork.MaxSizeBytes / 1024 / 1024} MB.");
        }

        var contentType = ReaderPhotos.DetectImageType(command.Content)
            ?? throw new Common.Exceptions.ValidationException("file", "Tệp tải lên không phải ảnh JPG hoặc PNG.");

        var objectName = $"{CardArtwork.Prefix}{Guid.NewGuid():N}{(contentType == "image/png" ? ".png" : ".jpg")}";

        await _storage.EnsureBucketAsync(CardArtwork.Bucket, ct);

        using var stream = new MemoryStream(command.Content);
        await _storage.UploadAsync(CardArtwork.Bucket, objectName, stream, contentType, ct);

        return objectName;
    }
}

/// <summary>Đọc lại ảnh nền đã tải, cho trình thiết kế hiển thị.</summary>
public record GetCardArtworkQuery(string Key) : IRequest<ReaderPhotoDto>;

public class GetCardArtworkQueryHandler : IRequestHandler<GetCardArtworkQuery, ReaderPhotoDto>
{
    private readonly IFileStorage _storage;

    public GetCardArtworkQueryHandler(IFileStorage storage) => _storage = storage;

    public async Task<ReaderPhotoDto> Handle(GetCardArtworkQuery query, CancellationToken ct)
    {
        // Chỉ phục vụ đúng tiền tố của ảnh nền: khoá là chuỗi người gọi gửi lên, không được để nó
        // trỏ sang ảnh bạn đọc hay tệp khác trong cùng bucket.
        if (string.IsNullOrWhiteSpace(query.Key)
            || !query.Key.StartsWith(CardArtwork.Prefix, StringComparison.Ordinal)
            || query.Key.Contains("..", StringComparison.Ordinal))
        {
            throw new NotFoundException("ảnh nền mẫu thẻ", query.Key);
        }

        var bytes = await ReaderPhotoWriter.LoadAsync(_storage, query.Key, ct)
            ?? throw new NotFoundException("ảnh nền mẫu thẻ", query.Key);

        return new ReaderPhotoDto(bytes, ReaderPhotos.DetectImageType(bytes) ?? "application/octet-stream");
    }
}

public record GetReaderCardFieldsQuery : IRequest<IReadOnlyList<CardFieldOptionDto>>;

public record CardFieldOptionDto(string Key, string Label);

public class GetReaderCardFieldsQueryHandler
    : IRequestHandler<GetReaderCardFieldsQuery, IReadOnlyList<CardFieldOptionDto>>
{
    public Task<IReadOnlyList<CardFieldOptionDto>> Handle(
        GetReaderCardFieldsQuery query, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CardFieldOptionDto>>(
            ReaderCardFields.Labels.Select(pair => new CardFieldOptionDto(pair.Key, pair.Value)).ToList());
}

/// <summary>
/// In thẻ bạn đọc: một người, một danh sách tick chọn, hoặc toàn bộ kết quả của bộ lọc (VI.2).
/// </summary>
public class PrintReaderCardsCommand : IRequest<PrintedFileDto>
{
    public ReaderSelectionDto Selection { get; set; } = new();
    /// <summary>Bỏ trống thì dùng mẫu mặc định.</summary>
    public Guid? TemplateId { get; set; }
    /// <summary>Bật: xếp nhiều thẻ trên tờ A4. Tắt: mỗi thẻ một trang đúng khổ, cho máy in thẻ nhựa.</summary>
    public bool MultiplePerPage { get; set; } = true;
    /// <summary>Xem trước thì không tăng số lần in của thẻ.</summary>
    public bool Preview { get; set; }
}

public class PrintReaderCardsCommandHandler : IRequestHandler<PrintReaderCardsCommand, PrintedFileDto>
{
    /// <summary>Chặn trên số thẻ mỗi tệp PDF, để tệp còn mở được trên máy quầy.</summary>
    private const int MaxCards = 1000;

    private readonly IApplicationDbContext _db;
    private readonly IReaderCardPrintService _printer;
    private readonly ISystemParameterService _parameters;
    private readonly IFileStorage _storage;
    private readonly IDateTimeProvider _clock;

    public PrintReaderCardsCommandHandler(
        IApplicationDbContext db,
        IReaderCardPrintService printer,
        ISystemParameterService parameters,
        IFileStorage storage,
        IDateTimeProvider clock)
    {
        _db = db;
        _printer = printer;
        _parameters = parameters;
        _storage = storage;
        _clock = clock;
    }

    public async Task<PrintedFileDto> Handle(PrintReaderCardsCommand command, CancellationToken ct)
    {
        var today = _clock.Today;

        var template = await LoadTemplateAsync(command.TemplateId, ct);
        var readers = await ReaderSelectionResolver.ResolveAsync(_db, command.Selection, today, ct);

        if (readers.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("readerIds", "Không có bạn đọc nào để in thẻ.");
        }

        if (readers.Count > MaxCards)
        {
            throw new Common.Exceptions.ValidationException(
                "readerIds",
                $"Mỗi tệp in tối đa {MaxCards:#,##0} thẻ. Hãy chia nhỏ bộ lọc, ví dụ in theo từng lớp.");
        }

        var readerIds = readers.Select(reader => reader.Id).ToList();

        var lookups = await _db.Readers
            .AsNoTracking()
            .Where(reader => readerIds.Contains(reader.Id))
            .Select(reader => new
            {
                reader.Id,
                ReaderTypeName = reader.ReaderType!.Name,
                FacultyName = reader.Faculty!.Name,
                MajorName = reader.Major!.Name
            })
            .ToDictionaryAsync(row => row.Id, ct);

        var cards = new List<ReaderCardDataDto>(readers.Count);

        foreach (var reader in readers)
        {
            lookups.TryGetValue(reader.Id, out var names);

            cards.Add(new ReaderCardDataDto
            {
                ReaderId = reader.Id,
                CardNumber = reader.CardNumber,
                FullName = reader.FullName,
                StudentCode = reader.StudentCode,
                ReaderTypeName = names?.ReaderTypeName,
                FacultyName = names?.FacultyName,
                MajorName = names?.MajorName,
                ClassName = reader.ClassName,
                CourseYear = reader.CourseYear,
                Gender = reader.Gender,
                DateOfBirth = reader.DateOfBirth,
                CardIssueDate = reader.CardIssueDate,
                CardExpireDate = reader.CardExpireDate,
                Email = reader.Email,
                Phone = reader.Phone,
                Address = reader.Address,
                Photo = await ReaderPhotoWriter.LoadAsync(_storage, reader.PhotoUrl, ct)
            });
        }

        var artwork = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var key in new[] { template.Front.BackgroundImage, template.Back.BackgroundImage })
        {
            if (string.IsNullOrWhiteSpace(key) || artwork.ContainsKey(key))
            {
                continue;
            }

            // Thiếu ảnh nền thì thẻ vẫn in — thiếu nền còn hơn không có thẻ đưa cho bạn đọc.
            var bytes = await ReaderPhotoWriter.LoadAsync(_storage, key, ct);

            if (bytes is not null)
            {
                artwork[key] = bytes;
            }
        }

        var library = new CardLibraryInfo(
            await _parameters.GetAsync("LIBRARY.NAME", string.Empty, ct),
            await _parameters.GetAsync("LIBRARY.ADDRESS", string.Empty, ct),
            await _parameters.GetAsync("LIBRARY.PHONE", string.Empty, ct),
            await Admin.Parameters.ParameterFileLoader.LoadAsync(
                _storage, await _parameters.GetAsync("LIBRARY.LOGO_URL", ct), ct),
            artwork);

        var pdf = _printer.Render(template, cards, library, command.MultiplePerPage);

        if (!command.Preview)
        {
            // Đếm số lần in của từng thẻ (VI.2): con số này là căn cứ khi bạn đọc xin cấp lại thẻ
            // lần thứ ba, và cũng để đối chiếu với số phôi thẻ đã xuất kho.
            var current = await _db.ReaderCards
                .Where(card => readerIds.Contains(card.ReaderId) && card.IsCurrent)
                .ToListAsync(ct);

            foreach (var card in current)
            {
                card.PrintCount++;
                card.TemplateId = template.Id;
            }

            await _db.SaveChangesAsync(ct);
        }

        var fileName = readers.Count == 1
            ? $"the-ban-doc-{readers[0].CardNumber}.pdf"
            : $"the-ban-doc-{readers.Count}-the-{today:yyyyMMdd}.pdf";

        return new PrintedFileDto(pdf, fileName, "application/pdf");
    }

    private async Task<ReaderCardTemplateDto> LoadTemplateAsync(Guid? templateId, CancellationToken ct)
    {
        var template = templateId is null
            ? await _db.ReaderCardTemplates
                .AsNoTracking()
                .Where(entity => entity.IsActive)
                .OrderByDescending(entity => entity.IsDefault)
                .ThenBy(entity => entity.Name)
                .FirstOrDefaultAsync(ct)
            : await _db.ReaderCardTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(entity => entity.Id == templateId, ct);

        if (template is null)
        {
            throw new Common.Exceptions.ValidationException(
                nameof(PrintReaderCardsCommand.TemplateId),
                "Chưa có mẫu thẻ nào đang dùng. Vào Bạn đọc → Mẫu thẻ để tạo mẫu trước khi in.");
        }

        return GetReaderCardTemplatesQueryHandler.Map(template);
    }
}
