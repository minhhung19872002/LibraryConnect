using System.Text.RegularExpressions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Admin.Users;

/// <summary>
/// Nhập người dùng từ Excel (I.2).
///
/// The whole file is validated before anything is written, and the import runs in a single
/// transaction: an operator either gets every valid row or a clean error report, never a half-loaded
/// user list they have to reconcile by hand.
/// </summary>
public record ImportUsersCommand(Stream FileStream, string FileName, bool DryRun) : IRequest<UserImportResultDto>;

public class ImportUsersCommandHandler : IRequestHandler<ImportUsersCommand, UserImportResultDto>
{
    // Column headers of the template produced by GetUserImportTemplateQuery.
    private const string ColumnUsername = "Tên đăng nhập";
    private const string ColumnFullName = "Họ và tên";
    private const string ColumnEmail = "Email";
    private const string ColumnPhone = "Điện thoại";
    private const string ColumnPosition = "Chức vụ";
    private const string ColumnDepartment = "Đơn vị";
    private const string ColumnGroups = "Nhóm quyền";
    private const string ColumnActive = "Kích hoạt";

    private static readonly Regex UsernamePattern = new("^[a-zA-Z0-9._-]{3,100}$", RegexOptions.Compiled);
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;
    private readonly IPasswordHasher _hasher;
    private readonly IPasswordPolicyProvider _policyProvider;
    private readonly IAuditService _audit;

    public ImportUsersCommandHandler(
        IApplicationDbContext db,
        IExcelService excel,
        IPasswordHasher hasher,
        IPasswordPolicyProvider policyProvider,
        IAuditService audit)
    {
        _db = db;
        _excel = excel;
        _hasher = hasher;
        _policyProvider = policyProvider;
        _audit = audit;
    }

    public async Task<UserImportResultDto> Handle(ImportUsersCommand request, CancellationToken ct)
    {
        var sheet = _excel.Read(request.FileStream);
        var result = new UserImportResultDto { TotalRows = sheet.Rows.Count };

        if (!sheet.Headers.Contains(ColumnUsername, StringComparer.OrdinalIgnoreCase) ||
            !sheet.Headers.Contains(ColumnFullName, StringComparer.OrdinalIgnoreCase))
        {
            result.Errors.Add(new ImportRowErrorDto
            {
                Row = 1,
                Message = $"Tệp thiếu cột bắt buộc '{ColumnUsername}' hoặc '{ColumnFullName}'. " +
                          "Vui lòng tải lại tệp mẫu."
            });
            result.ErrorRows = result.TotalRows;
            return result;
        }

        var existingUsernames = await _db.Users
            .Select(u => u.Username)
            .ToListAsync(ct);
        var takenUsernames = existingUsernames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var groupsByName = await _db.UserGroups
            .ToDictionaryAsync(g => g.Name, g => g.Id, StringComparer.OrdinalIgnoreCase, ct);
        var groupsByCode = await _db.UserGroups
            .ToDictionaryAsync(g => g.Code, g => g.Id, StringComparer.OrdinalIgnoreCase, ct);

        var policy = await _policyProvider.GetAsync(ct);
        var pending = new List<(User User, List<Guid> GroupIds, string Password)>();

        foreach (var row in sheet.Rows)
        {
            var rowErrors = new List<ImportRowErrorDto>();

            var username = row.Get(ColumnUsername).ToLowerInvariant();
            var fullName = row.Get(ColumnFullName);
            var email = row.Get(ColumnEmail);
            var phone = row.Get(ColumnPhone);

            if (string.IsNullOrWhiteSpace(username))
            {
                rowErrors.Add(Error(row.RowNumber, ColumnUsername, username, "Chưa nhập tên đăng nhập."));
            }
            else if (!UsernamePattern.IsMatch(username))
            {
                rowErrors.Add(Error(row.RowNumber, ColumnUsername, username,
                    "Tên đăng nhập từ 3 đến 100 ký tự, chỉ gồm chữ cái, chữ số và các ký tự . _ -"));
            }
            else if (!takenUsernames.Add(username))
            {
                // Add() returning false covers both an existing account and a duplicate inside the file.
                rowErrors.Add(Error(row.RowNumber, ColumnUsername, username,
                    "Tên đăng nhập đã tồn tại trong hệ thống hoặc bị lặp trong tệp."));
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                rowErrors.Add(Error(row.RowNumber, ColumnFullName, fullName, "Chưa nhập họ và tên."));
            }

            if (!string.IsNullOrWhiteSpace(email) && !EmailPattern.IsMatch(email))
            {
                rowErrors.Add(Error(row.RowNumber, ColumnEmail, email, "Địa chỉ email không hợp lệ."));
            }

            var groupIds = new List<Guid>();
            var groupCell = row.Get(ColumnGroups);

            foreach (var token in groupCell.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (groupsByName.TryGetValue(token, out var byName))
                {
                    groupIds.Add(byName);
                }
                else if (groupsByCode.TryGetValue(token, out var byCode))
                {
                    groupIds.Add(byCode);
                }
                else
                {
                    rowErrors.Add(Error(row.RowNumber, ColumnGroups, token,
                        $"Không tìm thấy nhóm quyền '{token}'. Nhập theo tên hoặc mã nhóm, nhiều nhóm ngăn cách bằng dấu phẩy."));
                }
            }

            if (rowErrors.Count > 0)
            {
                result.Errors.AddRange(rowErrors);
                result.ErrorRows++;
                continue;
            }

            var password = TemporaryPasswordGenerator.Generate(policy);

            pending.Add((new User
            {
                Username = username,
                PasswordHash = _hasher.Hash(password),
                FullName = fullName.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                Position = NullIfBlank(row.Get(ColumnPosition)),
                Department = NullIfBlank(row.Get(ColumnDepartment)),
                IsActive = ParseBoolean(row.Get(ColumnActive), defaultValue: true),
                MustChangePassword = true
            }, groupIds.Distinct().ToList(), password));
        }

        result.SuccessRows = pending.Count;

        // Dry run backs the "kiểm tra trước khi nhập" step of the wizard: the user sees exactly what
        // would happen without anything being written.
        if (request.DryRun || pending.Count == 0)
        {
            return result;
        }

        foreach (var (user, groupIds, password) in pending)
        {
            _db.Users.Add(user);
            result.GeneratedPasswords[user.Username] = password;

            foreach (var groupId in groupIds)
            {
                _db.UserGroupMembers.Add(new UserGroupMember { UserId = user.Id, GroupId = groupId });
            }
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Import, nameof(User), null, request.FileName,
            newValue: new { result.TotalRows, result.SuccessRows, result.ErrorRows },
            message: $"Nhập người dùng từ tệp '{request.FileName}': thành công {result.SuccessRows}/{result.TotalRows}",
            ct: ct);

        return result;
    }

    private static ImportRowErrorDto Error(int row, string column, string? value, string message) =>
        new() { Row = row, Column = column, Value = value, Message = message };

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool ParseBoolean(string value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "có" or "co" or "x" or "1" or "true" or "yes" => true,
            "không" or "khong" or "0" or "false" or "no" => false,
            _ => defaultValue
        };
    }
}

/// <summary>Tệp Excel mẫu để nhập người dùng, kèm sheet hướng dẫn từng cột.</summary>
public record GetUserImportTemplateQuery : IRequest<byte[]>;

public class GetUserImportTemplateQueryHandler : IRequestHandler<GetUserImportTemplateQuery, byte[]>
{
    private readonly IExcelService _excel;
    private readonly IApplicationDbContext _db;

    public GetUserImportTemplateQueryHandler(IExcelService excel, IApplicationDbContext db)
    {
        _excel = excel;
        _db = db;
    }

    public async Task<byte[]> Handle(GetUserImportTemplateQuery request, CancellationToken ct)
    {
        var groupNames = await _db.UserGroups
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .Select(g => g.Name)
            .ToListAsync(ct);

        var groupHint = groupNames.Count > 0
            ? "Các nhóm hiện có: " + string.Join(", ", groupNames)
            : "Chưa có nhóm quyền nào trong hệ thống.";

        var columns = new List<ExcelTemplateColumn>
        {
            new("Tên đăng nhập", "Bắt buộc, 3–100 ký tự, chỉ gồm chữ cái, chữ số và . _ -", Required: true, Example: "nguyenvana"),
            new("Họ và tên", "Bắt buộc, họ tên đầy đủ có dấu.", Required: true, Example: "Nguyễn Văn A"),
            new("Email", "Dùng để nhận thông báo hệ thống.", Example: "nguyenvana@example.edu.vn"),
            new("Điện thoại", "Số điện thoại liên hệ.", Example: "0912345678"),
            new("Chức vụ", "Chức danh trong đơn vị.", Example: "Cán bộ biên mục"),
            new("Đơn vị", "Phòng/ban công tác.", Example: "Phòng Nghiệp vụ"),
            new("Nhóm quyền", $"Tên hoặc mã nhóm, nhiều nhóm ngăn cách bằng dấu phẩy. {groupHint}", Example: "Cán bộ biên mục"),
            new("Kích hoạt", "Có / Không. Bỏ trống hiểu là Có.", Example: "Có")
        };

        var sample = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                ["Tên đăng nhập"] = "nguyenvana",
                ["Họ và tên"] = "Nguyễn Văn A",
                ["Email"] = "nguyenvana@example.edu.vn",
                ["Điện thoại"] = "0912345678",
                ["Chức vụ"] = "Cán bộ biên mục",
                ["Đơn vị"] = "Phòng Nghiệp vụ",
                ["Nhóm quyền"] = groupNames.FirstOrDefault() ?? string.Empty,
                ["Kích hoạt"] = "Có"
            }
        };

        return _excel.WriteTemplate("Người dùng", columns, sample);
    }
}
