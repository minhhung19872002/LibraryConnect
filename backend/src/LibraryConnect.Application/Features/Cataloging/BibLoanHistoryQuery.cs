using System.Linq.Expressions;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Cir;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Một lượt mượn của một bản thuộc biểu ghi — tab "Lịch sử lưu thông" (II.3).</summary>
public class BibLoanDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public string? Barcode { get; set; }
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string ReaderCardNumber { get; set; } = string.Empty;
    public DateTimeOffset LoanDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTimeOffset? ReturnDate { get; set; }
    public int RenewedCount { get; set; }
    public LoanStatus Status { get; set; }
    public LoanType LoanType { get; set; }
}

/// <summary>Bộ lọc của tab "Lịch sử lưu thông": phân trang, sắp xếp và một ô tìm chung.</summary>
public class BibLoanListRequest : PagedRequest
{
    /// <summary>Chỉ những lượt chưa trả — trả lời ngay câu "cuốn này hiện ai đang giữ".</summary>
    public bool? OpenOnly { get; set; }
}

/// <summary>
/// Lịch sử lưu thông của mọi bản thuộc một biểu ghi, mới nhất trước (II.3, tab thứ tư).
///
/// Phân trang phía máy chủ như mọi danh sách khác: một nhan đề giáo trình có hàng chục bản in và
/// sống nhiều năm, nên số lượt mượn của nó lớn hơn hẳn số bản in — trả hết về trình duyệt là đúng
/// kiểu sai mà mục 6.3 cấm.
/// </summary>
public record GetBibLoansQuery(Guid BibId, BibLoanListRequest Request)
    : IRequest<PagedResult<BibLoanDto>>;

public class GetBibLoansQueryHandler : IRequestHandler<GetBibLoansQuery, PagedResult<BibLoanDto>>
{
    /// <summary>Các cột được phép sắp xếp; tên khác rơi về ngày mượn giảm dần.</summary>
    private static readonly IReadOnlyDictionary<string, Expression<Func<Loan, object?>>> Sorts =
        new Dictionary<string, Expression<Func<Loan, object?>>>
        {
            ["code"] = loan => loan.Code,
            ["barcode"] = loan => loan.Barcode,
            ["readerName"] = loan => loan.Reader!.FullName,
            ["loanDate"] = loan => loan.LoanDate,
            ["dueDate"] = loan => loan.DueDate,
            ["returnDate"] = loan => loan.ReturnDate,
            ["status"] = loan => loan.Status
        };

    private readonly IApplicationDbContext _db;

    public GetBibLoansQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<BibLoanDto>> Handle(GetBibLoansQuery query, CancellationToken ct)
    {
        var exists = await _db.BibRecords.AsNoTracking().AnyAsync(bib => bib.Id == query.BibId, ct);

        if (!exists)
        {
            throw new NotFoundException("Không tìm thấy biểu ghi.");
        }

        var request = query.Request;

        // Lọc theo BibId chép sẵn trên phiếu mượn chứ không theo biểu ghi hiện tại của bản in: cột
        // ấy được chép lúc ghi mượn đúng để lịch sử còn nguyên sau khi bản in được biên mục lại
        // sang biểu ghi khác. Đi vòng qua bảng ấn phẩm là gán lịch sử cũ cho biểu ghi mới.
        var loans = _db.Loans
            .AsNoTracking()
            .Where(loan => loan.BibId == query.BibId)
            .WhereIf(request.OpenOnly == true, loan => loan.ReturnDate == null);

        if (request.HasKeyword())
        {
            var keyword = request.Keyword!.Trim().ToLowerInvariant();
            var unaccented = VietnameseText.RemoveDiacritics(keyword);

            loans = loans.Where(loan =>
                loan.Code.ToLower().Contains(keyword)
                || (loan.Barcode != null && loan.Barcode.ToLower().Contains(keyword))
                || loan.Reader!.CardNumber.ToLower().Contains(keyword)
                || DatabaseFunctions.Unaccent(loan.Reader!.FullName).Contains(unaccented));
        }

        return await loans
            .ApplySort(request, Sorts, loan => loan.LoanDate, defaultDescending: true)
            .Select(loan => new BibLoanDto
            {
                Id = loan.Id,
                Code = loan.Code,
                ItemId = loan.ItemId,
                Barcode = loan.Barcode ?? (loan.Item != null ? loan.Item.Barcode : null),
                ReaderId = loan.ReaderId,
                ReaderName = loan.Reader != null ? loan.Reader.FullName : string.Empty,
                ReaderCardNumber = loan.Reader != null ? loan.Reader.CardNumber : string.Empty,
                LoanDate = loan.LoanDate,
                DueDate = loan.DueDate,
                ReturnDate = loan.ReturnDate,
                RenewedCount = loan.RenewedCount,
                Status = loan.Status,
                LoanType = loan.LoanType
            })
            .ToPagedResultAsync(request, ct);
    }
}
