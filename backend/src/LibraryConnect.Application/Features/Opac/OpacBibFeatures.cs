using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Opac;

// ---------------------------------------------------------------------------------------------
// IX.2 — Chi tiết tài liệu trên trang tra cứu: mô tả thư mục, danh sách bản in kèm vị trí và tình
// trạng, tài liệu số đính kèm, nhận xét đã duyệt và tài liệu liên quan.
// ---------------------------------------------------------------------------------------------

public record GetOpacBibQuery(Guid Id) : IRequest<OpacBibDetailDto>;

public class GetOpacBibQueryHandler : IRequestHandler<GetOpacBibQuery, OpacBibDetailDto>
{
    /// <summary>Số tài liệu liên quan gợi ý ở cuối trang.</summary>
    private const int RelatedCount = 6;

    private readonly IApplicationDbContext _db;

    public GetOpacBibQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<OpacBibDetailDto> Handle(GetOpacBibQuery query, CancellationToken ct)
    {
        var bib = await _db.BibRecords.AsNoTracking()
                      .Include(record => record.Authors).ThenInclude(link => link.Author)
                      .Include(record => record.Subjects).ThenInclude(link => link.Subject)
                      .Include(record => record.Keywords).ThenInclude(link => link.Keyword)
                      .Include(record => record.Classifications).ThenInclude(link => link.Classification)
                      .Include(record => record.DocumentType)
                      .Include(record => record.Language)
                      .Include(record => record.Series)
                      .FirstOrDefaultAsync(record => record.Id == query.Id, ct)
                  ?? throw new NotFoundException("Không tìm thấy tài liệu.");

        if (bib.Status != RecordStatus.Published)
        {
            // Không nói "biểu ghi này chưa xuất bản" — bạn đọc không cần biết kho có gì đang biên
            // mục dở, và nói ra là lộ cả tiến độ nội bộ.
            throw new NotFoundException("Không tìm thấy tài liệu.");
        }

        var marc = MarcJson.Deserialize(bib.MarcData);

        var items = await _db.Items.AsNoTracking()
            .Where(item => item.BibId == bib.Id)
            .Include(item => item.Warehouse).ThenInclude(warehouse => warehouse!.Library)
            .Include(item => item.Shelf)
            .OrderBy(item => item.Warehouse!.Name)
            .ThenBy(item => item.RegisterNumber)
            .ToListAsync(ct);

        // Hạn trả của bản đang có người mượn: bạn đọc nhìn vào đó để quyết định chờ hay đặt giữ.
        var itemIds = items.Select(item => item.Id).ToList();

        var dueDates = await _db.Loans.AsNoTracking()
            .Where(loan => itemIds.Contains(loan.ItemId) && loan.ReturnDate == null)
            .Select(loan => new { loan.ItemId, loan.DueDate })
            .ToListAsync(ct);

        var digital = await _db.DigitalDocuments.AsNoTracking()
            .Where(document => document.BibId == bib.Id
                               && document.AccessLevel != DigitalAccessLevel.Forbidden)
            .OrderBy(document => document.Title)
            .Select(document => new OpacDigitalDocumentDto(
                document.Id,
                document.Title,
                document.FileName,
                document.MimeType,
                document.FileSize,
                document.PageCount,
                OpacLabels.Describe(document.AccessLevel),
                document.AccessLevel == DigitalAccessLevel.Restricted,
                document.AllowDownload))
            .ToListAsync(ct);

        var reviews = await _db.OpacReviews.AsNoTracking()
            .Where(review => review.BibId == bib.Id && review.IsApproved)
            .OrderByDescending(review => review.CreatedAt)
            .Select(review => new OpacReviewDto(
                review.Id,
                review.Reader!.FullName,
                review.Rating,
                review.Comment,
                review.CreatedAt))
            .ToListAsync(ct);

        var related = await RelatedAsync(bib.Id, ct);

        return new OpacBibDetailDto(
            bib.Id,
            bib.ControlNumber,
            bib.Title,
            bib.Subtitle,
            bib.StatementOfResponsibility,
            bib.AuthorMain,
            bib.Authors
                .OrderByDescending(link => link.IsMain)
                .ThenBy(link => link.SortOrder)
                .Select(link => new OpacLinkedTermDto(link.AuthorId, link.Author!.Name, link.Role))
                .ToList(),
            bib.Subjects
                .Select(link => new OpacLinkedTermDto(link.SubjectId, link.Subject!.Name, null))
                .ToList(),
            bib.Keywords
                .Select(link => new OpacLinkedTermDto(link.KeywordId, link.Keyword!.Name, null))
                .ToList(),
            bib.Classifications
                .Select(link => new OpacLinkedTermDto(
                    link.ClassificationId,
                    $"{link.Classification!.Code} {link.Classification.Name}".Trim(),
                    link.Scheme))
                .ToList(),
            bib.PublisherName,
            bib.PublishPlace,
            bib.PublishYear,
            bib.Edition,
            bib.Pages,
            bib.Dimensions,
            bib.Isbn,
            bib.Issn,
            bib.Ddc,
            bib.Series?.Name,
            bib.Language?.Name,
            bib.DocumentType?.Name,
            bib.Abstract,
            bib.CoverImageUrl,
            IsbdFormatter.DescribeAsParagraph(marc),
            bib.MarcData,
            bib.ItemCount,
            bib.AvailableItemCount,
            items.Select(item => new OpacItemDto(
                    item.Id,
                    item.Barcode,
                    item.RegisterNumber,
                    item.CallNumber,
                    item.Warehouse?.Library?.Name ?? string.Empty,
                    item.Warehouse?.Name ?? string.Empty,
                    item.Shelf?.Name,
                    OpacLabels.Describe(item.Status),
                    item.IsAvailable,
                    dueDates.FirstOrDefault(loan => loan.ItemId == item.Id)?.DueDate))
                .ToList(),
            digital,
            reviews,
            reviews.Count == 0 ? null : Math.Round(reviews.Average(review => review.Rating), 1),
            related);
    }

    /// <summary>
    /// Tài liệu liên quan: cùng chủ đề trước, không đủ thì lấy thêm cùng nhánh phân loại.
    ///
    /// Chỉ lấy tài liệu còn bản in hoặc có bản số — gợi ý một cuốn mà thư viện không phục vụ được
    /// thì chỉ làm bạn đọc mất công.
    /// </summary>
    private async Task<IReadOnlyList<OpacResultDto>> RelatedAsync(Guid bibId, CancellationToken ct)
    {
        var subjectIds = await _db.BibSubjects.AsNoTracking()
            .Where(link => link.BibId == bibId)
            .Select(link => link.SubjectId)
            .ToListAsync(ct);

        var candidates = OpacQueryBuilder.Published(_db.BibRecords.AsNoTracking())
            .Where(bib => bib.Id != bibId
                          && (bib.ItemCount > 0 || bib.DigitalDocumentCount > 0));

        var related = new List<OpacResultDto>();

        if (subjectIds.Count > 0)
        {
            related = await candidates
                .Where(bib => bib.Subjects.Any(link => subjectIds.Contains(link.SubjectId)))
                .OrderByDescending(bib => bib.LoanCount)
                .Take(RelatedCount)
                .Select(OpacQueryBuilder.ToResult())
                .ToListAsync(ct);
        }

        if (related.Count >= RelatedCount)
        {
            return related;
        }

        var ddc = await _db.BibRecords.AsNoTracking()
            .Where(bib => bib.Id == bibId)
            .Select(bib => bib.Ddc)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(ddc))
        {
            return related;
        }

        var prefix = ddc.Length >= 3 ? ddc[..3] : ddc;
        var taken = related.Select(row => row.Id).ToList();

        var byClass = await candidates
            .Where(bib => !taken.Contains(bib.Id) && bib.Ddc != null && bib.Ddc.StartsWith(prefix))
            .OrderByDescending(bib => bib.LoanCount)
            .Take(RelatedCount - related.Count)
            .Select(OpacQueryBuilder.ToResult())
            .ToListAsync(ct);

        return related.Concat(byClass).ToList();
    }
}

/// <summary>
/// Đếm lượt xem một tài liệu.
///
/// Tách khỏi truy vấn chi tiết vì đó là một phép ghi: để chung thì mỗi lần mở trang là một lần ghi
/// vào cơ sở dữ liệu ngay trên đường trả kết quả, và trang chi tiết là trang được mở nhiều nhất.
/// </summary>
public record RecordOpacBibViewCommand(Guid Id) : IRequest;

public class RecordOpacBibViewCommandHandler : IRequestHandler<RecordOpacBibViewCommand>
{
    private readonly IApplicationDbContext _db;

    public RecordOpacBibViewCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(RecordOpacBibViewCommand command, CancellationToken ct)
    {
        await _db.BibRecords
            .Where(bib => bib.Id == command.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(bib => bib.ViewCount, bib => bib.ViewCount + 1), ct);
    }
}
