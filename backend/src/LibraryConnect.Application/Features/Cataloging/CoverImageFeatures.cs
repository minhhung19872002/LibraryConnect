using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

// ---------------------------------------------------------------------------------------------
// Ảnh bìa: ảnh thật nếu tra được, còn lại thì dựng một bìa đọc được từ chính dữ liệu thư mục.
// ---------------------------------------------------------------------------------------------

/// <summary>Ảnh bìa dựng sẵn của một biểu ghi, kèm dấu bản để đặt bộ nhớ đệm.</summary>
public record BibCoverDto(string Svg, string ETag);

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

    public GetBibCoverQueryHandler(IApplicationDbContext db) => _db = db;

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
                row.UpdatedAt,
                row.CreatedAt,
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("biểu ghi", query.BibId);

        var input = new CoverInput(
            bib.Title,
            bib.AuthorMain,
            bib.PublishYear,
            bib.DocumentTypeCode,
            bib.DocumentTypeName);

        var svg = CoverImageBuilder.BuildSvg(input);

        // Dấu bản đổi khi biểu ghi đổi, nên trình duyệt giữ ảnh cũ tới lúc nào biểu ghi được sửa.
        var moc = bib.UpdatedAt ?? bib.CreatedAt;
        var etag = $"\"{query.BibId:N}-{moc.ToUnixTimeSeconds()}\"";

        return new BibCoverDto(svg, etag);
    }
}
