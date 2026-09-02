using System.Security.Cryptography;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Digital;

/// <summary>
/// Gói tài liệu số đọc ngoại tuyến (Phase 15, mục 3.3).
///
/// Ứng dụng xin một gói: máy chủ kiểm quyền bằng đúng bộ quy tắc của đọc trực tuyến, sinh một khoá AES
/// riêng cho lần cấp ấy, ghi hạn dùng, rồi phát tệp gốc đã mã hoá bằng khoá đó. Khoá về máy qua kênh
/// HTTPS đã đăng nhập và ứng dụng cất trong kho bảo mật của hệ điều hành; tệp trên đĩa điện thoại không
/// đọc được nếu thiếu khoá, và hết hạn thì máy chủ không phát nữa, ứng dụng tự xoá.
/// </summary>
public static class OfflinePackageParameters
{
    public const string Days = "DIGITAL.OFFLINE_PACKAGE_DAYS";
}

public record OfflinePackageDto(
    Guid PackageId,
    Guid DocumentId,
    string Title,
    string FileName,
    string MimeType,
    long SizeBytes,
    string Checksum,
    string Algorithm,
    string KeyBase64,
    string IvBase64,
    DateTimeOffset ExpiresAt,
    string DownloadUrl);

public record CreateOfflinePackageCommand(Guid DocumentId) : IRequest<OfflinePackageDto>;

public class CreateOfflinePackageCommandHandler : IRequestHandler<CreateOfflinePackageCommand, OfflinePackageDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDigitalAccessEvaluator _access;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly DigitalAccessRecorder _recorder;

    public CreateOfflinePackageCommandHandler(
        IApplicationDbContext db,
        IDigitalAccessEvaluator access,
        ISystemParameterService parameters,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        DigitalAccessRecorder recorder)
    {
        _db = db;
        _access = access;
        _parameters = parameters;
        _currentUser = currentUser;
        _clock = clock;
        _recorder = recorder;
    }

    public async Task<OfflinePackageDto> Handle(CreateOfflinePackageCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        var document = await _db.DigitalDocuments
            .FirstOrDefaultAsync(row => row.Id == command.DocumentId, ct)
            ?? throw new NotFoundException("tài liệu số", command.DocumentId);

        var permission = await _access.EvaluateAsync(document, ct);

        if (!permission.CanRead)
        {
            throw new ForbiddenException(permission.Reason);
        }

        if (!permission.CanDownload)
        {
            throw new ForbiddenException(
                document.AllowDownload
                    ? permission.Reason
                    : "Tài liệu này chỉ đọc trực tuyến, thư viện không cho tải về máy.");
        }

        var days = Math.Max(1, await _parameters.GetAsync(OfflinePackageParameters.Days, 7, ct));
        var expiresAt = _clock.Now.AddDays(days);

        // Quyền đọc tài liệu hạn chế có hạn riêng: gói không được sống lâu hơn quyền ấy.
        if (permission.AccessExpireAt is { } accessExpireAt && accessExpireAt < expiresAt)
        {
            expiresAt = accessExpireAt;
        }

        var package = new DigitalOfflinePackage
        {
            DocumentId = document.Id,
            ReaderId = readerId,
            KeyBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            IvBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            ExpiresAt = expiresAt,
            SizeBytes = document.FileSize,
            Checksum = document.ChecksumSha256,
        };

        _db.DigitalOfflinePackages.Add(package);
        await _recorder.RecordAsync(document, DigitalAccessAction.OfflineDownload, null, null, ct);
        await _db.SaveChangesAsync(ct);

        return new OfflinePackageDto(
            package.Id,
            document.Id,
            document.Title,
            document.FileName,
            document.MimeType,
            document.FileSize,
            document.ChecksumSha256 ?? string.Empty,
            "AES-256-CBC",
            package.KeyBase64,
            package.IvBase64,
            package.ExpiresAt,
            $"/api/reader/digital/offline-packages/{package.Id}/file");
    }
}

public record DownloadOfflinePackageQuery(Guid PackageId) : IRequest<DigitalFileResult>;

public class DownloadOfflinePackageQueryHandler : IRequestHandler<DownloadOfflinePackageQuery, DigitalFileResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public DownloadOfflinePackageQueryHandler(
        IApplicationDbContext db,
        IFileStorage storage,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _storage = storage;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<DigitalFileResult> Handle(DownloadOfflinePackageQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        var package = await _db.DigitalOfflinePackages
            .Include(row => row.Document)
            .FirstOrDefaultAsync(row => row.Id == query.PackageId && row.ReaderId == readerId, ct)
            ?? throw new NotFoundException("gói tài liệu ngoại tuyến", query.PackageId);

        if (package.IsRevoked)
        {
            throw new ForbiddenException("Gói tài liệu này đã bị thư viện thu hồi.");
        }

        if (package.ExpiresAt < _clock.Now)
        {
            throw new ForbiddenException("Gói tài liệu ngoại tuyến đã hết hạn. Hãy xin cấp lại.");
        }

        var document = package.Document ?? throw new NotFoundException("tài liệu số", package.DocumentId);

        await using var source = await _storage.DownloadAsync(DigitalStorage.Bucket, document.FilePath, ct);
        using var plain = new MemoryStream();
        await source.CopyToAsync(plain, ct);

        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(package.KeyBase64);
        aes.IV = Convert.FromBase64String(package.IvBase64);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(plain.GetBuffer(), 0, (int)plain.Length);

        package.DownloadedAt = _clock.Now;
        package.SizeBytes = plain.Length;
        await _db.SaveChangesAsync(ct);

        return new DigitalFileResult(cipher, "application/octet-stream", $"{package.Id:N}.lcpkg");
    }
}

public record OfflinePackageRowDto(
    Guid PackageId,
    Guid DocumentId,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? DownloadedAt,
    bool IsRevoked,
    bool IsExpired);

public record GetMyOfflinePackagesQuery : IRequest<IReadOnlyList<OfflinePackageRowDto>>;

public class GetMyOfflinePackagesQueryHandler
    : IRequestHandler<GetMyOfflinePackagesQuery, IReadOnlyList<OfflinePackageRowDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetMyOfflinePackagesQueryHandler(IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<OfflinePackageRowDto>> Handle(GetMyOfflinePackagesQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);
        var now = _clock.Now;

        return await _db.DigitalOfflinePackages.AsNoTracking()
            .Where(row => row.ReaderId == readerId)
            .OrderByDescending(row => row.CreatedAt)
            .Select(row => new OfflinePackageRowDto(
                row.Id,
                row.DocumentId,
                row.Document!.Title,
                row.CreatedAt,
                row.ExpiresAt,
                row.DownloadedAt,
                row.IsRevoked,
                row.ExpiresAt < now))
            .ToListAsync(ct);
    }
}
