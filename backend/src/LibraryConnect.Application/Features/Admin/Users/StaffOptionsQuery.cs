using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Admin.Users;

/// <summary>
/// Danh sách cán bộ để **chọn người nhận việc** — phân công biên mục (II.4), phân công kiểm kê
/// (III.4), sau này là mọi chỗ khác cần chỉ định người.
///
/// Tách khỏi <c>/api/admin/users</c> vì hai lẽ. Một, màn hình phân công không phải màn hình quản trị
/// người dùng: cán bộ biên mục phân việc cho đồng nghiệp mà không được xem hồ sơ tài khoản ai cả.
/// Hai, câu trả lời ở đây chỉ có tên và tên đăng nhập, không có email, không có trạng thái khóa,
/// không có nhóm quyền — vừa đủ để hiện lên một ô chọn.
/// </summary>
public record GetStaffOptionsQuery(string? Keyword = null) : IRequest<IReadOnlyList<StaffOptionDto>>;

public record StaffOptionDto(Guid Id, string FullName, string Username);

public class GetStaffOptionsQueryHandler
    : IRequestHandler<GetStaffOptionsQuery, IReadOnlyList<StaffOptionDto>>
{
    /// <summary>Ô chọn không hiện nổi hơn chừng này dòng; gõ để tìm là cách duy nhất đi xa hơn.</summary>
    private const int Limit = 200;

    private readonly IApplicationDbContext _db;

    public GetStaffOptionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<StaffOptionDto>> Handle(
        GetStaffOptionsQuery query, CancellationToken ct)
    {
        var users = _db.Users.AsNoTracking().Where(user => user.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim().ToLowerInvariant();

            // Lọc trong câu hỏi gửi xuống cơ sở dữ liệu, không cắt trước rồi lọc sau (bài học 3).
            users = users.Where(user =>
                DatabaseFunctions.Unaccent(user.FullName).Contains(DatabaseFunctions.Unaccent(keyword))
                || user.Username.ToLower().Contains(keyword));
        }

        return await users
            .OrderBy(user => user.FullName)
            .Take(Limit)
            .Select(user => new StaffOptionDto(user.Id, user.FullName, user.Username))
            .ToListAsync(ct);
    }
}
