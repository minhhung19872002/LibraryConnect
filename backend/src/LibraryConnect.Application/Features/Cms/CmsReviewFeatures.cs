using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cms;

// ---------------------------------------------------------------------------------------------
// Kiểm duyệt nhận xét bạn đọc gửi từ trang tra cứu (IX.2).
//
// Nhận xét chỉ hiện sau khi cán bộ duyệt. Đây là nội dung do người ngoài viết và hiện công khai
// dưới tên thư viện, nên mặc định là chờ duyệt chứ không phải đăng ngay rồi gỡ sau.
// ---------------------------------------------------------------------------------------------

public record GetCmsReviewsQuery(CmsReviewListRequest Request)
    : IRequest<PagedResult<CmsReviewRowDto>>;

public class GetCmsReviewsQueryHandler
    : IRequestHandler<GetCmsReviewsQuery, PagedResult<CmsReviewRowDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCmsReviewsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<CmsReviewRowDto>> Handle(
        GetCmsReviewsQuery query, CancellationToken ct)
    {
        var request = query.Request;

        return await _db.OpacReviews.AsNoTracking()
            .WhereIf(request.IsApproved is not null,
                review => review.IsApproved == request.IsApproved)
            .OrderByDescending(review => review.CreatedAt)
            .Select(review => new CmsReviewRowDto(
                review.Id,
                review.BibId,
                review.Bib!.Title,
                review.ReaderId,
                review.Reader!.FullName,
                review.Reader.CardNumber,
                review.Rating,
                review.Comment,
                review.IsApproved,
                review.CreatedAt))
            .ToPagedResultAsync(request, ct);
    }
}

/// <summary>Duyệt hoặc bỏ duyệt một nhận xét.</summary>
public record ModerateCmsReviewCommand(Guid Id, bool Approve) : IRequest;

public class ModerateCmsReviewCommandHandler : IRequestHandler<ModerateCmsReviewCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ModerateCmsReviewCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ModerateCmsReviewCommand command, CancellationToken ct)
    {
        var review = await _db.OpacReviews.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                     ?? throw new NotFoundException("nhận xét", command.Id);

        review.IsApproved = command.Approve;
        review.ApprovedBy = command.Approve ? _currentUser.UserId : null;

        await _db.SaveChangesAsync(ct);
    }
}

public record DeleteCmsReviewCommand(Guid Id) : IRequest;

public class DeleteCmsReviewCommandHandler : IRequestHandler<DeleteCmsReviewCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCmsReviewCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCmsReviewCommand command, CancellationToken ct)
    {
        var review = await _db.OpacReviews.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                     ?? throw new NotFoundException("nhận xét", command.Id);

        _db.OpacReviews.Remove(review);
        await _db.SaveChangesAsync(ct);
    }
}
