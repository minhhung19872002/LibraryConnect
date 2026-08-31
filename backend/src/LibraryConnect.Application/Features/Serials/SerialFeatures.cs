using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Entities.Ser;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Serials;

// ---------------------------------------------------------------------------------------------
// IV.1 và IV.4 — Đầu báo, tạp chí: tìm kiếm, khai kỳ hạn, phân kho.
// ---------------------------------------------------------------------------------------------

public class SerialDto
{
    public Guid Id { get; set; }
    public Guid BibId { get; set; }
    public string? ControlNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Issn { get; set; }
    public Guid? PublisherId { get; set; }
    public string? PublisherName { get; set; }
    public Guid? LanguageId { get; set; }
    public string? LanguageName { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public SerialFrequency Frequency { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Guid? ShelfId { get; set; }
    public string? ShelfName { get; set; }
    public string? CallNumber { get; set; }
    public DateOnly? SubscriptionStart { get; set; }
    public DateOnly? SubscriptionEnd { get; set; }
    public decimal? PricePerIssue { get; set; }
    public int CopiesPerIssue { get; set; }
    public bool IsActive { get; set; }
    public string? Note { get; set; }

    /// <summary>Số kỳ đã sinh dự kiến, đã nhận và còn thiếu — cột tình trạng của danh sách.</summary>
    public int ExpectedCount { get; set; }
    public int ReceivedCount { get; set; }
    public int MissingCount { get; set; }
    /// <summary>Đơn đặt hết hạn trong vòng 60 ngày — nhắc cán bộ gia hạn.</summary>
    public bool SubscriptionEndingSoon { get; set; }
}

public class SerialDetailDto : SerialDto
{
    public SerialPatternDto Pattern { get; set; } = new();
}

public class SerialListRequest : PagedRequest
{
    public SerialFrequency? Frequency { get; set; }
    public Guid? PublisherId { get; set; }
    public Guid? LanguageId { get; set; }
    public Guid? WarehouseId { get; set; }
    public bool? IsActive { get; set; }
    /// <summary>Chỉ các đầu báo đang trong thời gian đặt mua.</summary>
    public bool? SubscribedOnly { get; set; }
}

/// <summary>Tìm kiếm báo, tạp chí (IV.1).</summary>
public record SearchSerialsQuery(SerialListRequest Request) : IRequest<PagedResult<SerialDto>>;

public class SearchSerialsQueryHandler : IRequestHandler<SearchSerialsQuery, PagedResult<SerialDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public SearchSerialsQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<SerialDto>> Handle(SearchSerialsQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var today = _clock.Today;

        var serials = _db.Serials
            .AsNoTracking()
            .WhereIf(request.Frequency is not null, serial => serial.Frequency == request.Frequency)
            .WhereIf(request.PublisherId is not null, serial => serial.PublisherId == request.PublisherId)
            .WhereIf(request.LanguageId is not null, serial => serial.LanguageId == request.LanguageId)
            .WhereIf(request.WarehouseId is not null, serial => serial.WarehouseId == request.WarehouseId)
            .WhereIf(request.IsActive is not null, serial => serial.IsActive == request.IsActive)
            .WhereIf(request.SubscribedOnly == true,
                serial => serial.SubscriptionStart <= today
                          && (serial.SubscriptionEnd == null || serial.SubscriptionEnd >= today));

        if (request.HasKeyword())
        {
            var keyword = VietnameseText.RemoveDiacritics(request.Keyword!.Trim()).ToLowerInvariant();

            serials = serials.Where(serial =>
                DatabaseFunctions.Unaccent(serial.Title).Contains(keyword)
                || (serial.Issn != null && serial.Issn.ToLower().Contains(keyword)));
        }

        var total = await serials.CountAsync(ct);

        var page = await serials
            .OrderBy(serial => serial.Title)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(Projection)
            .ToListAsync(ct);

        foreach (var row in page)
        {
            row.SubscriptionEndingSoon = row.IsActive
                                         && row.SubscriptionEnd is not null
                                         && row.SubscriptionEnd >= today
                                         && row.SubscriptionEnd.Value.DayNumber - today.DayNumber <= 60;
        }

        return new PagedResult<SerialDto>(page, total, request.Page, request.PageSize);
    }

    internal static System.Linq.Expressions.Expression<Func<Serial, SerialDto>> Projection => serial =>
        new SerialDto
        {
            Id = serial.Id,
            BibId = serial.BibId,
            ControlNumber = serial.Bib!.ControlNumber,
            Title = serial.Title,
            Issn = serial.Issn,
            PublisherId = serial.PublisherId,
            PublisherName = serial.Publisher!.Name,
            LanguageId = serial.LanguageId,
            LanguageName = serial.Language!.Name,
            SupplierId = serial.SupplierId,
            SupplierName = serial.Supplier!.Name,
            Frequency = serial.Frequency,
            WarehouseId = serial.WarehouseId,
            WarehouseName = serial.Warehouse!.Name,
            ShelfId = serial.ShelfId,
            CallNumber = serial.CallNumber,
            SubscriptionStart = serial.SubscriptionStart,
            SubscriptionEnd = serial.SubscriptionEnd,
            PricePerIssue = serial.PricePerIssue,
            CopiesPerIssue = serial.CopiesPerIssue,
            IsActive = serial.IsActive,
            Note = serial.Note,
            ExpectedCount = serial.Issues.Count(issue => issue.Status == SerialIssueStatus.Expected),
            ReceivedCount = serial.Issues.Count(issue =>
                issue.Status == SerialIssueStatus.Received || issue.Status == SerialIssueStatus.Bound),
            MissingCount = serial.Issues.Count(issue =>
                issue.Status == SerialIssueStatus.Missing || issue.Status == SerialIssueStatus.Claimed)
        };
}

public record GetSerialQuery(Guid Id) : IRequest<SerialDetailDto>;

public class GetSerialQueryHandler : IRequestHandler<GetSerialQuery, SerialDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetSerialQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<SerialDetailDto> Handle(GetSerialQuery query, CancellationToken ct)
    {
        var serial = await _db.Serials
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == query.Id, ct)
            ?? throw new NotFoundException("ấn phẩm định kỳ", query.Id);

        var row = await _db.Serials
            .AsNoTracking()
            .Where(entity => entity.Id == query.Id)
            .Select(SearchSerialsQueryHandler.Projection)
            .FirstAsync(ct);

        var shelfName = serial.ShelfId is null
            ? null
            : await _db.Shelves.AsNoTracking()
                .Where(shelf => shelf.Id == serial.ShelfId)
                .Select(shelf => shelf.Name)
                .FirstOrDefaultAsync(ct);

        return new SerialDetailDto
        {
            Id = row.Id,
            BibId = row.BibId,
            ControlNumber = row.ControlNumber,
            Title = row.Title,
            Issn = row.Issn,
            PublisherId = row.PublisherId,
            PublisherName = row.PublisherName,
            LanguageId = row.LanguageId,
            LanguageName = row.LanguageName,
            SupplierId = row.SupplierId,
            SupplierName = row.SupplierName,
            Frequency = row.Frequency,
            WarehouseId = row.WarehouseId,
            WarehouseName = row.WarehouseName,
            ShelfId = row.ShelfId,
            ShelfName = shelfName,
            CallNumber = row.CallNumber,
            SubscriptionStart = row.SubscriptionStart,
            SubscriptionEnd = row.SubscriptionEnd,
            PricePerIssue = row.PricePerIssue,
            CopiesPerIssue = row.CopiesPerIssue,
            IsActive = row.IsActive,
            Note = row.Note,
            ExpectedCount = row.ExpectedCount,
            ReceivedCount = row.ReceivedCount,
            MissingCount = row.MissingCount,
            Pattern = SerialPatternDto.Read(serial.FrequencyConfig)
        };
    }
}

/// <summary>Tạo hoặc sửa một đầu báo, tạp chí (IV.4).</summary>
public class SaveSerialCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Issn { get; set; }
    public Guid? PublisherId { get; set; }
    public Guid? LanguageId { get; set; }
    public Guid? SupplierId { get; set; }
    public SerialFrequency Frequency { get; set; } = SerialFrequency.Monthly;
    public SerialPatternDto Pattern { get; set; } = new();
    public Guid? WarehouseId { get; set; }
    public Guid? ShelfId { get; set; }
    public string? CallNumber { get; set; }
    public DateOnly? SubscriptionStart { get; set; }
    public DateOnly? SubscriptionEnd { get; set; }
    public decimal? PricePerIssue { get; set; }
    public int CopiesPerIssue { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    /// <summary>Chỉ số DDC, ghi vào biểu ghi để báo cáo theo môn loại có số liệu.</summary>
    public string? Ddc { get; set; }
}

public class SaveSerialCommandValidator : AbstractValidator<SaveSerialCommand>
{
    public SaveSerialCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Chưa nhập tên báo / tạp chí.").MaximumLength(1000);

        RuleFor(command => command.CopiesPerIssue)
            .InclusiveBetween(1, 100).WithMessage("Số bản mỗi kỳ phải từ 1 đến 100.");

        RuleFor(command => command.PricePerIssue)
            .GreaterThanOrEqualTo(0).When(command => command.PricePerIssue.HasValue)
            .WithMessage("Đơn giá mỗi kỳ không được âm.");

        RuleFor(command => command)
            .Must(command => command.SubscriptionEnd is null || command.SubscriptionStart is null
                             || command.SubscriptionEnd >= command.SubscriptionStart)
            .WithMessage("Thời gian đặt mua: ngày kết thúc phải sau ngày bắt đầu.");

        RuleFor(command => command.Pattern.DayOfWeek)
            .InclusiveBetween(1, 7).When(command => command.Pattern.DayOfWeek.HasValue)
            .WithMessage("Thứ phát hành phải từ 1 (thứ Hai) đến 7 (Chủ nhật).");

        RuleFor(command => command.Pattern.DayOfMonth)
            .InclusiveBetween(1, 31).When(command => command.Pattern.DayOfMonth.HasValue)
            .WithMessage("Ngày phát hành phải từ 1 đến 31.");

        RuleFor(command => command.Pattern.IssuesPerYear)
            .GreaterThan(0).When(command => command.Frequency == SerialFrequency.Irregular)
            .WithMessage("Kỳ hạn không định kỳ thì phải khai số kỳ trong năm để hệ thống dựng khung số.");
    }
}

public class SaveSerialCommandHandler : IRequestHandler<SaveSerialCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IBibRecordWriter _writer;

    public SaveSerialCommandHandler(IApplicationDbContext db, IBibRecordWriter writer)
    {
        _db = db;
        _writer = writer;
    }

    public async Task<Guid> Handle(SaveSerialCommand command, CancellationToken ct)
    {
        Serial serial;

        if (command.Id is null)
        {
            // Mỗi đầu báo là một biểu ghi thư mục: đó là thứ bạn đọc tìm thấy trên OPAC, còn dòng
            // ở bảng ser.serials chỉ mang thông tin đặt mua và kỳ hạn.
            var bib = new BibRecord
            {
                Id = Guid.NewGuid(),
                Source = BibSource.Manual,
                Status = RecordStatus.Draft
            };

            var marc = BuildSerialMarc(command);

            await _writer.PrepareAsync(bib, marc, ct);
            _db.BibRecords.Add(bib);
            await _writer.ApplyAsync(bib, marc, isNew: true, changeNote: "Khai báo ấn phẩm định kỳ", ct);

            serial = new Serial { Id = Guid.NewGuid(), BibId = bib.Id };
            _db.Serials.Add(serial);
        }
        else
        {
            serial = await _db.Serials.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                ?? throw new NotFoundException("ấn phẩm định kỳ", command.Id);

            var bib = await _db.BibRecords.FirstOrDefaultAsync(record => record.Id == serial.BibId, ct);

            if (bib is not null)
            {
                var marc = BuildSerialMarc(command);
                await _writer.ApplyAsync(bib, marc, isNew: false, changeNote: "Cập nhật ấn phẩm định kỳ", ct);
            }
        }

        serial.Title = command.Title.Trim();
        serial.Issn = MarcProjection.NormaliseStandardNumber(command.Issn);
        serial.PublisherId = command.PublisherId;
        serial.LanguageId = command.LanguageId;
        serial.SupplierId = command.SupplierId;
        serial.Frequency = command.Frequency;
        serial.FrequencyConfig = SerialPatternDto.Write(command.Pattern);
        serial.WarehouseId = command.WarehouseId;
        serial.ShelfId = command.ShelfId;
        serial.CallNumber = command.CallNumber?.Trim();
        serial.SubscriptionStart = command.SubscriptionStart;
        serial.SubscriptionEnd = command.SubscriptionEnd;
        serial.PricePerIssue = command.PricePerIssue;
        serial.CopiesPerIssue = command.CopiesPerIssue;
        serial.IsActive = command.IsActive;
        serial.Note = command.Note?.Trim();

        await _db.SaveChangesAsync(ct);
        return serial.Id;
    }

    /// <summary>
    /// Biểu ghi MARC 21 của một đầu ấn phẩm định kỳ.
    ///
    /// Khác biểu ghi sách ở ba chỗ mà phần mềm khác đọc vào sẽ dùng để nhận ra đây là ấn phẩm nhiều
    /// kỳ: leader vị trí 07 là 's', trường 022 mang ISSN, và trường 310 ghi kỳ hạn hiện tại.
    /// </summary>
    private static MarcRecord BuildSerialMarc(SaveSerialCommand command)
    {
        var record = new MarcRecord();
        record.Leader.RecordStatus = 'n';
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 's';
        record.Leader.CharacterCodingScheme = 'a';
        record.Leader.EncodingLevel = '3';

        if (!string.IsNullOrWhiteSpace(command.Issn))
        {
            record.AddField("022").AddSubfield('a', command.Issn.Trim());
        }

        if (!string.IsNullOrWhiteSpace(command.Ddc))
        {
            record.AddField("082", '0', '4').AddSubfield('a', command.Ddc.Trim());
        }

        record.AddField("245", '0', '0').AddSubfield('a', command.Title.Trim());
        record.AddField("310").AddSubfield('a', FrequencyLabels.Of(command.Frequency));

        if (!string.IsNullOrWhiteSpace(command.Note))
        {
            record.AddField("500").AddSubfield('a', command.Note.Trim());
        }

        return record;
    }
}

public record DeleteSerialCommand(Guid Id) : IRequest;

public class DeleteSerialCommandHandler : IRequestHandler<DeleteSerialCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteSerialCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteSerialCommand command, CancellationToken ct)
    {
        var serial = await _db.Serials.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("ấn phẩm định kỳ", command.Id);

        var received = await _db.SerialIssues.CountAsync(
            issue => issue.SerialId == serial.Id
                     && (issue.Status == SerialIssueStatus.Received || issue.Status == SerialIssueStatus.Bound),
            ct);

        if (received > 0)
        {
            throw new ConflictException(
                $"Đầu báo \"{serial.Title}\" đã nhận {received} số nên không xóa được. " +
                "Hãy đánh dấu ngừng đặt thay vì xóa.");
        }

        var predicted = await _db.SerialIssues
            .Where(issue => issue.SerialId == serial.Id)
            .ToListAsync(ct);

        // Số mới ở trạng thái dự kiến thì chưa có gì tham chiếu tới, xóa cùng đầu báo là đúng.
        _db.SerialIssues.RemoveRange(predicted);
        _db.Serials.Remove(serial);

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Nhãn tiếng Việt của kỳ hạn xuất bản, dùng cả trên màn hình và trong trường MARC 310.</summary>
public static class FrequencyLabels
{
    public static readonly IReadOnlyDictionary<SerialFrequency, string> All =
        new Dictionary<SerialFrequency, string>
        {
            [SerialFrequency.Daily] = "Nhật báo",
            [SerialFrequency.Weekly] = "Tuần",
            [SerialFrequency.Biweekly] = "Hai tuần một kỳ",
            [SerialFrequency.SemiMonthly] = "Nửa tháng",
            [SerialFrequency.Monthly] = "Tháng",
            [SerialFrequency.Bimonthly] = "Hai tháng một kỳ",
            [SerialFrequency.Quarterly] = "Quý",
            [SerialFrequency.SemiAnnual] = "Nửa năm",
            [SerialFrequency.Annual] = "Năm",
            [SerialFrequency.Irregular] = "Không định kỳ"
        };

    public static string Of(SerialFrequency frequency) =>
        All.TryGetValue(frequency, out var label) ? label : frequency.ToString();
}
