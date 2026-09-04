using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Cms;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Opac;

// ---------------------------------------------------------------------------------------------
// IX.1 — Trang thông tin điện tử: trang chủ, tin tức, trang tĩnh, thư viện ảnh.
//
// Cả nhóm này không cần đăng nhập. Chỗ nào cũng chỉ trả nội dung đã xuất bản và đã tới giờ đăng —
// một bản tin hẹn giờ mà lọt ra sớm thì không lấy lại được.
// ---------------------------------------------------------------------------------------------

public record GetOpacHomeQuery : IRequest<OpacHomeDto>;

public class GetOpacHomeQueryHandler : IRequestHandler<GetOpacHomeQuery, OpacHomeDto>
{
    private const int ShelfSize = 8;
    private const int NewsSize = 6;

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetOpacHomeQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<OpacHomeDto> Handle(GetOpacHomeQuery query, CancellationToken ct)
    {
        var now = _clock.Now;
        var today = DateOnly.FromDateTime(now.Date);
        var records = OpacQueryBuilder.Published(_db.BibRecords.AsNoTracking());

        // Chỉ nêu tài liệu bạn đọc mượn hoặc đọc được. Biểu ghi mới nhất về mặt thời gian thường
        // là đầu báo, đầu tạp chí hay biểu ghi vừa thu hoạch về — chưa có bản in nào trong kho,
        // cũng chưa có bản số. Đưa chúng lên khối đầu trang chủ thì bạn đọc mở trang ra là thấy
        // ngay một dãy tài liệu không mượn được và hiểu là thư viện trống.
        var newBooks = await records
            .Where(bib => bib.ItemCount > 0 || bib.DigitalDocumentCount > 0)
            .OrderByDescending(bib => bib.CreatedAt)
            .Take(ShelfSize)
            .Select(OpacQueryBuilder.ToResult())
            .ToListAsync(ct);

        var popular = await records
            .Where(bib => bib.LoanCount > 0)
            .OrderByDescending(bib => bib.LoanCount)
            .Take(ShelfSize)
            .Select(OpacQueryBuilder.ToResult())
            .ToListAsync(ct);

        // Thông báo có khối riêng ở dưới, nên bỏ khỏi khối tin tức: hai khối cạnh nhau cùng hiện
        // một bản tin thì bạn đọc tưởng thư viện đăng trùng.
        var news = await PublishedNews(_db, now)
            .Where(item => item.Category == null || item.Category.Code != AnnouncementCategoryCode)
            .OrderByDescending(item => item.IsFeatured)
            .ThenByDescending(item => item.PublishedAt)
            .Take(NewsSize)
            .Select(item => new OpacHomeNewsDto(
                item.Id,
                item.Title,
                item.Slug,
                item.Summary,
                item.ThumbnailUrl,
                item.Category!.Name,
                item.IsFeatured,
                item.PublishedAt))
            .ToListAsync(ct);

        var banners = await _db.CmsBanners.AsNoTracking()
            .Where(banner => banner.IsActive
                             && banner.Position == "HOME_SLIDER"
                             && (banner.StartDate == null || banner.StartDate <= today)
                             && (banner.EndDate == null || banner.EndDate >= today))
            .OrderBy(banner => banner.SortOrder)
            .Select(banner => new OpacHomeBannerDto(
                banner.Id, banner.Title, banner.ImageUrl, banner.Link))
            .ToListAsync(ct);

        var links = await _db.CmsExternalLinks.AsNoTracking()
            .Where(link => link.IsActive)
            .OrderBy(link => link.SortOrder)
            .ThenBy(link => link.Name)
            .Take(12)
            .Select(link => new OpacHomeLinkDto(
                link.Id, link.Name, link.Url, link.LogoUrl, link.GroupName))
            .ToListAsync(ct);

        var statistics = new OpacStatisticsDto(
            await records.CountAsync(ct),
            await _db.Items.AsNoTracking().CountAsync(ct),
            // Đếm đúng phần khách vãng lai mở được, cùng luật với danh sách ở trang Tài liệu số:
            // hứa nhiều hơn thứ bạn đọc thấy khi bấm vào là một lời hứa hỏng.
            await _db.DigitalDocuments.AsNoTracking()
                .CountAsync(document => document.AccessLevel != DigitalAccessLevel.Forbidden
                                        && document.AccessLevel != DigitalAccessLevel.Internal, ct),
            await _db.Readers.AsNoTracking().CountAsync(ct));

        // Thông báo: bản tin thuộc chuyên mục nạp sẵn "Thông báo" (mã THONG_BAO). Khối này không
        // xét cờ nổi bật — thông báo mới nhất luôn đứng đầu, vì đó là thứ bạn đọc cần biết hôm nay.
        var announcements = await PublishedNews(_db, now)
            .Where(item => item.Category != null && item.Category.Code == AnnouncementCategoryCode)
            .OrderByDescending(item => item.PublishedAt)
            .Take(AnnouncementSize)
            .Select(item => new OpacHomeNewsDto(
                item.Id,
                item.Title,
                item.Slug,
                item.Summary,
                item.ThumbnailUrl,
                item.Category!.Name,
                item.IsFeatured,
                item.PublishedAt))
            .ToListAsync(ct);

        return new OpacHomeDto(newBooks, popular, news, announcements, banners, links, statistics);
    }

    /// <summary>Mã chuyên mục tin nạp sẵn dành cho thông báo của thư viện.</summary>
    public const string AnnouncementCategoryCode = "THONG_BAO";

    private const int AnnouncementSize = 5;

    /// <summary>
    /// Bản tin đã đăng và đã tới giờ.
    ///
    /// Bài hẹn giờ vẫn mang cờ "đã xuất bản" từ lúc cán bộ bấm lưu, nên chỉ lọc theo cờ là chưa đủ:
    /// phải so cả mốc thời gian, nếu không thì bài hẹn cho tuần sau hiện ngay hôm nay.
    /// </summary>
    internal static IQueryable<Domain.Entities.Web.CmsNews> PublishedNews(
        IApplicationDbContext db, DateTimeOffset now) =>
        db.CmsNews.AsNoTracking()
            .Where(item => item.IsPublished
                           && item.PublishedAt != null
                           && item.PublishedAt <= now);
}

/// <summary>Danh sách tin tức trên trang tra cứu.</summary>
public record GetPublicNewsQuery(PublicNewsRequest Request) : IRequest<PagedResult<OpacHomeNewsDto>>;

public class PublicNewsRequest : PagedRequest
{
    public Guid? CategoryId { get; set; }
    public string? Tag { get; set; }
}

public class GetPublicNewsQueryHandler
    : IRequestHandler<GetPublicNewsQuery, PagedResult<OpacHomeNewsDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetPublicNewsQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<OpacHomeNewsDto>> Handle(
        GetPublicNewsQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var news = GetOpacHomeQueryHandler.PublishedNews(_db, _clock.Now);

        if (request.HasKeyword())
        {
            var keyword = OpacQueryBuilder.Normalise(request.Keyword!);

            news = news.Where(item =>
                DatabaseFunctions.Unaccent(item.Title).Contains(keyword)
                || DatabaseFunctions.Unaccent(item.Summary ?? string.Empty).Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var tag = request.Tag.Trim().ToLowerInvariant();
            news = news.Where(item => (item.Tags ?? string.Empty).ToLower().Contains(tag));
        }

        return await news
            .WhereIf(request.CategoryId is not null, item => item.CategoryId == request.CategoryId)
            .WhereIf(request.UpdatedSince is not null, item => (item.UpdatedAt ?? item.CreatedAt) >= request.UpdatedSince)
            .OrderByDescending(item => item.PublishedAt)
            .Select(item => new OpacHomeNewsDto(
                item.Id,
                item.Title,
                item.Slug,
                item.Summary,
                item.ThumbnailUrl,
                item.Category!.Name,
                item.IsFeatured,
                item.PublishedAt))
            .ToPagedResultAsync(request, ct);
    }
}

/// <summary>Một bản tin đọc theo đường dẫn thân thiện; mỗi lần mở cộng một lượt xem.</summary>
public record GetPublicNewsDetailQuery(string Slug) : IRequest<PublicNewsDetailDto>;

public record PublicNewsDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    string? Content,
    string? ThumbnailUrl,
    string? CategoryName,
    Guid? CategoryId,
    string? Tags,
    string? Author,
    DateTimeOffset? PublishedAt,
    int ViewCount,
    IReadOnlyList<OpacHomeNewsDto> Related);

public class GetPublicNewsDetailQueryHandler
    : IRequestHandler<GetPublicNewsDetailQuery, PublicNewsDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetPublicNewsDetailQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PublicNewsDetailDto> Handle(
        GetPublicNewsDetailQuery query, CancellationToken ct)
    {
        var slug = (query.Slug ?? string.Empty).Trim();

        var item = await GetOpacHomeQueryHandler.PublishedNews(_db, _clock.Now)
                       .Include(news => news.Category)
                       .FirstOrDefaultAsync(news => news.Slug == slug, ct)
                   ?? throw new NotFoundException("Không tìm thấy bản tin.");

        var related = await GetOpacHomeQueryHandler.PublishedNews(_db, _clock.Now)
            .Where(news => news.Id != item.Id
                           && (item.CategoryId == null || news.CategoryId == item.CategoryId))
            .OrderByDescending(news => news.PublishedAt)
            .Take(4)
            .Select(news => new OpacHomeNewsDto(
                news.Id,
                news.Title,
                news.Slug,
                news.Summary,
                news.ThumbnailUrl,
                news.Category!.Name,
                news.IsFeatured,
                news.PublishedAt))
            .ToListAsync(ct);

        await _db.CmsNews
            .Where(news => news.Id == item.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(news => news.ViewCount, news => news.ViewCount + 1), ct);

        return new PublicNewsDetailDto(
            item.Id,
            item.Title,
            item.Slug,
            item.Summary,
            item.Content,
            item.ThumbnailUrl,
            item.Category?.Name,
            item.CategoryId,
            item.Tags,
            item.Author,
            item.PublishedAt,
            item.ViewCount + 1,
            related);
    }
}

/// <summary>Chuyên mục tin kèm số bài đã đăng, dùng cho thanh lọc trên trang tin.</summary>
public record GetPublicNewsCategoriesQuery : IRequest<IReadOnlyList<PublicNewsCategoryDto>>;

public record PublicNewsCategoryDto(Guid Id, string Code, string Name, int NewsCount);

public class GetPublicNewsCategoriesQueryHandler
    : IRequestHandler<GetPublicNewsCategoriesQuery, IReadOnlyList<PublicNewsCategoryDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetPublicNewsCategoriesQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PublicNewsCategoryDto>> Handle(
        GetPublicNewsCategoriesQuery query, CancellationToken ct)
    {
        var now = _clock.Now;

        var counts = await GetOpacHomeQueryHandler.PublishedNews(_db, now)
            .Where(item => item.CategoryId != null)
            .GroupBy(item => item.CategoryId!.Value)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Key, row => row.Count, ct);

        var categories = await _db.CmsNewsCategories.AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new { category.Id, category.Code, category.Name })
            .ToListAsync(ct);

        return categories
            .Select(category => new PublicNewsCategoryDto(
                category.Id, category.Code, category.Name, counts.GetValueOrDefault(category.Id)))
            .Where(category => category.NewsCount > 0)
            .ToList();
    }
}

/// <summary>Trang tĩnh đọc theo đường dẫn thân thiện.</summary>
public record GetPublicPageQuery(string Slug) : IRequest<CmsPageDto>;

public class GetPublicPageQueryHandler : IRequestHandler<GetPublicPageQuery, CmsPageDto>
{
    private readonly IApplicationDbContext _db;

    public GetPublicPageQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CmsPageDto> Handle(GetPublicPageQuery query, CancellationToken ct)
    {
        var slug = (query.Slug ?? string.Empty).Trim();

        var page = await _db.CmsPages.AsNoTracking()
                       .FirstOrDefaultAsync(entity => entity.Slug == slug && entity.IsPublished, ct)
                   ?? throw new NotFoundException("Không tìm thấy trang.");

        await _db.CmsPages
            .Where(entity => entity.Id == page.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(entity => entity.ViewCount, entity => entity.ViewCount + 1),
                ct);

        return new CmsPageDto(
            page.Id,
            page.Slug,
            page.Title,
            page.Content,
            page.MetaDescription,
            page.IsPublished,
            page.PublishedAt,
            page.ViewCount + 1,
            page.SortOrder,
            page.ParentId);
    }
}

/// <summary>Danh sách trang tĩnh đã đăng, dùng dựng menu chân trang và sơ đồ trang.</summary>
public record GetPublicPagesQuery : IRequest<IReadOnlyList<CmsPageRowDto>>;

public class GetPublicPagesQueryHandler
    : IRequestHandler<GetPublicPagesQuery, IReadOnlyList<CmsPageRowDto>>
{
    private readonly IApplicationDbContext _db;

    public GetPublicPagesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CmsPageRowDto>> Handle(
        GetPublicPagesQuery query, CancellationToken ct) =>
        await _db.CmsPages.AsNoTracking()
            .Where(page => page.IsPublished)
            .OrderBy(page => page.SortOrder)
            .ThenBy(page => page.Title)
            .Select(page => new CmsPageRowDto(
                page.Id,
                page.Slug,
                page.Title,
                page.MetaDescription,
                page.IsPublished,
                page.PublishedAt,
                page.ViewCount,
                page.SortOrder,
                page.ParentId,
                page.UpdatedAt))
            .ToListAsync(ct);
}
