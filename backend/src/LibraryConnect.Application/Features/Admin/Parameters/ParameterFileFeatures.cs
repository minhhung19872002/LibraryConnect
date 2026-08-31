using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Admin.Parameters;

/// <summary>
/// Tệp gắn với một tham số kiểu Tệp — hiện chỉ có logo thư viện, thứ được in lên mọi biểu mẫu.
///
/// Tham số kiểu Tệp lưu tên đối tượng trong kho đối tượng chứ không lưu địa chỉ web bên ngoài: biểu
/// mẫu in ra ở phía máy chủ, mà máy chủ đi tải một địa chỉ do người dùng nhập là một lối tấn công
/// vào mạng nội bộ.
/// </summary>
public static class ParameterFiles
{
    public const string Bucket = "branding";

    /// <summary>Ảnh thôi: đây là thứ được nhúng thẳng vào PDF và trang quản trị.</summary>
    public static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/gif"] = ".gif",
            ["image/webp"] = ".webp"
        };

    public const long MaxSizeBytes = 2 * 1024 * 1024;
}

public record UploadParameterFileCommand(string Key, string FileName, string ContentType, byte[] Content)
    : IRequest<string>;

public class UploadParameterFileCommandHandler : IRequestHandler<UploadParameterFileCommand, string>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ISystemParameterService _parameters;

    public UploadParameterFileCommandHandler(
        IApplicationDbContext db, IFileStorage storage, ISystemParameterService parameters)
    {
        _db = db;
        _storage = storage;
        _parameters = parameters;
    }

    public async Task<string> Handle(UploadParameterFileCommand command, CancellationToken ct)
    {
        var parameter = await _db.SystemParameters
            .FirstOrDefaultAsync(entity => entity.Key == command.Key, ct)
            ?? throw new NotFoundException("tham số", command.Key);

        if (parameter.DataType != ParameterDataType.File)
        {
            throw new Common.Exceptions.ValidationException(
                "key", $"Tham số {parameter.Name} không phải kiểu Tệp nên không tải tệp lên được.");
        }

        if (command.Content.Length == 0)
        {
            throw new Common.Exceptions.ValidationException("file", "Tệp rỗng.");
        }

        if (command.Content.Length > ParameterFiles.MaxSizeBytes)
        {
            throw new Common.Exceptions.ValidationException(
                "file", $"Tệp tối đa {ParameterFiles.MaxSizeBytes / 1024 / 1024} MB.");
        }

        if (!ParameterFiles.AllowedTypes.TryGetValue(command.ContentType, out var extension))
        {
            throw new Common.Exceptions.ValidationException(
                "file", "Chỉ nhận tệp ảnh PNG, JPG, GIF hoặc WEBP.");
        }

        // Tên đối tượng sinh từ khóa tham số chứ không lấy tên tệp người dùng gửi lên: tên tệp là dữ
        // liệu người dùng kiểm soát, còn đây là đường dẫn trong kho.
        var objectName = $"{command.Key.ToLowerInvariant().Replace('.', '-')}{extension}";

        using var stream = new MemoryStream(command.Content);

        await _storage.UploadAsync(ParameterFiles.Bucket, objectName, stream, command.ContentType, ct);
        await _parameters.SetAsync(command.Key, objectName, ct);

        return objectName;
    }
}

/// <summary>Nội dung tệp của một tham số, để hiển thị và để nhúng vào biểu mẫu in.</summary>
public record ParameterFileDto(byte[] Content, string ContentType, string FileName);

public record GetParameterFileQuery(string Key) : IRequest<ParameterFileDto>;

public class GetParameterFileQueryHandler : IRequestHandler<GetParameterFileQuery, ParameterFileDto>
{
    private readonly ISystemParameterService _parameters;
    private readonly IFileStorage _storage;

    public GetParameterFileQueryHandler(ISystemParameterService parameters, IFileStorage storage)
    {
        _parameters = parameters;
        _storage = storage;
    }

    public async Task<ParameterFileDto> Handle(GetParameterFileQuery query, CancellationToken ct)
    {
        var objectName = await _parameters.GetAsync(query.Key, ct);

        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new NotFoundException($"Tham số {query.Key} chưa có tệp nào.");
        }

        var content = await ParameterFileLoader.LoadAsync(_storage, objectName, ct)
            ?? throw new NotFoundException($"Không đọc được tệp của tham số {query.Key}.");

        var extension = Path.GetExtension(objectName).ToLowerInvariant();

        var contentType = ParameterFiles.AllowedTypes
            .FirstOrDefault(pair => pair.Value == extension).Key ?? "application/octet-stream";

        return new ParameterFileDto(content, contentType, objectName);
    }
}

/// <summary>Đọc tệp của một tham số, trả về null khi không có hoặc không đọc được.</summary>
public static class ParameterFileLoader
{
    public static async Task<byte[]?> LoadAsync(
        IFileStorage storage, string? objectName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        try
        {
            await using var stream = await storage.DownloadAsync(ParameterFiles.Bucket, objectName, ct);
            using var buffer = new MemoryStream();

            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }
        catch (Exception)
        {
            // Thiếu logo không được làm hỏng cả tờ biểu mẫu: in không có logo vẫn là tờ giấy dùng được.
            return null;
        }
    }
}
