using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Application.Features.Cataloging;

// ---------------------------------------------------------------------------------------------
// Ảnh bìa: ảnh thật nếu tra được, còn lại thì dựng một bìa đọc được từ chính dữ liệu thư mục.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Ảnh bìa của một biểu ghi, kèm dấu bản để đặt bộ nhớ đệm.
///
/// Một địa chỉ duy nhất cho mọi biểu ghi: có ảnh thật thì trả ảnh thật, chưa có thì trả bìa dựng
/// sẵn. Phía gọi không phải phân biệt, nên giao diện không có chỗ nào rẽ nhánh theo "biểu ghi này
/// đã có ảnh chưa" — chính chỗ rẽ nhánh ấy là nơi lỗi hay nấp.
/// </summary>
public record BibCoverDto(byte[] Content, string ContentType, string ETag);

/// <summary>
/// Dựng ảnh bìa thay thế cho một biểu ghi.
///
/// Dựng ở máy chủ chứ không ở trình duyệt: mỗi trang kết quả tra cứu có hai chục ô bìa, dựng lại
/// bằng JavaScript mỗi lần tải trang là hai chục lần tính toán thừa trên máy bạn đọc, và ảnh ấy
/// không đặt được bộ nhớ đệm nên lần nào vào cũng dựng lại từ đầu.
/// </summary>
public record GetBibCoverQuery(Guid BibId) : IRequest<BibCoverDto>;

public class GetBibCoverQueryHandler : IRequestHandler<GetBibCoverQuery, BibCoverDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ISystemParameterService _parameters;
    private readonly ILogger<GetBibCoverQueryHandler> _logger;

    public GetBibCoverQueryHandler(
        IApplicationDbContext db,
        IFileStorage storage,
        ISystemParameterService parameters,
        ILogger<GetBibCoverQueryHandler> logger)
    {
        _db = db;
        _storage = storage;
        _parameters = parameters;
        _logger = logger;
    }

    public async Task<BibCoverDto> Handle(GetBibCoverQuery query, CancellationToken ct)
    {
        var bib = await _db.BibRecords
            .AsNoTracking()
            .Where(row => row.Id == query.BibId && row.DeletedAt == null)
            .Select(row => new
            {
                row.Title,
                row.AuthorMain,
                row.PublishYear,
                DocumentTypeCode = row.DocumentType!.Code,
                DocumentTypeName = row.DocumentType.Name,
                row.CoverObjectName,
                row.UpdatedAt,
                row.CreatedAt,
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("biểu ghi", query.BibId);

        // Dấu bản đổi khi biểu ghi đổi, nên trình duyệt giữ ảnh cũ tới lúc nào biểu ghi được sửa.
        var moc = bib.UpdatedAt ?? bib.CreatedAt;
        var etag = $"\"{query.BibId:N}-{moc.ToUnixTimeSeconds()}\"";

        // Ảnh thật đã tải về kho đối tượng: lấy thẳng ra đây.
        if (!string.IsNullOrWhiteSpace(bib.CoverObjectName)
            && await DocAnhThatAsync(bib.CoverObjectName, ct) is { } anhThat)
        {
            return anhThat with { ETag = etag };
        }

        var svg = CoverImageBuilder.BuildSvg(new CoverInput(
            bib.Title,
            bib.AuthorMain,
            bib.PublishYear,
            bib.DocumentTypeCode,
            bib.DocumentTypeName));

        return new BibCoverDto(
            System.Text.Encoding.UTF8.GetBytes(svg), "image/svg+xml; charset=utf-8", etag);
    }

    /// <summary>
    /// Đọc ảnh thật ra khỏi kho đối tượng.
    ///
    /// Ảnh mất khỏi kho — bị xoá tay, đổi thùng chứa — thì trả về null để phía trên rơi xuống bìa
    /// dựng sẵn, chứ không để trang tra cứu đầy ô ảnh hỏng. Đó đúng là chuyện đã xảy ra: địa chỉ ghi
    /// trong biểu ghi trỏ tới một endpoint chỉ phục vụ thư mục khác, nên 16 ảnh vừa tải về hiện
    /// thành ô hỏng mà báo cáo vẫn nói "đã có ảnh".
    /// </summary>
    private async Task<BibCoverDto?> DocAnhThatAsync(string coverObjectName, CancellationToken ct)
    {
        var objectName = TenDoiTuong(coverObjectName);

        if (objectName is null)
        {
            return null;
        }

        var bucket = await _parameters.GetAsync("MINIO.IMAGES_BUCKET", "lc-images", ct);

        try
        {
            if (!await _storage.ExistsAsync(bucket, objectName, ct))
            {
                return null;
            }

            await using var source = await _storage.DownloadAsync(bucket, objectName, ct);
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, ct);

            return new BibCoverDto(buffer.ToArray(), KieuAnh(objectName), string.Empty);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Không đọc được ảnh bìa {ObjectName}.", objectName);
            return null;
        }
    }

    /// <summary>
    /// Bóc tên đối tượng ra khỏi địa chỉ ảnh bìa, và chỉ nhận thư mục ảnh bìa.
    ///
    /// Địa chỉ này lưu trong cơ sở dữ liệu; chặn ở đây để một biểu ghi bị sửa cột ấy không đọc được
    /// tệp tài liệu số của kho.
    /// </summary>
    private static string? TenDoiTuong(string coverObjectName)
    {
        var value = coverObjectName.Trim();
        var moc = value.IndexOf("covers/", StringComparison.Ordinal);

        if (moc < 0 || value.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        var ten = value[moc..];

        return ten.Contains('/', StringComparison.Ordinal)
               && ten.LastIndexOf('/') == "covers".Length
            ? ten
            : null;
    }

    private static string KieuAnh(string objectName) =>
        System.IO.Path.GetExtension(objectName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "image/jpeg",
        };
}
