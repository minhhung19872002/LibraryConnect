using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Auth;

/// <summary>
/// Returns the signed-in identity together with the effective permission list. The Admin SPA calls
/// this once after login to build the sidebar and to decide which buttons are enabled — the backend
/// still re-checks every permission on every request.
/// </summary>
public record GetCurrentUserQuery : IRequest<AuthUserDto>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, AuthUserDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionResolver _permissions;

    public GetCurrentUserQueryHandler(IApplicationDbContext db, ICurrentUser currentUser, IPermissionResolver permissions)
    {
        _db = db;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<AuthUserDto> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        if (_currentUser.ReaderId is { } readerId)
        {
            var reader = await _db.Readers
                .Where(r => r.Id == readerId && r.DeletedAt == null)
                .Select(r => new AuthUserDto
                {
                    Id = r.Id,
                    Username = r.CardNumber,
                    FullName = r.FullName,
                    Email = r.Email,
                    AvatarUrl = r.AvatarUrl,
                    IsReader = true
                })
                .FirstOrDefaultAsync(ct);

            return reader ?? throw new NotFoundException("bạn đọc", readerId);
        }

        var userId = _currentUser.UserId ?? throw new UnauthorizedException();

        var dto = await _db.Users
            .Where(u => u.Id == userId && u.DeletedAt == null)
            .Select(u => new AuthUserDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl,
                IsReader = false
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("người dùng", userId);

        dto.Groups = await _db.UserGroupMembers
            .Where(m => m.UserId == userId && m.DeletedAt == null)
            .Select(m => m.Group!.Name)
            .ToListAsync(ct);

        dto.Permissions = (await _permissions.GetUserPermissionsAsync(userId, ct)).ToList();

        dto.DataScopes = await _db.UserDataScopes
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .Select(s => new DataScopeDto { ScopeType = s.ScopeType.ToString(), ScopeId = s.ScopeId })
            .ToListAsync(ct);

        return dto;
    }
}
