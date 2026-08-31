using System.Diagnostics;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Web;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Opac;

// ---------------------------------------------------------------------------------------------
// IX.2 — Tra cứu tài liệu: cơ bản, nâng cao, gợi ý khi gõ, bộ đếm facet, tra theo ISBN và theo mã
// vạch. Đây cũng chính là nhóm endpoint /api/search/* mà ứng dụng di động đợt sau sẽ gọi (XI.4).
// ---------------------------------------------------------------------------------------------

/// <summary>Tìm kiếm cơ bản: một ô từ khóa kèm phạm vi và bộ lọc.</summary>
public record OpacSearchQuery(OpacSearchRequest Request) : IRequest<PagedResult<OpacResultDto>>;

public class OpacSearchQueryHandler : IRequestHandler<OpacSearchQuery, PagedResult<OpacResultDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IOpacSearchLogger _logger;

    public OpacSearchQueryHandler(IApplicationDbContext db, IOpacSearchLogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResult<OpacResultDto>> Handle(
        OpacSearchQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var stopwatch = Stopwatch.StartNew();

        var records = OpacQueryBuilder.Published(_db.BibRecords.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            records = records.Where(OpacQueryBuilder.Clause(request.Scope, request.Keyword));
        }

        records = OpacQueryBuilder.ApplyFilter(records, request.Filter);
        records = OpacQueryBuilder.ApplySort(records, request.Sort, request.Keyword);

        var result = await records
            .Select(OpacQueryBuilder.ToResult())
            .ToPagedResultAsync(request, OpacQueryBuilder.CountLimit, ct);

        stopwatch.Stop();

        await _logger.LogAsync(
            request.Keyword,
            OpacScopeLabels.Describe(request.Scope),
            result.TotalCount,
            (int)stopwatch.ElapsedMilliseconds,
            ct);

        return result;
    }
}

/// <summary>Tìm kiếm nâng cao: nhiều mệnh đề nối bằng VÀ / HOẶC / KHÔNG.</summary>
public record OpacAdvancedSearchQuery(OpacAdvancedSearchRequest Request)
    : IRequest<PagedResult<OpacResultDto>>;

public class OpacAdvancedSearchQueryValidator : AbstractValidator<OpacAdvancedSearchQuery>
{
    public OpacAdvancedSearchQueryValidator()
    {
        RuleFor(query => query.Request.Clauses)
            .Must(clauses => clauses.Count <= 10)
            .WithMessage("Tối đa 10 điều kiện trong một lần tra cứu.");

        RuleForEach(query => query.Request.Clauses)
            .Must(clause => (clause.Term ?? string.Empty).Length <= 300)
            .WithMessage("Mỗi điều kiện tối đa 300 ký tự.");
    }
}

public class OpacAdvancedSearchQueryHandler
    : IRequestHandler<OpacAdvancedSearchQuery, PagedResult<OpacResultDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IOpacSearchLogger _logger;

    public OpacAdvancedSearchQueryHandler(IApplicationDbContext db, IOpacSearchLogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResult<OpacResultDto>> Handle(
        OpacAdvancedSearchQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var stopwatch = Stopwatch.StartNew();

        var records = OpacQueryBuilder.Published(_db.BibRecords.AsNoTracking())
            .Where(OpacQueryBuilder.Combine(request.Clauses));

        records = OpacQueryBuilder.ApplyFilter(records, request.Filter);

        // Mệnh đề đầu tiên quyết định thứ tự "liên quan nhất": nó là thứ người tra cứu gõ trước.
        var leadTerm = request.Clauses
            .FirstOrDefault(clause => !string.IsNullOrWhiteSpace(clause.Term))?.Term;

        records = OpacQueryBuilder.ApplySort(records, request.Sort, leadTerm);

        var result = await records
            .Select(OpacQueryBuilder.ToResult())
            .ToPagedResultAsync(request, OpacQueryBuilder.CountLimit, ct);

        stopwatch.Stop();

        await _logger.LogAsync(
            DescribeClauses(request.Clauses),
            "Nâng cao",
            result.TotalCount,
            (int)stopwatch.ElapsedMilliseconds,
            ct);

        return result;
    }

    private static string DescribeClauses(IReadOnlyList<OpacSearchClause> clauses) =>
        string.Join(" ", clauses
            .Where(clause => !string.IsNullOrWhiteSpace(clause.Term))
            .Select((clause, index) =>
            {
                var connector = index == 0
                    ? string.Empty
                    : clause.Connector switch
                    {
                        OpacConnector.Or => "HOẶC ",
                        OpacConnector.Not => "KHÔNG ",
                        _ => "VÀ "
                    };

                return $"{connector}{OpacScopeLabels.Describe(clause.Field)}={clause.Term}";
            }));
}

/// <summary>Gợi ý tự động khi gõ vào ô tìm kiếm.</summary>
public record OpacSuggestQuery(string Term, int Limit = 10)
    : IRequest<IReadOnlyList<OpacSuggestionDto>>;

public class OpacSuggestQueryHandler
    : IRequestHandler<OpacSuggestQuery, IReadOnlyList<OpacSuggestionDto>>
{
    private readonly IApplicationDbContext _db;

    public OpacSuggestQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<OpacSuggestionDto>> Handle(
        OpacSuggestQuery query, CancellationToken ct)
    {
        var term = (query.Term ?? string.Empty).Trim();

        // Gõ một chữ thì gợi ý gần như toàn kho, vô nghĩa mà lại nặng. Đợi tới chữ thứ hai.
        if (term.Length < 2)
        {
            return Array.Empty<OpacSuggestionDto>();
        }

        var keyword = OpacQueryBuilder.Normalise(term);
        var limit = Math.Clamp(query.Limit, 1, 20);
        var records = OpacQueryBuilder.Published(_db.BibRecords.AsNoTracking());

        var titles = await records
            .Where(bib => DatabaseFunctions.Unaccent(bib.Title).Contains(keyword))
            .OrderByDescending(bib => bib.LoanCount)
            .Select(bib => bib.Title)
            .Distinct()
            .Take(limit)
            .ToListAsync(ct);

        var authors = await _db.Authors.AsNoTracking()
            .Where(author => DatabaseFunctions.Unaccent(author.Name).Contains(keyword))
            .OrderBy(author => author.Name)
            .Select(author => author.Name)
            .Take(limit / 2 + 1)
            .ToListAsync(ct);

        var subjects = await _db.Subjects.AsNoTracking()
            .Where(subject => DatabaseFunctions.Unaccent(subject.Name).Contains(keyword))
            .OrderBy(subject => subject.Name)
            .Select(subject => subject.Name)
            .Take(limit / 2 + 1)
            .ToListAsync(ct);

        return titles.Select(title => new OpacSuggestionDto(title, "Nhan đề", 0))
            .Concat(authors.Select(author => new OpacSuggestionDto(author, "Tác giả", 0)))
            .Concat(subjects.Select(subject => new OpacSuggestionDto(subject, "Chủ đề", 0)))
            .Take(limit)
            .ToList();
    }
}

/// <summary>
/// Bộ đếm cho các bộ lọc bên trái kết quả.
///
/// Đếm trên đúng tập kết quả hiện tại chứ không phải trên toàn kho: con số dưới mỗi dòng lọc phải
/// là "còn bao nhiêu tài liệu nếu tôi bấm thêm cái này", nếu không thì bấm vào rồi ra danh sách
/// rỗng, và bạn đọc mất lòng tin vào cả trang tra cứu.
/// </summary>
public record OpacFacetsQuery(OpacSearchRequest Request) : IRequest<IReadOnlyList<OpacFacetGroupDto>>;

public class OpacFacetsQueryHandler
    : IRequestHandler<OpacFacetsQuery, IReadOnlyList<OpacFacetGroupDto>>
{
    /// <summary>Số giá trị hiện tối đa cho mỗi nhóm lọc; nhiều hơn thì cột lọc dài hơn cả kết quả.</summary>
    private const int MaxValuesPerGroup = 12;

    private readonly IApplicationDbContext _db;

    public OpacFacetsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<OpacFacetGroupDto>> Handle(
        OpacFacetsQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var records = OpacQueryBuilder.Published(_db.BibRecords.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            records = records.Where(OpacQueryBuilder.Clause(request.Scope, request.Keyword));
        }

        records = OpacQueryBuilder.ApplyFilter(records, request.Filter);

        // Bộ đếm tính trên tối đa CountLimit biểu ghi đầu tiên khớp điều kiện, cùng ngưỡng với con
        // số tổng ở danh sách kết quả. Trên kho lớn, một câu hỏi rộng khớp hàng trăm nghìn biểu ghi
        // và việc gom nhóm hết chỗ ấy mất vài giây — trong khi bảy con số hiện ở cột lọc chỉ để
        // người tra cứu chọn hướng thu hẹp, chứ không ai đối chiếu sổ sách bằng chúng.
        records = records.Take(OpacQueryBuilder.CountLimit);

        // Đếm theo mã rồi mới lấy tên ở một truy vấn khác.
        //
        // Gom nhóm thẳng theo tên của bảng liên kết thì câu lệnh không dịch được sang SQL khi phía
        // trên đã có điều kiện lồng — mà điều kiện lồng chính là thứ mọi lượt tra cứu đều có. Tách
        // làm hai bước thì cả hai đều là câu lệnh đơn giản, và tên hiển thị lấy từ danh mục nên
        // luôn đúng với chỗ khác trong phần mềm.
        var documentTypeCounts = await CountAsync(
            records.Where(bib => bib.DocumentTypeId != null)
                .GroupBy(bib => bib.DocumentTypeId!.Value), ct);

        var languageCounts = await CountAsync(
            records.Where(bib => bib.LanguageId != null)
                .GroupBy(bib => bib.LanguageId!.Value), ct);

        var authorCounts = await CountAsync(
            records.SelectMany(bib => bib.Authors).GroupBy(link => link.AuthorId), ct);

        var subjectCounts = await CountAsync(
            records.SelectMany(bib => bib.Subjects).GroupBy(link => link.SubjectId), ct);

        var warehouseCounts = await records
            .SelectMany(bib => bib.Items)
            .GroupBy(item => item.WarehouseId)
            .Select(group => new
            {
                Id = group.Key,
                // Một kho có nhiều bản của cùng một cuốn; bạn đọc quan tâm số đầu sách, không phải
                // số bản in.
                Count = group.Select(item => item.BibId).Distinct().Count()
            })
            .OrderByDescending(row => row.Count)
            .Take(MaxValuesPerGroup)
            .ToDictionaryAsync(row => row.Id, row => row.Count, ct);

        var documentTypes = await LabelAsync(
            _db.DocumentTypes.AsNoTracking(), documentTypeCounts, ct);

        var languages = await LabelAsync(_db.Languages.AsNoTracking(), languageCounts, ct);
        var authors = await LabelAsync(_db.Authors.AsNoTracking(), authorCounts, ct);
        var subjects = await LabelAsync(_db.Subjects.AsNoTracking(), subjectCounts, ct);
        var warehouses = await LabelAsync(_db.Warehouses.AsNoTracking(), warehouseCounts, ct);

        var years = (await records
                .Where(bib => bib.PublishYear != null)
                .GroupBy(bib => bib.PublishYear!.Value)
                .Select(group => new { Year = group.Key, Count = group.Count() })
                .OrderByDescending(row => row.Year)
                .Take(MaxValuesPerGroup)
                .ToListAsync(ct))
            .Select(row => new OpacFacetValueDto(
                row.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.Count))
            .ToList();

        var digital = await records.CountAsync(bib => bib.DigitalDocumentCount > 0, ct);
        var available = await records.CountAsync(bib => bib.AvailableItemCount > 0, ct);

        return new List<OpacFacetGroupDto>
        {
            new("documentType", "Dạng tài liệu", documentTypes),
            new("author", "Tác giả", authors),
            new("subject", "Chủ đề", subjects),
            new("language", "Ngôn ngữ", languages),
            new("publishYear", "Năm xuất bản", years),
            new("warehouse", "Kho", warehouses),
            new("availability", "Tình trạng", new List<OpacFacetValueDto>
            {
                new("available", "Còn bản rảnh", available),
                new("digital", "Có tài liệu số", digital)
            })
        };
    }

    /// <summary>Đếm số tài liệu cho mỗi mã, lấy các mã nhiều tài liệu nhất.</summary>
    private static Task<Dictionary<Guid, int>> CountAsync<TSource>(
        IQueryable<IGrouping<Guid, TSource>> groups, CancellationToken ct) =>
        groups
            .Select(group => new { Id = group.Key, Count = group.Count() })
            .OrderByDescending(row => row.Count)
            .Take(MaxValuesPerGroup)
            .ToDictionaryAsync(row => row.Id, row => row.Count, ct);

    /// <summary>Ghép số đếm với tên danh mục, giữ nguyên thứ tự từ nhiều tới ít.</summary>
    private static async Task<IReadOnlyList<OpacFacetValueDto>> LabelAsync<TEntity>(
        IQueryable<TEntity> catalog,
        IReadOnlyDictionary<Guid, int> counts,
        CancellationToken ct)
        where TEntity : Domain.Common.CatalogEntity
    {
        if (counts.Count == 0)
        {
            return Array.Empty<OpacFacetValueDto>();
        }

        var ids = counts.Keys.ToList();

        var names = await catalog
            .Where(entity => ids.Contains(entity.Id))
            .Select(entity => new { entity.Id, entity.Name })
            .ToDictionaryAsync(row => row.Id, row => row.Name, ct);

        return counts
            .OrderByDescending(pair => pair.Value)
            .Where(pair => names.ContainsKey(pair.Key))
            .Select(pair => new OpacFacetValueDto(
                pair.Key.ToString(), names[pair.Key], pair.Value))
            .ToList();
    }
}


/// <summary>Tra theo ISBN — dùng cho chức năng quét mã trên ứng dụng di động.</summary>
public record OpacSearchByIsbnQuery(string Isbn) : IRequest<IReadOnlyList<OpacResultDto>>;

public class OpacSearchByIsbnQueryHandler
    : IRequestHandler<OpacSearchByIsbnQuery, IReadOnlyList<OpacResultDto>>
{
    private readonly IApplicationDbContext _db;

    public OpacSearchByIsbnQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<OpacResultDto>> Handle(
        OpacSearchByIsbnQuery query, CancellationToken ct)
    {
        var isbn = new string((query.Isbn ?? string.Empty).Where(char.IsLetterOrDigit).ToArray())
            .ToLowerInvariant();

        if (string.IsNullOrEmpty(isbn))
        {
            throw new Common.Exceptions.ValidationException("isbn", "Chưa nhập mã ISBN.");
        }

        return await OpacQueryBuilder.Published(_db.BibRecords.AsNoTracking())
            .Where(bib =>
                (bib.Isbn ?? string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty)
                    .ToLower().Contains(isbn)
                || (bib.Issn ?? string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty)
                    .ToLower().Contains(isbn))
            .OrderBy(bib => bib.Title)
            .Take(20)
            .Select(OpacQueryBuilder.ToResult())
            .ToListAsync(ct);
    }
}

/// <summary>
/// Tra một bản in theo mã vạch — quét mã trên gáy sách là ra ngay biểu ghi và vị trí của nó.
/// </summary>
public record OpacSearchByBarcodeQuery(string Barcode) : IRequest<OpacBarcodeResultDto>;

public record OpacBarcodeResultDto(
    string Barcode,
    string RegisterNumber,
    string? CallNumber,
    string LibraryName,
    string WarehouseName,
    string? ShelfName,
    string StatusLabel,
    bool IsAvailable,
    OpacResultDto Bib);

public class OpacSearchByBarcodeQueryHandler
    : IRequestHandler<OpacSearchByBarcodeQuery, OpacBarcodeResultDto>
{
    private readonly IApplicationDbContext _db;

    public OpacSearchByBarcodeQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<OpacBarcodeResultDto> Handle(
        OpacSearchByBarcodeQuery query, CancellationToken ct)
    {
        var barcode = (query.Barcode ?? string.Empty).Trim();

        var item = await _db.Items.AsNoTracking()
                       .Include(entity => entity.Warehouse)!.ThenInclude(warehouse => warehouse!.Library)
                       .Include(entity => entity.Shelf)
                       .Include(entity => entity.Bib)!.ThenInclude(bib => bib!.DocumentType)
                       .Include(entity => entity.Bib)!.ThenInclude(bib => bib!.Language)
                       .FirstOrDefaultAsync(
                           entity => entity.Barcode == barcode || entity.RegisterNumber == barcode, ct)
                   ?? throw new NotFoundException($"Không tìm thấy ấn phẩm mang mã \"{barcode}\".");

        if (item.Bib is null || item.Bib.Status != Domain.Enums.RecordStatus.Published)
        {
            throw new NotFoundException($"Không tìm thấy ấn phẩm mang mã \"{barcode}\".");
        }

        var bib = item.Bib;

        return new OpacBarcodeResultDto(
            item.Barcode,
            item.RegisterNumber,
            item.CallNumber,
            item.Warehouse?.Library?.Name ?? string.Empty,
            item.Warehouse?.Name ?? string.Empty,
            item.Shelf?.Name,
            OpacLabels.Describe(item.Status),
            item.IsAvailable,
            new OpacResultDto(
                bib.Id,
                bib.ControlNumber,
                bib.Title,
                bib.Subtitle,
                bib.AuthorMain,
                bib.PublisherName,
                bib.PublishYear,
                bib.Isbn,
                bib.Ddc,
                bib.DocumentType?.Name,
                bib.Language?.Name,
                bib.CoverImageUrl,
                bib.Abstract,
                bib.ItemCount,
                bib.AvailableItemCount,
                bib.DigitalDocumentCount,
                bib.LoanCount));
    }
}

/// <summary>
/// Ghi nhật ký tra cứu.
///
/// Ghi cả lượt tra không ra kết quả — đó chính là dữ liệu cán bộ cần: bạn đọc đang tìm gì mà thư
/// viện không có. Ghi ngoài đường đi của kết quả nên nếu ghi hỏng thì lượt tra vẫn thành công.
/// </summary>
public interface IOpacSearchLogger
{
    Task LogAsync(string? keyword, string searchType, int resultCount, int durationMs,
        CancellationToken ct = default);
}

public class OpacSearchLogger : IOpacSearchLogger
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public OpacSearchLogger(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task LogAsync(
        string? keyword, string searchType, int resultCount, int durationMs, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return;
        }

        _db.OpacSearchLogs.Add(new OpacSearchLog
        {
            Keyword = keyword.Trim().Length > 300 ? keyword.Trim()[..300] : keyword.Trim(),
            SearchType = searchType,
            ResultCount = resultCount,
            ReaderId = _currentUser.ReaderId,
            Ip = _currentUser.Ip,
            DurationMs = durationMs,
            OccurredAt = _clock.Now
        });

        await _db.SaveChangesAsync(ct);
    }
}
