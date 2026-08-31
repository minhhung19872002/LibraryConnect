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
// VIII.1 — Trang tĩnh: Giới thiệu, Nội quy, Hướng dẫn sử dụng, Liên hệ, Hỏi đáp.
// ---------------------------------------------------------------------------------------------

public record GetCmsPagesQuery(CmsPageListRequest Request) : IRequest<PagedResult<CmsPageRowDto>>;

public class GetCmsPagesQueryHandler : IRequestHandler<GetCmsPagesQuery, PagedResult<CmsPageRowDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCmsPagesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<CmsPageRowDto>> Handle(GetCmsPagesQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var pages = _db.CmsPages.AsNoTracking();

        if (request.HasKeyword())
        {
            var keyword = VietnameseText.RemoveDiacritics(request.Keyword!.Trim()).ToLowerInvariant();

            pages = pages.Where(page =>
                DatabaseFunctions.Unaccent(page.Title).Contains(keyword)
                || page.Slug.ToLower().Contains(keyword));
        }

        return await pages
            .WhereIf(request.IsPublished is not null, page => page.IsPublished == request.IsPublished)
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
            .ToPagedResultAsync(request, ct);
    }
}

public record GetCmsPageQuery(Guid Id) : IRequest<CmsPageDto>;

public class GetCmsPageQueryHandler : IRequestHandler<GetCmsPageQuery, CmsPageDto>
{
    private readonly IApplicationDbContext _db;

    public GetCmsPageQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CmsPageDto> Handle(GetCmsPageQuery query, CancellationToken ct)
    {
        var page = await _db.CmsPages.AsNoTracking()
                       .FirstOrDefaultAsync(entity => entity.Id == query.Id, ct)
                   ?? throw new NotFoundException("trang tĩnh", query.Id);

        return new CmsPageDto(
            page.Id,
            page.Slug,
            page.Title,
            page.Content,
            page.MetaDescription,
            page.IsPublished,
            page.PublishedAt,
            page.ViewCount,
            page.SortOrder,
            page.ParentId);
    }
}

public class SaveCmsPageCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    /// <summary>Bỏ trống thì hệ thống tự sinh từ tiêu đề.</summary>
    public string? Slug { get; set; }
    public string? Content { get; set; }
    public string? MetaDescription { get; set; }
    public bool IsPublished { get; set; }
    public int SortOrder { get; set; }
    public Guid? ParentId { get; set; }
}

public class SaveCmsPageCommandValidator : AbstractValidator<SaveCmsPageCommand>
{
    public SaveCmsPageCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Chưa nhập tiêu đề trang.")
            .MaximumLength(300).WithMessage("Tiêu đề tối đa 300 ký tự.");

        RuleFor(command => command.MetaDescription)
            .MaximumLength(300).WithMessage("Mô tả cho công cụ tìm kiếm tối đa 300 ký tự.");
    }
}

public class SaveCmsPageCommandHandler : IRequestHandler<SaveCmsPageCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IHtmlSanitizer _sanitizer;
    private readonly IDateTimeProvider _clock;

    public SaveCmsPageCommandHandler(
        IApplicationDbContext db, IHtmlSanitizer sanitizer, IDateTimeProvider clock)
    {
        _db = db;
        _sanitizer = sanitizer;
        _clock = clock;
    }

    public async Task<Guid> Handle(SaveCmsPageCommand command, CancellationToken ct)
    {
        var page = command.Id is null
            ? new CmsPage()
            : await _db.CmsPages.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
              ?? throw new NotFoundException("trang tĩnh", command.Id.Value);

        var slug = await CmsSlug.UniqueAsync(
            _db.CmsPages,
            entity => entity.Slug,
            command.Slug,
            command.Title,
            command.Id,
            ct);

        page.Title = command.Title.Trim();
        page.Slug = slug;
        page.Content = _sanitizer.Sanitize(command.Content);
        page.MetaDescription = string.IsNullOrWhiteSpace(command.MetaDescription)
            ? HtmlText.Shorten(_sanitizer.ToPlainText(command.Content), 160)
            : command.MetaDescription.Trim();
        page.SortOrder = command.SortOrder;
        page.ParentId = command.ParentId;

        // Mốc xuất bản chỉ đặt lần đầu; sửa lại bài đã đăng không được đẩy nó lên đầu danh sách.
        if (command.IsPublished && !page.IsPublished)
        {
            page.PublishedAt = _clock.Now;
        }

        page.IsPublished = command.IsPublished;

        if (command.Id is null)
        {
            _db.CmsPages.Add(page);
        }

        await _db.SaveChangesAsync(ct);
        return page.Id;
    }
}

public record DeleteCmsPageCommand(Guid Id) : IRequest;

public class DeleteCmsPageCommandHandler : IRequestHandler<DeleteCmsPageCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCmsPageCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCmsPageCommand command, CancellationToken ct)
    {
        var page = await _db.CmsPages.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                   ?? throw new NotFoundException("trang tĩnh", command.Id);

        var hasChildren = await _db.CmsPages.AnyAsync(entity => entity.ParentId == page.Id, ct);

        if (hasChildren)
        {
            throw new ConflictException(
                "Trang này còn trang con bên dưới. Hãy chuyển hoặc xóa các trang con trước.");
        }

        _db.CmsPages.Remove(page);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Sinh đường dẫn duy nhất cho trang, tin và album.
///
/// Đường dẫn nằm trên thanh địa chỉ và bị công cụ tìm kiếm ghi nhớ, nên hai bài không được trùng
/// nhau. Trùng thì nối thêm số thứ tự chứ không báo lỗi bắt cán bộ tự nghĩ tên khác — họ không có
/// cách nào biết bài nào đang giữ đường dẫn đó.
/// </summary>
public static class CmsSlug
{
    public static async Task<string> UniqueAsync<TEntity>(
        IQueryable<TEntity> source,
        System.Linq.Expressions.Expression<Func<TEntity, string>> selector,
        string? requested,
        string fallbackTitle,
        Guid? currentId,
        CancellationToken ct)
        where TEntity : Domain.Common.BaseEntity
    {
        var seed = string.IsNullOrWhiteSpace(requested)
            ? VietnameseText.UrlSlug(fallbackTitle)
            : VietnameseText.UrlSlug(requested);

        if (string.IsNullOrWhiteSpace(seed))
        {
            seed = "trang";
        }

        var taken = await source
            .Where(entity => currentId == null || entity.Id != currentId)
            .Select(selector)
            .ToListAsync(ct);

        var used = new HashSet<string>(taken, StringComparer.OrdinalIgnoreCase);

        if (!used.Contains(seed))
        {
            return seed;
        }

        for (var index = 2; index < 10_000; index++)
        {
            var candidate = $"{seed}-{index}";

            if (!used.Contains(candidate))
            {
                return candidate;
            }
        }

        // Không bao giờ tới đây với dữ liệu thật, nhưng thà sinh chuỗi ngẫu nhiên còn hơn treo.
        return $"{seed}-{Guid.NewGuid():N}";
    }
}

/// <summary>Cắt ngắn đoạn chữ theo ranh giới từ, dùng cho tóm tắt và thẻ mô tả tự sinh.</summary>
public static class HtmlText
{
    public static string? Shorten(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text.Trim();

        if (text.Length <= maxLength)
        {
            return text;
        }

        var cut = text[..maxLength];
        var lastSpace = cut.LastIndexOf(' ');

        if (lastSpace > maxLength / 2)
        {
            cut = cut[..lastSpace];
        }

        return cut.TrimEnd(' ', ',', ';', '.') + "…";
    }
}
