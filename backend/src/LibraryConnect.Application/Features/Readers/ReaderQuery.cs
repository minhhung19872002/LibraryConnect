using System.Linq.Expressions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Readers;

/// <summary>
/// Bộ lọc và phép chiếu dùng chung cho mọi màn hình đọc danh sách bạn đọc: danh sách, xuất Excel,
/// in thẻ hàng loạt và các thao tác hàng loạt.
///
/// Gom về một chỗ vì các màn hình đó phải hiểu bộ lọc giống hệt nhau — cán bộ lọc ra 812 bạn đọc
/// khóa K45 rồi bấm "In thẻ" thì phải đúng 812 thẻ đó được in, không hơn không kém.
/// </summary>
internal static class ReaderQuery
{
    /// <summary>Khóa tham số khai khoảng cảnh báo thẻ sắp hết hạn.</summary>
    public const string ExpiringDaysParameter = "READER.CARD_EXPIRING_DAYS";

    /// <summary>Dùng khi tham số chưa được khai; vẫn cấu hình lại được từ Tham số hệ thống.</summary>
    public const int DefaultExpiringDays = 30;

    public static IQueryable<Reader> Apply(IApplicationDbContext db, ReaderListRequest request, DateOnly today)
    {
        var readers = db.Readers
            .AsNoTracking()
            .WhereIf(request.ReaderTypeId is not null, reader => reader.ReaderTypeId == request.ReaderTypeId)
            .WhereIf(request.FacultyId is not null, reader => reader.FacultyId == request.FacultyId)
            .WhereIf(request.MajorId is not null, reader => reader.MajorId == request.MajorId)
            .WhereIf(!string.IsNullOrWhiteSpace(request.ClassName), reader => reader.ClassName == request.ClassName)
            .WhereIf(!string.IsNullOrWhiteSpace(request.CourseYear), reader => reader.CourseYear == request.CourseYear)
            .WhereIf(request.Status is not null, reader => reader.Status == request.Status)
            .WhereIf(request.Expired == true, reader => reader.CardExpireDate < today)
            .WhereIf(request.Expired == false, reader => reader.CardExpireDate >= today)
            .WhereIf(request.CreatedFrom is not null,
                reader => reader.CreatedAt >= request.CreatedFrom!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(request.CreatedTo is not null,
                reader => reader.CreatedAt < request.CreatedTo!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

        if (request.ExpiringInDays is > 0)
        {
            var limit = today.AddDays(request.ExpiringInDays.Value);
            readers = readers.Where(reader => reader.CardExpireDate >= today && reader.CardExpireDate <= limit);
        }

        // Công nợ và số sách đang giữ đọc thẳng từ sổ mượn và sổ phạt chứ không tin vào cột đếm sẵn
        // trên hồ sơ: cột đếm là bộ nhớ đệm của phân hệ Lưu thông, còn quyết định chặn ra trường thì
        // phải dựa trên chứng cứ.
        if (request.HasDebt == true)
        {
            readers = readers.Where(reader =>
                db.Fines.Any(fine => fine.ReaderId == reader.Id && !fine.Waived && fine.Amount > fine.PaidAmount));
        }
        else if (request.HasDebt == false)
        {
            readers = readers.Where(reader =>
                !db.Fines.Any(fine => fine.ReaderId == reader.Id && !fine.Waived && fine.Amount > fine.PaidAmount));
        }

        if (request.Borrowing == true)
        {
            readers = readers.Where(reader => db.Loans.Any(loan =>
                loan.ReaderId == reader.Id
                && (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue)));
        }

        if (request.NeverBorrowed == true)
        {
            readers = readers.Where(reader => !db.Loans.Any(loan => loan.ReaderId == reader.Id));
        }

        if (request.HasKeyword())
        {
            var raw = request.Keyword!.Trim().ToLowerInvariant();
            var keyword = VietnameseText.RemoveDiacritics(raw);

            readers = readers.Where(reader =>
                DatabaseFunctions.Unaccent(reader.FullName).Contains(keyword)
                || reader.CardNumber.ToLower().Contains(raw)
                || (reader.StudentCode != null && reader.StudentCode.ToLower().Contains(raw))
                || (reader.IdCardNumber != null && reader.IdCardNumber.Contains(raw))
                || (reader.Email != null && reader.Email.ToLower().Contains(raw))
                || (reader.Phone != null && reader.Phone.Contains(raw)));
        }

        return readers;
    }

    /// <summary>Cột sắp xếp cho phép, khớp với tiêu đề bảng trên màn hình.</summary>
    public static readonly IReadOnlyDictionary<string, Expression<Func<Reader, object?>>> Sorts =
        new Dictionary<string, Expression<Func<Reader, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["cardNumber"] = reader => reader.CardNumber,
            ["studentCode"] = reader => reader.StudentCode,
            ["fullName"] = reader => reader.FullName,
            ["className"] = reader => reader.ClassName,
            ["courseYear"] = reader => reader.CourseYear,
            ["cardExpireDate"] = reader => reader.CardExpireDate,
            ["status"] = reader => reader.Status,
            ["createdAt"] = reader => reader.CreatedAt
        };

    public static Expression<Func<Reader, ReaderDto>> Projection(
        IApplicationDbContext db, DateOnly today, int expiringDays = DefaultExpiringDays) =>
        reader => new ReaderDto
        {
            Id = reader.Id,
            CardNumber = reader.CardNumber,
            StudentCode = reader.StudentCode,
            FullName = reader.FullName,
            Gender = reader.Gender,
            DateOfBirth = reader.DateOfBirth,
            Email = reader.Email,
            Phone = reader.Phone,
            PhotoUrl = reader.PhotoUrl,
            ReaderTypeId = reader.ReaderTypeId,
            ReaderTypeName = reader.ReaderType!.Name,
            FacultyId = reader.FacultyId,
            FacultyName = reader.Faculty!.Name,
            MajorId = reader.MajorId,
            MajorName = reader.Major!.Name,
            ClassName = reader.ClassName,
            CourseYear = reader.CourseYear,
            CardIssueDate = reader.CardIssueDate,
            CardExpireDate = reader.CardExpireDate,
            Status = reader.Status,
            StatusReason = reader.StatusReason,
            DepositAmount = reader.DepositAmount,
            DebtAmount = db.Fines
                .Where(fine => fine.ReaderId == reader.Id && !fine.Waived)
                .Sum(fine => (decimal?)(fine.Amount - fine.PaidAmount)) ?? 0,
            CurrentLoanCount = db.Loans.Count(loan =>
                loan.ReaderId == reader.Id
                && (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue)),
            TotalLoanCount = db.Loans.Count(loan => loan.ReaderId == reader.Id),
            IsExpired = reader.CardExpireDate < today,
            IsExpiringSoon = reader.CardExpireDate >= today
                             && reader.CardExpireDate <= today.AddDays(expiringDays),
            CanBorrow = reader.Status == ReaderStatus.Active
                        && reader.CardExpireDate >= today
                        && reader.LockedUntil == null
        };
}
