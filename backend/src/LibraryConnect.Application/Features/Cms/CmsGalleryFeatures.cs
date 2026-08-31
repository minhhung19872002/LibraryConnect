using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Web;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cms;

// ---------------------------------------------------------------------------------------------
// VIII.2 — Thư viện ảnh: mỗi album là một sự kiện, ảnh trong album sắp xếp được.
// ---------------------------------------------------------------------------------------------

public record GetCmsGalleriesQuery(bool PublishedOnly = false)
    : IRequest<IReadOnlyList<CmsGalleryDto>>;

public class GetCmsGalleriesQueryHandler
    : IRequestHandler<GetCmsGalleriesQuery, IReadOnlyList<CmsGalleryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCmsGalleriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CmsGalleryDto>> Handle(
        GetCmsGalleriesQuery query, CancellationToken ct)
    {
        var galleries = _db.CmsGalleries.AsNoTracking().Include(album => album.Images).AsQueryable();

        if (query.PublishedOnly)
        {
            galleries = galleries.Where(album => album.IsPublished);
        }

        var rows = await galleries
            .OrderByDescending(album => album.EventDate)
            .ThenByDescending(album => album.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(Map).ToList();
    }

    internal static CmsGalleryDto Map(CmsGallery album) => new(
        album.Id,
        album.Title,
        album.Description,
        album.CoverUrl,
        album.EventDate,
        album.IsPublished,
        album.Images
            .OrderBy(image => image.SortOrder)
            .Select(image => new CmsGalleryImageDto(
                image.Id, image.ImageUrl, image.Caption, image.SortOrder))
            .ToList());
}

public record GetCmsGalleryQuery(Guid Id) : IRequest<CmsGalleryDto>;

public class GetCmsGalleryQueryHandler : IRequestHandler<GetCmsGalleryQuery, CmsGalleryDto>
{
    private readonly IApplicationDbContext _db;

    public GetCmsGalleryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CmsGalleryDto> Handle(GetCmsGalleryQuery query, CancellationToken ct)
    {
        var album = await _db.CmsGalleries.AsNoTracking()
                        .Include(entity => entity.Images)
                        .FirstOrDefaultAsync(entity => entity.Id == query.Id, ct)
                    ?? throw new NotFoundException("album ảnh", query.Id);

        return GetCmsGalleriesQueryHandler.Map(album);
    }
}

public class SaveCmsGalleryCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverUrl { get; set; }
    public DateOnly? EventDate { get; set; }
    public bool IsPublished { get; set; }
    public List<CmsGalleryImageInput> Images { get; set; } = new();
}

public record CmsGalleryImageInput(Guid? Id, string ImageUrl, string? Caption, int SortOrder);

public class SaveCmsGalleryCommandValidator : AbstractValidator<SaveCmsGalleryCommand>
{
    public SaveCmsGalleryCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Chưa nhập tên album.")
            .MaximumLength(200).WithMessage("Tên album tối đa 200 ký tự.");

        RuleForEach(command => command.Images)
            .Must(image => !string.IsNullOrWhiteSpace(image.ImageUrl))
            .WithMessage("Có ảnh trong album chưa có đường dẫn tệp.");
    }
}

public class SaveCmsGalleryCommandHandler : IRequestHandler<SaveCmsGalleryCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveCmsGalleryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveCmsGalleryCommand command, CancellationToken ct)
    {
        var album = command.Id is null
            ? new CmsGallery()
            : await _db.CmsGalleries
                  .Include(entity => entity.Images)
                  .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
              ?? throw new NotFoundException("album ảnh", command.Id.Value);

        album.Title = command.Title.Trim();
        album.Description = command.Description?.Trim();
        album.EventDate = command.EventDate;
        album.IsPublished = command.IsPublished;

        if (command.Id is null)
        {
            _db.CmsGalleries.Add(album);
        }

        var keep = command.Images.Where(image => image.Id is not null)
            .Select(image => image.Id!.Value)
            .ToHashSet();

        foreach (var removed in album.Images.Where(image => !keep.Contains(image.Id)).ToList())
        {
            album.Images.Remove(removed);
            _db.CmsGalleryImages.Remove(removed);
        }

        foreach (var input in command.Images)
        {
            var image = input.Id is null
                ? null
                : album.Images.FirstOrDefault(entity => entity.Id == input.Id);

            if (image is null)
            {
                image = new CmsGalleryImage { GalleryId = album.Id };
                album.Images.Add(image);
            }

            image.ImageUrl = input.ImageUrl.Trim();
            image.Caption = input.Caption?.Trim();
            image.SortOrder = input.SortOrder;
        }

        // Ảnh bìa bỏ trống thì lấy ảnh đầu tiên: danh sách album trên trang tra cứu là một lưới
        // ảnh, một ô trống giữa lưới trông như lỗi tải trang.
        album.CoverUrl = string.IsNullOrWhiteSpace(command.CoverUrl)
            ? album.Images.OrderBy(image => image.SortOrder).FirstOrDefault()?.ImageUrl
            : command.CoverUrl.Trim();

        await _db.SaveChangesAsync(ct);
        return album.Id;
    }
}

public record DeleteCmsGalleryCommand(Guid Id) : IRequest;

public class DeleteCmsGalleryCommandHandler : IRequestHandler<DeleteCmsGalleryCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCmsGalleryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCmsGalleryCommand command, CancellationToken ct)
    {
        var album = await _db.CmsGalleries
                        .Include(entity => entity.Images)
                        .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                    ?? throw new NotFoundException("album ảnh", command.Id);

        foreach (var image in album.Images.ToList())
        {
            _db.CmsGalleryImages.Remove(image);
        }

        _db.CmsGalleries.Remove(album);
        await _db.SaveChangesAsync(ct);
    }
}
