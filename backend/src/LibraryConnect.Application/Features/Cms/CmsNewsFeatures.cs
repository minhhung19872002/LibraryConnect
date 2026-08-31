using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Web;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cms;

// ---------------------------------------------------------------------------------------------
// VIII.2 — Tin tức và sự kiện. Chuyên mục tin dùng chung màn hình danh mục ("news-categories"),
// nên ở đây chỉ còn phần bài viết.
// ---------------------------------------------------------------------------------------------

public record GetCmsNewsListQuery(CmsNewsListRequest Request) : IRequest<PagedResult<CmsNewsRowDto>>;

public class GetCmsNewsListQueryHandler
    : IRequestHandler<GetCmsNewsListQuery, PagedResult<CmsNewsRowDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCmsNewsListQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<CmsNewsRowDto>> Handle(
        GetCmsNewsListQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var news = _db.CmsNews.AsNoTracking();

        if (request.HasKeyword())
        {
            var keyword = VietnameseText.RemoveDiacritics(request.Keyword!.Trim()).ToLowerInvariant();

            news = news.Where(item =>
                DatabaseFunctions.Unaccent(item.Title).Contains(keyword)
                || DatabaseFunctions.Unaccent(item.Summary ?? string.Empty).Contains(keyword)
                || item.Slug.ToLower().Contains(keyword)
                || DatabaseFunctions.Unaccent(item.Tags ?? string.Empty).Contains(keyword));
        }

        return await news
            .WhereIf(request.CategoryId is not null, item => item.CategoryId == request.CategoryId)
            .WhereIf(request.IsPublished is not null, item => item.IsPublished == request.IsPublished)
            .WhereIf(request.IsFeatured is not null, item => item.IsFeatured == request.IsFeatured)
            .OrderByDescending(item => item.PublishedAt ?? item.CreatedAt)
            .Select(item => new CmsNewsRowDto(
                item.Id,
                item.Title,
                item.Slug,
                item.Summary,
                item.ThumbnailUrl,
                item.CategoryId,
                item.Category!.Name,
                item.Tags,
                item.Author,
                item.IsFeatured,
                item.IsPublished,
                item.PublishedAt,
                item.ViewCount,
                item.UpdatedAt))
            .ToPagedResultAsync(request, ct);
    }
}

public record GetCmsNewsQuery(Guid Id) : IRequest<CmsNewsDto>;

public class GetCmsNewsQueryHandler : IRequestHandler<GetCmsNewsQuery, CmsNewsDto>
{
    private readonly IApplicationDbContext _db;

    public GetCmsNewsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CmsNewsDto> Handle(GetCmsNewsQuery query, CancellationToken ct)
    {
        var item = await _db.CmsNews.AsNoTracking()
                       .Include(news => news.Category)
                       .FirstOrDefaultAsync(news => news.Id == query.Id, ct)
                   ?? throw new NotFoundException("bản tin", query.Id);

        return new CmsNewsDto(
            item.Id,
            item.Title,
            item.Slug,
            item.Summary,
            item.Content,
            item.ThumbnailUrl,
            item.CategoryId,
            item.Category?.Name,
            item.Tags,
            item.Author,
            item.IsFeatured,
            item.IsPublished,
            item.PublishedAt,
            item.ViewCount);
    }
}

public class SaveCmsNewsCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? ThumbnailUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Tags { get; set; }
    public string? Author { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; }

    /// <summary>
    /// Hẹn giờ đăng. Bỏ trống mà bật xuất bản thì đăng ngay; đặt mốc tương lai thì bài chỉ hiện
    /// trên trang tra cứu khi tới giờ.
    /// </summary>
    public DateTimeOffset? PublishedAt { get; set; }
}

public class SaveCmsNewsCommandValidator : AbstractValidator<SaveCmsNewsCommand>
{
    public SaveCmsNewsCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Chưa nhập tiêu đề tin.")
            .MaximumLength(300).WithMessage("Tiêu đề tối đa 300 ký tự.");

        RuleFor(command => command.Summary)
            .MaximumLength(1000).WithMessage("Tóm tắt tối đa 1000 ký tự.");

        RuleFor(command => command.Tags)
            .MaximumLength(300).WithMessage("Danh sách thẻ tối đa 300 ký tự.");
    }
}

public class SaveCmsNewsCommandHandler : IRequestHandler<SaveCmsNewsCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IHtmlSanitizer _sanitizer;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public SaveCmsNewsCommandHandler(
        IApplicationDbContext db,
        IHtmlSanitizer sanitizer,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _sanitizer = sanitizer;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(SaveCmsNewsCommand command, CancellationToken ct)
    {
        var item = command.Id is null
            ? new CmsNews()
            : await _db.CmsNews.FirstOrDefaultAsync(news => news.Id == command.Id, ct)
              ?? throw new NotFoundException("bản tin", command.Id.Value);

        if (command.CategoryId is not null
            && !await _db.CmsNewsCategories.AnyAsync(
                category => category.Id == command.CategoryId, ct))
        {
            throw new NotFoundException("chuyên mục tin", command.CategoryId.Value);
        }

        item.Slug = await CmsSlug.UniqueAsync(
            _db.CmsNews, news => news.Slug, command.Slug, command.Title, command.Id, ct);

        item.Title = command.Title.Trim();
        item.Content = _sanitizer.Sanitize(command.Content);

        // Tóm tắt bỏ trống thì lấy đoạn đầu của bài, vì trang chủ và danh sách tin đều cần một
        // dòng mô tả — không có thì ô tin chỉ còn mỗi tiêu đề trơ.
        item.Summary = string.IsNullOrWhiteSpace(command.Summary)
            ? HtmlText.Shorten(_sanitizer.ToPlainText(command.Content), 300)
            : command.Summary.Trim();

        item.ThumbnailUrl = command.ThumbnailUrl?.Trim();
        item.CategoryId = command.CategoryId;
        item.Tags = command.Tags?.Trim();
        item.Author = string.IsNullOrWhiteSpace(command.Author)
            ? _currentUser.FullName
            : command.Author.Trim();
        item.IsFeatured = command.IsFeatured;

        if (command.IsPublished)
        {
            item.PublishedAt = command.PublishedAt ?? item.PublishedAt ?? _clock.Now;
        }

        item.IsPublished = command.IsPublished;

        if (command.Id is null)
        {
            _db.CmsNews.Add(item);
        }

        await _db.SaveChangesAsync(ct);
        return item.Id;
    }
}

public record DeleteCmsNewsCommand(Guid Id) : IRequest;

public class DeleteCmsNewsCommandHandler : IRequestHandler<DeleteCmsNewsCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCmsNewsCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCmsNewsCommand command, CancellationToken ct)
    {
        var item = await _db.CmsNews.FirstOrDefaultAsync(news => news.Id == command.Id, ct)
                   ?? throw new NotFoundException("bản tin", command.Id);

        _db.CmsNews.Remove(item);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Đăng hoặc gỡ bài mà không phải mở lại trình soạn thảo (VIII.2).</summary>
public record PublishCmsNewsCommand(Guid Id, bool Publish) : IRequest;

public class PublishCmsNewsCommandHandler : IRequestHandler<PublishCmsNewsCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public PublishCmsNewsCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(PublishCmsNewsCommand command, CancellationToken ct)
    {
        var item = await _db.CmsNews.FirstOrDefaultAsync(news => news.Id == command.Id, ct)
                   ?? throw new NotFoundException("bản tin", command.Id);

        item.IsPublished = command.Publish;

        if (command.Publish)
        {
            item.PublishedAt ??= _clock.Now;
        }

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Thống kê lượt xem tin theo chuyên mục và theo bài (VIII.2).</summary>
public record GetCmsNewsStatisticsQuery(int TopCount = 10) : IRequest<CmsNewsStatisticsDto>;

public record CmsNewsStatisticsDto(
    int TotalCount,
    int PublishedCount,
    int DraftCount,
    int TotalViews,
    IReadOnlyList<CmsNewsCategoryStatDto> ByCategory,
    IReadOnlyList<CmsNewsTopDto> TopViewed);

public record CmsNewsCategoryStatDto(string CategoryName, int NewsCount, int ViewCount);

public record CmsNewsTopDto(Guid Id, string Title, string Slug, int ViewCount, DateTimeOffset? PublishedAt);

public class GetCmsNewsStatisticsQueryHandler
    : IRequestHandler<GetCmsNewsStatisticsQuery, CmsNewsStatisticsDto>
{
    private readonly IApplicationDbContext _db;

    public GetCmsNewsStatisticsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CmsNewsStatisticsDto> Handle(
        GetCmsNewsStatisticsQuery query, CancellationToken ct)
    {
        var news = _db.CmsNews.AsNoTracking();

        // Bài chưa gán chuyên mục cho khóa nhóm rỗng; đặt tên cho nhóm đó sau khi dữ liệu đã về
        // bộ nhớ, vì phép gộp giá trị rỗng không dịch được sang câu lệnh nhóm của cơ sở dữ liệu.
        var byCategory = (await news
                .GroupBy(item => item.Category!.Name)
                .Select(group => new
                {
                    group.Key,
                    NewsCount = group.Count(),
                    ViewCount = group.Sum(item => item.ViewCount)
                })
                .ToListAsync(ct))
            .Select(row => new CmsNewsCategoryStatDto(
                string.IsNullOrWhiteSpace(row.Key) ? "Chưa phân loại" : row.Key,
                row.NewsCount,
                row.ViewCount))
            .OrderByDescending(row => row.NewsCount)
            .ToList();

        var top = await news
            .Where(item => item.IsPublished)
            .OrderByDescending(item => item.ViewCount)
            .Take(Math.Clamp(query.TopCount, 1, 100))
            .Select(item => new CmsNewsTopDto(
                item.Id, item.Title, item.Slug, item.ViewCount, item.PublishedAt))
            .ToListAsync(ct);

        var total = await news.CountAsync(ct);
        var published = await news.CountAsync(item => item.IsPublished, ct);
        var views = await news.SumAsync(item => item.ViewCount, ct);

        return new CmsNewsStatisticsDto(
            total, published, total - published, views, byCategory, top);
    }
}
