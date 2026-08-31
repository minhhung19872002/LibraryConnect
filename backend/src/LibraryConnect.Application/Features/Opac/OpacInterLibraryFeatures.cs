using FluentValidation;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.InterLibrary;
using LibraryConnect.Domain.Entities.Ill;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Opac;

// ---------------------------------------------------------------------------------------------
// IX.5 — Tìm ở thư viện khác, ngay trên trang tra cứu công khai.
//
// Khác với màn hình II.7 của cán bộ ở ba điểm, và cả ba đều là ranh giới an toàn:
//   - chỉ tra ở những máy chủ cán bộ đã bật cờ "hiện trên trang tra cứu";
//   - không trả biểu ghi MARC thô, vì bạn đọc không nhập biểu ghi vào kho;
//   - số biểu ghi lấy về bị chặn trên, để một lượt tra của khách vãng lai không kéo hàng trăm
//     biểu ghi từ máy chủ nước ngoài về.
// ---------------------------------------------------------------------------------------------

public class OpacRemoteSearchQuery : IRequest<OpacRemoteSearchResultDto>
{
    public RemoteSearchField Field { get; set; } = RemoteSearchField.Any;
    public string Term { get; set; } = string.Empty;
    public List<Guid> TargetIds { get; set; } = new();
}

public record OpacRemoteSearchResultDto(
    IReadOnlyList<OpacRemoteTargetDto> Targets,
    int TotalHits,
    int FetchedCount);

public record OpacRemoteTargetDto(
    Guid TargetId,
    string TargetName,
    bool Success,
    string? Message,
    int TotalHits,
    int DurationMs,
    IReadOnlyList<OpacRemoteRecordDto> Records);

public record OpacRemoteRecordDto(
    string SourceName,
    string? Title,
    string? Author,
    string? Publisher,
    string? PublishYear,
    string? Isbn,
    string? Edition,
    string? Pages,
    Guid? ExistingBibId,
    string? ExistingBibTitle);

public class OpacRemoteSearchQueryValidator : AbstractValidator<OpacRemoteSearchQuery>
{
    public OpacRemoteSearchQueryValidator()
    {
        RuleFor(query => query.Term)
            .NotEmpty().WithMessage("Chưa nhập từ khóa tra cứu.")
            .MaximumLength(200).WithMessage("Từ khóa tối đa 200 ký tự.");
    }
}

public class OpacRemoteSearchQueryHandler
    : IRequestHandler<OpacRemoteSearchQuery, OpacRemoteSearchResultDto>
{
    /// <summary>Số biểu ghi lấy về mỗi máy chủ cho một lượt tra của bạn đọc.</summary>
    private const int MaxRecords = 10;

    private readonly IApplicationDbContext _db;
    private readonly IRemoteCatalogSearcher _searcher;

    public OpacRemoteSearchQueryHandler(IApplicationDbContext db, IRemoteCatalogSearcher searcher)
    {
        _db = db;
        _searcher = searcher;
    }

    public async Task<OpacRemoteSearchResultDto> Handle(
        OpacRemoteSearchQuery query, CancellationToken ct)
    {
        var targets = await _db.Z3950Targets.AsNoTracking()
            .Where(target => target.IsActive
                             && target.ShowOnOpac
                             && (query.TargetIds.Count == 0 || query.TargetIds.Contains(target.Id)))
            .OrderBy(target => target.SortOrder)
            .ToListAsync(ct);

        if (targets.Count == 0)
        {
            return new OpacRemoteSearchResultDto(Array.Empty<OpacRemoteTargetDto>(), 0, 0);
        }

        // Song song giữa các nơi khác nhau, tuần tự trong cùng một nơi — cùng lý do như màn hình
        // của cán bộ: mở hai phiên cùng lúc tới một máy chủ thì phiên sau về tay không.
        var groups = targets
            .GroupBy(HostOf)
            .Select(async group =>
            {
                var results = new List<RemoteSearchTargetResultDto>();

                foreach (var target in group)
                {
                    results.Add(await _searcher.SearchAsync(
                        target, query.Field, query.Term.Trim(), MaxRecords, ct));
                }

                return results;
            });

        var raw = (await Task.WhenAll(groups)).SelectMany(group => group).ToList();
        var mapped = new List<OpacRemoteTargetDto>();

        foreach (var result in raw)
        {
            mapped.Add(new OpacRemoteTargetDto(
                result.TargetId,
                result.TargetName,
                result.Success,
                result.Message,
                result.TotalHits,
                result.DurationMs,
                await MatchAsync(result, ct)));
        }

        return new OpacRemoteSearchResultDto(
            mapped,
            mapped.Sum(target => target.TotalHits),
            mapped.Sum(target => target.Records.Count));
    }

    /// <summary>
    /// Đánh dấu cuốn nào thư viện mình đã có.
    ///
    /// Đây mới là điều bạn đọc quan tâm nhất khi tra sang thư viện khác: cuốn này ở đây có sẵn hay
    /// phải đi mượn nơi khác. So theo ISBN vì đó là thứ chắc chắn trùng nhau giữa hai kho.
    /// </summary>
    private async Task<IReadOnlyList<OpacRemoteRecordDto>> MatchAsync(
        RemoteSearchTargetResultDto result, CancellationToken ct)
    {
        var isbns = result.Records
            .Select(record => Normalize(record.Isbn))
            .Where(isbn => isbn is not null)
            .Select(isbn => isbn!)
            .Distinct()
            .ToList();

        var owned = new Dictionary<string, (Guid Id, string Title)>();

        if (isbns.Count > 0)
        {
            var candidates = await OpacQueryBuilder.Published(_db.BibRecords.AsNoTracking())
                .Where(bib => bib.Isbn != null)
                .Select(bib => new { bib.Id, bib.Title, bib.Isbn })
                .ToListAsync(ct);

            foreach (var bib in candidates)
            {
                var normalised = Normalize(bib.Isbn);

                if (normalised is not null && isbns.Contains(normalised))
                {
                    owned.TryAdd(normalised, (bib.Id, bib.Title));
                }
            }
        }

        return result.Records.Select(record =>
        {
            var isbn = Normalize(record.Isbn);
            Guid? existingId = null;
            string? existingTitle = null;

            if (isbn is not null && owned.TryGetValue(isbn, out var match))
            {
                existingId = match.Id;
                existingTitle = match.Title;
            }

            return new OpacRemoteRecordDto(
                record.TargetName,
                record.Title,
                record.Author,
                record.Publisher,
                record.PublishYear,
                record.Isbn,
                record.Edition,
                record.Pages,
                existingId,
                existingTitle);
        }).ToList();
    }

    private static string HostOf(Z3950Target target) =>
        target.UseSru && Uri.TryCreate(target.SruBaseUrl, UriKind.Absolute, out var uri)
            ? uri.Host.ToLowerInvariant()
            : target.Host.ToLowerInvariant();

    private static string? Normalize(string? isbn) =>
        string.IsNullOrWhiteSpace(isbn)
            ? null
            : new string(isbn.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}

/// <summary>Danh sách thư viện bạn được phép hiện trên trang tra cứu.</summary>
public record GetOpacRemoteTargetsQuery : IRequest<IReadOnlyList<OpacRemoteTargetInfoDto>>;

public record OpacRemoteTargetInfoDto(Guid Id, string Name);

public class GetOpacRemoteTargetsQueryHandler
    : IRequestHandler<GetOpacRemoteTargetsQuery, IReadOnlyList<OpacRemoteTargetInfoDto>>
{
    private readonly IApplicationDbContext _db;

    public GetOpacRemoteTargetsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<OpacRemoteTargetInfoDto>> Handle(
        GetOpacRemoteTargetsQuery query, CancellationToken ct) =>
        await _db.Z3950Targets.AsNoTracking()
            .Where(target => target.IsActive && target.ShowOnOpac)
            .OrderBy(target => target.SortOrder)
            .ThenBy(target => target.Name)
            .Select(target => new OpacRemoteTargetInfoDto(target.Id, target.Name))
            .ToListAsync(ct);
}
