using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Web;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cms;

// ---------------------------------------------------------------------------------------------
// VIII.1 — Bố cục trang tra cứu: cây menu điều hướng, banner trang chủ và liên kết website.
// ---------------------------------------------------------------------------------------------

/// <summary>Cây menu điều hướng. Trả về dạng cây để giao diện dựng thẳng, không phải tự gom.</summary>
public record GetCmsMenusQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<CmsMenuDto>>;

public class GetCmsMenusQueryHandler : IRequestHandler<GetCmsMenusQuery, IReadOnlyList<CmsMenuDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCmsMenusQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CmsMenuDto>> Handle(
        GetCmsMenusQuery query, CancellationToken ct)
    {
        // Nạp cả mục đang tắt rồi mới tỉa: tắt một mục cha là ẩn cả nhánh, chứ không phải đẩy các
        // mục con lên hàng đầu của thanh điều hướng.
        var rows = await _db.CmsMenus.AsNoTracking()
            .OrderBy(menu => menu.SortOrder)
            .ThenBy(menu => menu.Name)
            .ToListAsync(ct);

        var tree = CmsMenuTree.Build(rows);

        return query.IncludeInactive ? tree : CmsMenuTree.Prune(tree);
    }
}

/// <summary>
/// Dựng cây menu từ danh sách phẳng, và tỉa nhánh đang tắt.
///
/// Mục trỏ tới một cha không còn tồn tại vẫn được đưa lên hàng gốc chứ không biến mất: cán bộ phải
/// nhìn thấy nó trên màn hình quản trị thì mới sửa hoặc xóa được.
/// </summary>
public static class CmsMenuTree
{
    public static IReadOnlyList<CmsMenuDto> Build(IReadOnlyList<CmsMenu> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var known = rows.Select(row => row.Id).ToHashSet();

        CmsMenuDto ToDto(CmsMenu row) => new(
            row.Id,
            row.Name,
            row.Url,
            row.ParentId,
            row.SortOrder,
            row.Target,
            row.Icon,
            row.IsActive,
            ChildrenOf(row.Id));

        IReadOnlyList<CmsMenuDto> ChildrenOf(Guid? parentId) => rows
            .Where(row => row.ParentId == parentId)
            .Select(ToDto)
            .ToList();

        var roots = ChildrenOf(null).ToList();

        var orphans = rows
            .Where(row => row.ParentId is not null && !known.Contains(row.ParentId.Value))
            .Select(ToDto);

        roots.AddRange(orphans);

        return roots;
    }

    /// <summary>Bỏ các mục đang tắt cùng toàn bộ nhánh bên dưới chúng.</summary>
    public static IReadOnlyList<CmsMenuDto> Prune(IReadOnlyList<CmsMenuDto> tree)
    {
        ArgumentNullException.ThrowIfNull(tree);

        return tree
            .Where(node => node.IsActive)
            .Select(node => node with { Children = Prune(node.Children) })
            .ToList();
    }
}

public class SaveCmsMenuCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
    /// <summary>_self hoặc _blank.</summary>
    public string? Target { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveCmsMenuCommandValidator : AbstractValidator<SaveCmsMenuCommand>
{
    public SaveCmsMenuCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên mục menu.")
            .MaximumLength(200).WithMessage("Tên mục tối đa 200 ký tự.");

        RuleFor(command => command.Url)
            .MaximumLength(500).WithMessage("Địa chỉ tối đa 500 ký tự.");
    }
}

public class SaveCmsMenuCommandHandler : IRequestHandler<SaveCmsMenuCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveCmsMenuCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveCmsMenuCommand command, CancellationToken ct)
    {
        var menu = command.Id is null
            ? new CmsMenu()
            : await _db.CmsMenus.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
              ?? throw new NotFoundException("mục menu", command.Id.Value);

        if (command.ParentId is not null)
        {
            if (command.ParentId == command.Id)
            {
                throw new ConflictException("Một mục menu không thể là cha của chính nó.");
            }

            var parents = await _db.CmsMenus.AsNoTracking()
                .Select(entity => new { entity.Id, entity.ParentId })
                .ToListAsync(ct);

            if (!parents.Any(entity => entity.Id == command.ParentId))
            {
                throw new NotFoundException("mục menu cha", command.ParentId.Value);
            }

            // Đặt cha là một mục nằm dưới chính nó thì cây thành vòng, và mọi vòng lặp duyệt cây
            // sau đó treo máy chủ. Chặn ngay ở đây.
            var walker = command.ParentId;

            while (walker is not null)
            {
                if (walker == command.Id)
                {
                    throw new ConflictException(
                        "Không đặt được mục cha nằm bên dưới chính mục này.");
                }

                walker = parents.FirstOrDefault(entity => entity.Id == walker)?.ParentId;
            }
        }

        menu.Name = command.Name.Trim();
        menu.Url = command.Url?.Trim();
        menu.ParentId = command.ParentId;
        menu.SortOrder = command.SortOrder;
        menu.Target = command.Target;
        menu.Icon = command.Icon?.Trim();
        menu.IsActive = command.IsActive;

        if (command.Id is null)
        {
            _db.CmsMenus.Add(menu);
        }

        await _db.SaveChangesAsync(ct);
        return menu.Id;
    }
}

public record DeleteCmsMenuCommand(Guid Id) : IRequest;

public class DeleteCmsMenuCommandHandler : IRequestHandler<DeleteCmsMenuCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCmsMenuCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCmsMenuCommand command, CancellationToken ct)
    {
        var menu = await _db.CmsMenus.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                   ?? throw new NotFoundException("mục menu", command.Id);

        if (await _db.CmsMenus.AnyAsync(entity => entity.ParentId == menu.Id, ct))
        {
            throw new ConflictException("Mục này còn mục con bên dưới, hãy xóa hoặc chuyển chúng trước.");
        }

        _db.CmsMenus.Remove(menu);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Sắp xếp lại menu sau khi kéo thả trên giao diện.</summary>
public record ReorderCmsMenusCommand(IReadOnlyList<CmsMenuPositionDto> Items) : IRequest;

public record CmsMenuPositionDto(Guid Id, Guid? ParentId, int SortOrder);

public class ReorderCmsMenusCommandHandler : IRequestHandler<ReorderCmsMenusCommand>
{
    private readonly IApplicationDbContext _db;

    public ReorderCmsMenusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(ReorderCmsMenusCommand command, CancellationToken ct)
    {
        var ids = command.Items.Select(item => item.Id).ToList();
        var menus = await _db.CmsMenus.Where(menu => ids.Contains(menu.Id)).ToListAsync(ct);

        foreach (var menu in menus)
        {
            var position = command.Items.First(item => item.Id == menu.Id);
            menu.ParentId = position.ParentId;
            menu.SortOrder = position.SortOrder;
        }

        await _db.SaveChangesAsync(ct);
    }
}

// ---------------------------------------------------------------------------------------------
// Banner trang chủ
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Danh sách banner. <paramref name="ActiveOnly"/> lọc theo cả công tắc lẫn khoảng ngày hiển thị —
/// đây là thứ trang chủ dùng, còn màn hình quản trị lấy hết để cán bộ sửa được banner đã hết hạn.
/// </summary>
public record GetCmsBannersQuery(bool ActiveOnly = false, string? Position = null)
    : IRequest<IReadOnlyList<CmsBannerDto>>;

public class GetCmsBannersQueryHandler
    : IRequestHandler<GetCmsBannersQuery, IReadOnlyList<CmsBannerDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetCmsBannersQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<CmsBannerDto>> Handle(
        GetCmsBannersQuery query, CancellationToken ct)
    {
        var banners = _db.CmsBanners.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Position))
        {
            banners = banners.Where(banner => banner.Position == query.Position);
        }

        if (query.ActiveOnly)
        {
            var today = DateOnly.FromDateTime(_clock.Now.Date);

            banners = banners.Where(banner =>
                banner.IsActive
                && (banner.StartDate == null || banner.StartDate <= today)
                && (banner.EndDate == null || banner.EndDate >= today));
        }

        return await banners
            .OrderBy(banner => banner.SortOrder)
            .ThenBy(banner => banner.Title)
            .Select(banner => new CmsBannerDto(
                banner.Id,
                banner.Title,
                banner.ImageUrl,
                banner.Link,
                banner.Position,
                banner.SortOrder,
                banner.StartDate,
                banner.EndDate,
                banner.IsActive))
            .ToListAsync(ct);
    }
}

public class SaveCmsBannerCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string Position { get; set; } = "HOME_SLIDER";
    public int SortOrder { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveCmsBannerCommandValidator : AbstractValidator<SaveCmsBannerCommand>
{
    private static readonly string[] Positions = { "HOME_SLIDER", "SIDEBAR", "FOOTER" };

    public SaveCmsBannerCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Chưa nhập tiêu đề banner.")
            .MaximumLength(200).WithMessage("Tiêu đề tối đa 200 ký tự.");

        RuleFor(command => command.ImageUrl)
            .NotEmpty().WithMessage("Chưa chọn ảnh banner.");

        RuleFor(command => command.Position)
            .Must(position => Positions.Contains(position))
            .WithMessage("Vị trí hiển thị không hợp lệ.");

        RuleFor(command => command.EndDate)
            .GreaterThanOrEqualTo(command => command.StartDate!.Value)
            .When(command => command.StartDate is not null && command.EndDate is not null)
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
    }
}

public class SaveCmsBannerCommandHandler : IRequestHandler<SaveCmsBannerCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveCmsBannerCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveCmsBannerCommand command, CancellationToken ct)
    {
        var banner = command.Id is null
            ? new CmsBanner()
            : await _db.CmsBanners.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
              ?? throw new NotFoundException("banner", command.Id.Value);

        banner.Title = command.Title.Trim();
        banner.ImageUrl = command.ImageUrl.Trim();
        banner.Link = command.Link?.Trim();
        banner.Position = command.Position;
        banner.SortOrder = command.SortOrder;
        banner.StartDate = command.StartDate;
        banner.EndDate = command.EndDate;
        banner.IsActive = command.IsActive;

        if (command.Id is null)
        {
            _db.CmsBanners.Add(banner);
        }

        await _db.SaveChangesAsync(ct);
        return banner.Id;
    }
}

public record DeleteCmsBannerCommand(Guid Id) : IRequest;

public class DeleteCmsBannerCommandHandler : IRequestHandler<DeleteCmsBannerCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCmsBannerCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCmsBannerCommand command, CancellationToken ct)
    {
        var banner = await _db.CmsBanners.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                     ?? throw new NotFoundException("banner", command.Id);

        _db.CmsBanners.Remove(banner);
        await _db.SaveChangesAsync(ct);
    }
}

// ---------------------------------------------------------------------------------------------
// Liên kết website: thư viện bạn và cơ sở dữ liệu trực tuyến
// ---------------------------------------------------------------------------------------------

public record GetCmsLinksQuery(bool ActiveOnly = false) : IRequest<IReadOnlyList<CmsExternalLinkDto>>;

public class GetCmsLinksQueryHandler
    : IRequestHandler<GetCmsLinksQuery, IReadOnlyList<CmsExternalLinkDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCmsLinksQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CmsExternalLinkDto>> Handle(
        GetCmsLinksQuery query, CancellationToken ct)
    {
        var links = _db.CmsExternalLinks.AsNoTracking();

        if (query.ActiveOnly)
        {
            links = links.Where(link => link.IsActive);
        }

        return await links
            .OrderBy(link => link.GroupName)
            .ThenBy(link => link.SortOrder)
            .ThenBy(link => link.Name)
            .Select(link => new CmsExternalLinkDto(
                link.Id,
                link.Name,
                link.Url,
                link.LogoUrl,
                link.Description,
                link.GroupName,
                link.SortOrder,
                link.IsActive))
            .ToListAsync(ct);
    }
}

public class SaveCmsLinkCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? GroupName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveCmsLinkCommandValidator : AbstractValidator<SaveCmsLinkCommand>
{
    public SaveCmsLinkCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên liên kết.")
            .MaximumLength(200).WithMessage("Tên liên kết tối đa 200 ký tự.");

        RuleFor(command => command.Url)
            .NotEmpty().WithMessage("Chưa nhập địa chỉ liên kết.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Địa chỉ liên kết phải bắt đầu bằng http:// hoặc https://.");
    }
}

public class SaveCmsLinkCommandHandler : IRequestHandler<SaveCmsLinkCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveCmsLinkCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveCmsLinkCommand command, CancellationToken ct)
    {
        var link = command.Id is null
            ? new CmsExternalLink()
            : await _db.CmsExternalLinks.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
              ?? throw new NotFoundException("liên kết website", command.Id.Value);

        link.Name = command.Name.Trim();
        link.Url = command.Url.Trim();
        link.LogoUrl = command.LogoUrl?.Trim();
        link.Description = command.Description?.Trim();
        link.GroupName = command.GroupName?.Trim();
        link.SortOrder = command.SortOrder;
        link.IsActive = command.IsActive;

        if (command.Id is null)
        {
            _db.CmsExternalLinks.Add(link);
        }

        await _db.SaveChangesAsync(ct);
        return link.Id;
    }
}

public record DeleteCmsLinkCommand(Guid Id) : IRequest;

public class DeleteCmsLinkCommandHandler : IRequestHandler<DeleteCmsLinkCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCmsLinkCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCmsLinkCommand command, CancellationToken ct)
    {
        var link = await _db.CmsExternalLinks.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                   ?? throw new NotFoundException("liên kết website", command.Id);

        _db.CmsExternalLinks.Remove(link);
        await _db.SaveChangesAsync(ct);
    }
}
