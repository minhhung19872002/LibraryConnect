using LibraryConnect.Application.Common.Security;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>Default administrator account created on an empty database (section 8).</summary>
    public const string DefaultAdminUsername = "admin";

    /// <summary>
    /// Temporary password for the seeded administrator. The account is flagged
    /// <c>MustChangePassword</c>, so this value only ever survives until the first sign-in.
    /// </summary>
    public const string DefaultAdminPassword = "LibraryConnect@2025";

    /// <summary>
    /// The staff groups every deployment starts with. The permission selector is a predicate over
    /// the permission catalogue, so a new permission is automatically granted to the groups whose
    /// responsibility it falls under when the product is upgraded.
    /// </summary>
    private static readonly IReadOnlyList<GroupSeed> GroupSeeds = new List<GroupSeed>
    {
        new(PermissionResolver.SystemAdministratorGroupCode, "Quản trị hệ thống",
            "Toàn quyền trên hệ thống, quản lý người dùng, tham số, sao lưu và nhật ký.",
            _ => true),

        new("CATALOGER", "Cán bộ biên mục",
            "Biên mục biểu ghi MARC 21, quản lý định nghĩa trường, hàng đợi biên mục, in phích, nhập biểu ghi từ ISO 2709 / Z39.50.",
            code => code.StartsWith("CATALOG.", StringComparison.Ordinal)
                    || code.StartsWith("EXCHANGE.DATA.", StringComparison.Ordinal)
                    || code is PermissionCodes.AcqItemView or PermissionCodes.SerialArticleManage
                        or PermissionCodes.DigitalView or PermissionCodes.CourseDocumentLink),

        new("ACQUISITION", "Cán bộ bổ sung",
            "Lập yêu cầu và đơn đặt, kiểm nhận, đăng ký cá biệt, in mã vạch, kiểm kê, quản lý kho và ấn phẩm định kỳ.",
            code => code.StartsWith("ACQ.", StringComparison.Ordinal)
                    || code.StartsWith("SERIAL.", StringComparison.Ordinal)
                    || code is PermissionCodes.CatalogBibView or PermissionCodes.CatalogBibCreate
                        or PermissionCodes.CatalogListView or PermissionCodes.CatalogListCreate),

        new("CIRCULATION", "Cán bộ lưu thông",
            "Ghi mượn, ghi trả, gia hạn, đặt giữ, thu tiền phạt, tủ gửi đồ và các báo cáo lưu thông.",
            code => code.StartsWith("CIRCULATION.", StringComparison.Ordinal)
                    || code is PermissionCodes.ReaderView or PermissionCodes.ReaderUpdate
                        or PermissionCodes.ReaderLock or PermissionCodes.ReaderExtendCard
                        or PermissionCodes.ReaderPrintCard or PermissionCodes.ReaderViolationManage
                        or PermissionCodes.CatalogBibView or PermissionCodes.AcqItemView),

        new("LIBRARIAN", "Thủ thư",
            "Quản lý hồ sơ bạn đọc, tài liệu số, nội dung trang thông tin và tra cứu toàn hệ thống.",
            code => code.StartsWith("READER.", StringComparison.Ordinal)
                    || code.StartsWith("DIGITAL.", StringComparison.Ordinal)
                    || code.StartsWith("CMS.", StringComparison.Ordinal)
                    || code.StartsWith("COURSE.", StringComparison.Ordinal)
                    || code is PermissionCodes.CatalogBibView or PermissionCodes.AcqItemView
                        or PermissionCodes.CirculationLoanView)
    };

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        var existing = await _db.Permissions.ToDictionaryAsync(p => p.Code, ct);
        var added = 0;
        var sortOrder = 0;

        foreach (var definition in PermissionCodes.All)
        {
            sortOrder += 10;

            if (existing.TryGetValue(definition.Code, out var permission))
            {
                // Keep the Vietnamese labels in step with the catalogue after an upgrade.
                permission.Module = definition.Module;
                permission.Group = definition.Group;
                permission.Name = definition.Name;
                permission.SortOrder = sortOrder;
                continue;
            }

            _db.Permissions.Add(new Permission
            {
                Code = definition.Code,
                Module = definition.Module,
                Group = definition.Group,
                Name = definition.Name,
                SortOrder = sortOrder,
                CreatedAt = _clock.Now
            });

            added++;
        }

        if (await _db.SaveChangesAsync(ct) > 0)
        {
            _logger.LogInformation("Đã bổ sung {Count} mã quyền mới (tổng {Total})", added, PermissionCodes.All.Count);
        }
    }

    private async Task SeedUserGroupsAsync(CancellationToken ct)
    {
        var permissions = await _db.Permissions.ToListAsync(ct);

        foreach (var seed in GroupSeeds)
        {
            var group = await _db.UserGroups.FirstOrDefaultAsync(g => g.Code == seed.Code, ct);

            if (group is null)
            {
                group = new UserGroup
                {
                    Code = seed.Code,
                    Name = seed.Name,
                    Description = seed.Description,
                    IsSystem = true,
                    CreatedAt = _clock.Now
                };

                _db.UserGroups.Add(group);
                await _db.SaveChangesAsync(ct);
            }

            var granted = await _db.GroupPermissions
                .Where(gp => gp.GroupId == group.Id)
                .Select(gp => gp.PermissionId)
                .ToListAsync(ct);

            var grantedSet = granted.ToHashSet();

            var missing = permissions
                .Where(p => seed.Matches(p.Code) && !grantedSet.Contains(p.Id))
                .Select(p => new GroupPermission
                {
                    GroupId = group.Id,
                    PermissionId = p.Id,
                    CreatedAt = _clock.Now
                })
                .ToList();

            if (missing.Count > 0)
            {
                _db.GroupPermissions.AddRange(missing);
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Nhóm {Group}: cấp thêm {Count} quyền", seed.Name, missing.Count);
            }
        }
    }

    private async Task SeedAdministratorAsync(CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(u => u.Username == DefaultAdminUsername, ct))
        {
            return;
        }

        var admin = new User
        {
            Username = DefaultAdminUsername,
            PasswordHash = _hasher.Hash(DefaultAdminPassword),
            FullName = "Quản trị hệ thống",
            Email = null,
            IsActive = true,
            // Forces the password change on the very first sign-in, as section 8 requires.
            MustChangePassword = true,
            CreatedAt = _clock.Now
        };

        _db.Users.Add(admin);
        await _db.SaveChangesAsync(ct);

        var adminGroup = await _db.UserGroups
            .FirstAsync(g => g.Code == PermissionResolver.SystemAdministratorGroupCode, ct);

        _db.UserGroupMembers.Add(new UserGroupMember
        {
            UserId = admin.Id,
            GroupId = adminGroup.Id,
            CreatedAt = _clock.Now
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Đã tạo tài khoản quản trị mặc định '{Username}' với mật khẩu tạm thời. " +
            "Hệ thống sẽ bắt buộc đổi mật khẩu ở lần đăng nhập đầu tiên.",
            DefaultAdminUsername);
    }

    /// <summary>One seeded staff group and the rule deciding which permissions it receives.</summary>
    private sealed record GroupSeed(string Code, string Name, string Description, Func<string, bool> Matches);
}
