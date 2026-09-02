using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Domain.Entities.Cir;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Circulation;

/// <summary>
/// Xác thực vị trí cho mượn tự phục vụ (Phase 15, mục 3.2).
///
/// E-HSMT yêu cầu bạn đọc "tự vào kho chọn sách và quét mượn", nên phải chống mượn từ xa. Máy chủ giữ
/// toàn bộ quy tắc: ứng dụng chỉ gửi lên thứ nó nhìn thấy (tên Wi-Fi hoặc nội dung mã QR), máy chủ đối
/// chiếu rồi cấp một <b>phiếu xác thực</b> ký HMAC có hạn dùng; lượt mượn sau đó phải nộp phiếu ấy.
/// Cách này giữ được "hiệu lực 15 phút" dù mã QR dán tại kho là tĩnh: cái có hạn là phiếu do máy chủ
/// cấp, không phải mã QR.
/// </summary>
public static class SelfCheckoutParameters
{
    /// <summary>NONE | WIFI_SSID | QR_STATION.</summary>
    public const string VerifyMode = "CIRCULATION.SELF_CHECKOUT_VERIFY_MODE";

    /// <summary>Phiếu xác thực sống bao nhiêu phút.</summary>
    public const string TokenMinutes = "CIRCULATION.SELF_CHECKOUT_QR_TTL_MINUTES";

    /// <summary>Danh sách SSID hợp lệ, ngăn nhau bằng dấu phẩy.</summary>
    public const string WifiSsids = "MOBILE.SELF_CHECKOUT_WIFI_SSID";

    /// <summary>Khoá ký mã QR trạm và phiếu xác thực; trống thì máy chủ tự sinh lần đầu dùng.</summary>
    public const string Secret = "MOBILE.SELF_CHECKOUT_QR_SECRET";

    public const string ModeNone = "NONE";
    public const string ModeWifi = "WIFI_SSID";
    public const string ModeQr = "QR_STATION";

    public static readonly IReadOnlyList<string> Modes = new[] { ModeNone, ModeWifi, ModeQr };
}

/// <summary>Mã lỗi trả về ứng dụng khi xác thực vị trí không đạt, để nó hiện đúng màn hình.</summary>
public static class SelfCheckoutErrorCodes
{
    public const string Disabled = "SELF_CHECKOUT_DISABLED";
    public const string LocationRequired = "LOCATION_REQUIRED";
    public const string LocationInvalid = "LOCATION_INVALID";
    public const string LocationExpired = "LOCATION_EXPIRED";
    public const string WifiMismatch = "WIFI_MISMATCH";
    public const string StationUnknown = "STATION_UNKNOWN";
    public const string StationInactive = "STATION_INACTIVE";
}

// ---------------------------------------------------------------------------
// Ký và kiểm chữ ký
// ---------------------------------------------------------------------------

/// <summary>Chữ ký HMAC cho mã QR trạm và phiếu xác thực. Chỉ máy chủ có khoá.</summary>
public static class SelfCheckoutSigner
{
    public const string QrPrefix = "LCST1";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Nội dung in vào mã QR của một trạm: <c>LCST1|MÃ-TRẠM|chữ-ký</c>.</summary>
    public static string BuildStationQr(string stationCode, string secret)
    {
        var code = stationCode.Trim().ToUpperInvariant();
        return $"{QrPrefix}|{code}|{Sign($"{QrPrefix}|{code}", secret)}";
    }

    /// <summary>Đọc mã trạm từ nội dung QR, trả về null nếu sai định dạng hay sai chữ ký.</summary>
    public static string? ReadStationQr(string? content, string secret)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var parts = content.Trim().Split('|');

        if (parts.Length != 3 || parts[0] != QrPrefix)
        {
            return null;
        }

        var code = parts[1].Trim().ToUpperInvariant();
        var expected = Sign($"{QrPrefix}|{code}", secret);

        return FixedTimeEquals(expected, parts[2]) ? code : null;
    }

    public record TokenPayload(Guid ReaderId, string Mode, string? Place, long ExpiresUnix);

    /// <summary>Phiếu xác thực: JSON base64url + chữ ký, chỉ có giá trị cho đúng bạn đọc ấy.</summary>
    public static string IssueToken(TokenPayload payload, string secret)
    {
        var body = Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload, Json));
        return $"{body}.{Sign(body, secret)}";
    }

    public static TokenPayload? ReadToken(string? token, string secret)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Trim().Split('.');

        if (parts.Length != 2 || !FixedTimeEquals(Sign(parts[0], secret), parts[1]))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TokenPayload>(FromBase64Url(parts[0]), Json);
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            return null;
        }
    }

    public static string NewSecret() => Base64Url(RandomNumberGenerator.GetBytes(32));

    private static string Sign(string text, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(text)));
    }

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string text)
    {
        var padded = text.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}

/// <summary>Lấy khoá ký; chưa có thì sinh một lần và ghi vào tham số hệ thống.</summary>
public static class SelfCheckoutSecret
{
    public static async Task<string> GetOrCreateAsync(ISystemParameterService parameters, CancellationToken ct)
    {
        var secret = await parameters.GetAsync(SelfCheckoutParameters.Secret, string.Empty, ct);

        if (!string.IsNullOrWhiteSpace(secret))
        {
            return secret;
        }

        secret = SelfCheckoutSigner.NewSecret();
        await parameters.SetAsync(SelfCheckoutParameters.Secret, secret, ct);
        return secret;
    }
}

// ---------------------------------------------------------------------------
// Trạm mượn (quản trị)
// ---------------------------------------------------------------------------

public record CheckoutStationDto(
    Guid Id,
    string Code,
    string Name,
    Guid? WarehouseId,
    string? WarehouseName,
    string? Location,
    bool IsActive,
    string QrContent);

public record GetCheckoutStationsQuery(bool IncludeInactive) : IRequest<IReadOnlyList<CheckoutStationDto>>;

public class GetCheckoutStationsQueryHandler
    : IRequestHandler<GetCheckoutStationsQuery, IReadOnlyList<CheckoutStationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;

    public GetCheckoutStationsQueryHandler(IApplicationDbContext db, ISystemParameterService parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    public async Task<IReadOnlyList<CheckoutStationDto>> Handle(GetCheckoutStationsQuery query, CancellationToken ct)
    {
        var secret = await SelfCheckoutSecret.GetOrCreateAsync(_parameters, ct);

        var rows = await _db.CheckoutStations.AsNoTracking()
            .Where(station => query.IncludeInactive || station.IsActive)
            .OrderBy(station => station.Code)
            .Select(station => new
            {
                station.Id,
                station.Code,
                station.Name,
                station.WarehouseId,
                WarehouseName = _db.Warehouses
                    .Where(warehouse => warehouse.Id == station.WarehouseId)
                    .Select(warehouse => warehouse.Name)
                    .FirstOrDefault(),
                station.Location,
                station.IsActive
            })
            .ToListAsync(ct);

        return rows
            .Select(row => new CheckoutStationDto(
                row.Id, row.Code, row.Name, row.WarehouseId, row.WarehouseName, row.Location, row.IsActive,
                SelfCheckoutSigner.BuildStationQr(row.Code, secret)))
            .ToList();
    }
}

public class SaveCheckoutStationCommand : IRequest<CheckoutStationDto>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveCheckoutStationCommandValidator : AbstractValidator<SaveCheckoutStationCommand>
{
    public SaveCheckoutStationCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("Chưa nhập mã trạm.")
            .MaximumLength(50).WithMessage("Mã trạm tối đa 50 ký tự.")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Mã trạm chỉ gồm chữ, số, gạch ngang và gạch dưới.");
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên trạm.")
            .MaximumLength(200).WithMessage("Tên trạm tối đa 200 ký tự.");
        RuleFor(command => command.Location).MaximumLength(500);
    }
}

public class SaveCheckoutStationCommandHandler : IRequestHandler<SaveCheckoutStationCommand, CheckoutStationDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISender _mediator;

    public SaveCheckoutStationCommandHandler(IApplicationDbContext db, ISender mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<CheckoutStationDto> Handle(SaveCheckoutStationCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim().ToUpperInvariant();

        var duplicate = await _db.CheckoutStations
            .AnyAsync(station => station.Code == code && station.Id != command.Id, ct);

        if (duplicate)
        {
            throw new ConflictException($"Mã trạm {code} đã có.");
        }

        CheckoutStation station;

        if (command.Id is null)
        {
            station = new CheckoutStation { Code = code };
            _db.CheckoutStations.Add(station);
        }
        else
        {
            station = await _db.CheckoutStations.FirstOrDefaultAsync(row => row.Id == command.Id, ct)
                      ?? throw new NotFoundException("trạm mượn", command.Id);
            station.Code = code;
        }

        station.Name = command.Name.Trim();
        station.WarehouseId = command.WarehouseId;
        station.Location = command.Location?.Trim();
        station.IsActive = command.IsActive;

        await _db.SaveChangesAsync(ct);

        var all = await _mediator.Send(new GetCheckoutStationsQuery(true), ct);
        return all.Single(row => row.Id == station.Id);
    }
}

public record DeleteCheckoutStationCommand(Guid Id) : IRequest;

public class DeleteCheckoutStationCommandHandler : IRequestHandler<DeleteCheckoutStationCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCheckoutStationCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCheckoutStationCommand command, CancellationToken ct)
    {
        var station = await _db.CheckoutStations.FirstOrDefaultAsync(row => row.Id == command.Id, ct)
                      ?? throw new NotFoundException("trạm mượn", command.Id);

        _db.CheckoutStations.Remove(station);
        await _db.SaveChangesAsync(ct);
    }
}

// ---------------------------------------------------------------------------
// Bạn đọc xác thực vị trí, nhận phiếu
// ---------------------------------------------------------------------------

/// <summary>Ứng dụng gửi lên thứ nó thấy; máy chủ chọn cách kiểm theo tham số, không theo ứng dụng.</summary>
public class VerifySelfCheckoutLocationCommand : IRequest<SelfCheckoutVerificationDto>
{
    /// <summary>Tên Wi-Fi thiết bị đang nối (chế độ WIFI_SSID).</summary>
    public string? Ssid { get; set; }

    /// <summary>Nội dung mã QR vừa quét (chế độ QR_STATION).</summary>
    public string? QrContent { get; set; }
}

public record SelfCheckoutVerificationDto(
    string Mode,
    string VerificationToken,
    DateTimeOffset ExpiresAt,
    string? StationCode,
    string? StationName,
    string? WarehouseName);

public class VerifySelfCheckoutLocationCommandHandler
    : IRequestHandler<VerifySelfCheckoutLocationCommand, SelfCheckoutVerificationDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public VerifySelfCheckoutLocationCommandHandler(
        IApplicationDbContext db,
        ISystemParameterService parameters,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _parameters = parameters;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<SelfCheckoutVerificationDto> Handle(
        VerifySelfCheckoutLocationCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        if (!await _parameters.GetAsync(SelfCheckoutCommandHandler.EnabledParameter, false, ct))
        {
            throw new ConflictException(
                "Thư viện chưa mở chức năng mượn tự phục vụ.", SelfCheckoutErrorCodes.Disabled);
        }

        var mode = (await _parameters.GetAsync(SelfCheckoutParameters.VerifyMode, SelfCheckoutParameters.ModeNone, ct))
            .Trim().ToUpperInvariant();
        var minutes = Math.Max(1, await _parameters.GetAsync(SelfCheckoutParameters.TokenMinutes, 15, ct));
        var secret = await SelfCheckoutSecret.GetOrCreateAsync(_parameters, ct);
        var now = _clock.Now;

        string? place = null;
        string? stationName = null;
        string? warehouseName = null;

        switch (mode)
        {
            case SelfCheckoutParameters.ModeWifi:
            {
                var allowed = (await _parameters.GetAsync(SelfCheckoutParameters.WifiSsids, string.Empty, ct))
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var ssid = command.Ssid?.Trim().Trim('"');

                if (string.IsNullOrWhiteSpace(ssid))
                {
                    throw new ConflictException(
                        "Hãy nối vào Wi-Fi của thư viện rồi thử lại.", SelfCheckoutErrorCodes.LocationRequired);
                }

                if (allowed.Count == 0 || !allowed.Contains(ssid))
                {
                    throw new ConflictException(
                        "Thiết bị không nối vào Wi-Fi của thư viện. Mượn tự phục vụ chỉ dùng được khi đang ở trong thư viện.",
                        SelfCheckoutErrorCodes.WifiMismatch);
                }

                place = ssid;
                break;
            }

            case SelfCheckoutParameters.ModeQr:
            {
                if (string.IsNullOrWhiteSpace(command.QrContent))
                {
                    throw new ConflictException(
                        "Hãy quét mã QR dán tại kho trước khi mượn.", SelfCheckoutErrorCodes.LocationRequired);
                }

                var code = SelfCheckoutSigner.ReadStationQr(command.QrContent, secret)
                           ?? throw new ConflictException(
                               "Mã QR không phải mã trạm mượn của thư viện này.", SelfCheckoutErrorCodes.StationUnknown);

                var station = await _db.CheckoutStations.AsNoTracking()
                    .Where(row => row.Code == code)
                    .Select(row => new
                    {
                        row.Code,
                        row.Name,
                        row.IsActive,
                        WarehouseName = _db.Warehouses
                            .Where(warehouse => warehouse.Id == row.WarehouseId)
                            .Select(warehouse => warehouse.Name)
                            .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync(ct)
                    ?? throw new ConflictException(
                        "Trạm mượn ghi trên mã QR không còn trong hệ thống.", SelfCheckoutErrorCodes.StationUnknown);

                if (!station.IsActive)
                {
                    throw new ConflictException(
                        $"Trạm mượn {station.Name} đang tạm ngừng.", SelfCheckoutErrorCodes.StationInactive);
                }

                place = station.Code;
                stationName = station.Name;
                warehouseName = station.WarehouseName;
                break;
            }

            default:
                mode = SelfCheckoutParameters.ModeNone;
                break;
        }

        var expiresAt = now.AddMinutes(minutes);
        var token = SelfCheckoutSigner.IssueToken(
            new SelfCheckoutSigner.TokenPayload(readerId, mode, place, expiresAt.ToUnixTimeSeconds()), secret);

        return new SelfCheckoutVerificationDto(mode, token, expiresAt, place, stationName, warehouseName);
    }
}

/// <summary>Kiểm phiếu xác thực lúc mượn. Trả về nơi đã xác thực (mã trạm hoặc SSID) để ghi vào phiếu mượn.</summary>
public static class SelfCheckoutVerification
{
    public static async Task<string?> RequireAsync(
        ISystemParameterService parameters,
        string? verificationToken,
        Guid readerId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var mode = (await parameters.GetAsync(SelfCheckoutParameters.VerifyMode, SelfCheckoutParameters.ModeNone, ct))
            .Trim().ToUpperInvariant();

        if (mode == SelfCheckoutParameters.ModeNone)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(verificationToken))
        {
            throw new ConflictException(
                mode == SelfCheckoutParameters.ModeWifi
                    ? "Chưa xác thực vị trí: hãy nối Wi-Fi thư viện và bấm xác thực trước khi mượn."
                    : "Chưa xác thực vị trí: hãy quét mã QR dán tại kho trước khi mượn.",
                SelfCheckoutErrorCodes.LocationRequired);
        }

        var secret = await SelfCheckoutSecret.GetOrCreateAsync(parameters, ct);
        var payload = SelfCheckoutSigner.ReadToken(verificationToken, secret)
                      ?? throw new ConflictException(
                          "Phiếu xác thực vị trí không hợp lệ. Hãy xác thực lại.", SelfCheckoutErrorCodes.LocationInvalid);

        if (payload.ReaderId != readerId || !string.Equals(payload.Mode, mode, StringComparison.Ordinal))
        {
            throw new ConflictException(
                "Phiếu xác thực vị trí không thuộc về bạn hoặc không đúng chế độ. Hãy xác thực lại.",
                SelfCheckoutErrorCodes.LocationInvalid);
        }

        if (payload.ExpiresUnix < now.ToUnixTimeSeconds())
        {
            throw new ConflictException(
                "Phiếu xác thực vị trí đã hết hạn. Hãy quét lại mã tại kho.", SelfCheckoutErrorCodes.LocationExpired);
        }

        return payload.Place;
    }
}
