using System.Linq.Expressions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Cir;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Circulation;

/// <summary>
/// Truy vấn và phép chiếu dùng chung của Phân hệ VII.
///
/// Danh sách mượn trả xuất hiện ở năm chỗ khác nhau — quầy, hồ sơ bạn đọc, báo cáo quá hạn, trang
/// bạn đọc và ứng dụng di động — nên phép chiếu để một chỗ, tránh mỗi màn hình hiện một kiểu.
/// </summary>
internal static class LoanQuery
{
/// <summary>
/// Nền của mọi danh sách lưu thông.
///
/// Phải bỏ bộ lọc toàn cục rồi tự lọc theo <c>DeletedAt</c> của chính dòng ấy. Phép chiếu bên dưới
/// đi qua các điều hướng bắt buộc (bạn đọc, ĐKCB, biểu ghi, kho); bộ lọc xoá mềm của những bảng ấy
/// biến JOIN thành INNER, nên bạn đọc bị xoá hồ sơ là mọi phiếu mượn, khoản phạt, đặt giữ và lượt
/// vào thư viện của họ **biến mất khỏi danh sách** — trong khi <c>CountAsync</c> lược bỏ chính những
/// JOIN ấy nên vẫn đếm đủ. Trên máy chủ thật ngày 07/09/2026: 3.122 phiếu đếm được, 3.116 lấy ra
/// được; 134 đặt giữ đếm được, 128 lấy ra được; kỳ kiểm kê đếm 2 mà lấy ra 0. Đây đúng là bài học
/// 57 (bộ lọc xoá mềm lan từ cha sang con), lần này ở chiều đọc danh sách.
/// </summary>
    public static IQueryable<Loan> Base(IApplicationDbContext db, IDataScopeContext? scope = null)
    {
        var loans = db.Loans.IgnoreQueryFilters().AsNoTracking().Where(loan => loan.DeletedAt == null);

        // Bỏ bộ lọc toàn cục là bỏ luôn phạm vi dữ liệu theo kho, thứ trước đây được cưỡng chế một
        // cách tình cờ vì phép chiếu nối sang ĐKCB đã lọc. Tình cờ thì đếm sai: cán bộ chỉ có "Kho
        // mở" thấy tổng 3.122 phiếu mà chỉ lấy ra được 302 dòng, các trang sau rỗng trơn. Nay lọc
        // thẳng trên phiếu nên bộ đếm và danh sách nói cùng một con số.
        if (scope is { WarehouseRestricted: true })
        {
            var warehouseIds = scope.WarehouseIds.ToList();
            loans = loans.Where(loan => warehouseIds.Contains(loan.Item!.WarehouseId));
        }

        return loans;
    }

    public static readonly Expression<Func<Loan, LoanRowDto>> Projection = loan => new LoanRowDto
    {
        Id = loan.Id,
        Code = loan.Code,
        ReaderId = loan.ReaderId,
        ReaderCardNumber = loan.Reader!.CardNumber,
        ReaderName = loan.Reader!.FullName,
        ReaderTypeName = loan.Reader!.ReaderType!.Name,
        FacultyName = loan.Reader!.Faculty!.Name,
        ClassName = loan.Reader!.ClassName,
        ItemId = loan.ItemId,
        Barcode = loan.Barcode,
        // Nhan đề chép sẵn vào phiếu để danh sách khỏi nối bảng, nhưng phiếu tạo bằng đường khác có
        // thể để trống cột ấy. Không có phương án dự phòng thì bạn đọc mở "Sách đang mượn" chỉ thấy
        // một dấu gạch ngang, không biết mình đang giữ cuốn gì.
        Title = loan.BibTitle ?? loan.Item!.Bib!.Title,
        CallNumber = loan.Item!.CallNumber,
        WarehouseName = loan.Item!.Warehouse!.Name,
        LoanDate = loan.LoanDate,
        DueDate = loan.DueDate,
        ReturnDate = loan.ReturnDate,
        RenewedCount = loan.RenewedCount,
        Status = loan.Status,
        LoanType = loan.LoanType,
        Channel = loan.Channel,
        LoanByName = loan.LoanByName,
        ReturnByName = loan.ReturnByName,
        FineAmount = loan.FineAmount,
        Note = loan.Note
    };
}

internal static class HoldQuery
{
    /// <summary>Xem chú thích ở <see cref="LoanQuery.Base"/>.</summary>
    public static IQueryable<Hold> Base(IApplicationDbContext db) =>
        db.Holds.IgnoreQueryFilters().AsNoTracking().Where(hold => hold.DeletedAt == null);

    public static readonly Expression<Func<Hold, HoldRowDto>> Projection = hold => new HoldRowDto
    {
        Id = hold.Id,
        ReaderId = hold.ReaderId,
        ReaderCardNumber = hold.Reader!.CardNumber,
        ReaderName = hold.Reader!.FullName,
        BibId = hold.BibId,
        Title = hold.Bib!.Title,
        ItemId = hold.ItemId,
        Barcode = hold.Item!.Barcode,
        HoldDate = hold.HoldDate,
        ExpireDate = hold.ExpireDate,
        PickupWarehouseId = hold.PickupWarehouseId,
        PickupWarehouseName = hold.PickupWarehouse!.Name,
        Status = hold.Status,
        QueuePosition = hold.QueuePosition,
        NotifiedAt = hold.NotifiedAt,
        Channel = hold.Channel,
        CancelReason = hold.CancelReason
    };
}

internal static class FineQuery
{
    /// <summary>Xem chú thích ở <see cref="LoanQuery.Base"/>.</summary>
    public static IQueryable<Fine> Base(IApplicationDbContext db) =>
        db.Fines.IgnoreQueryFilters().AsNoTracking().Where(fine => fine.DeletedAt == null);

    public static readonly Expression<Func<Fine, FineRowDto>> Projection = fine => new FineRowDto
    {
        Id = fine.Id,
        Code = fine.Code,
        ReaderId = fine.ReaderId,
        ReaderCardNumber = fine.Reader!.CardNumber,
        ReaderName = fine.Reader!.FullName,
        LoanId = fine.LoanId,
        LoanCode = fine.Loan!.Code,
        Title = fine.Loan!.BibTitle ?? fine.Loan!.Item!.Bib!.Title,
        Barcode = fine.Loan!.Barcode,
        Type = fine.Type,
        Amount = fine.Amount,
        PaidAmount = fine.PaidAmount,
        Outstanding = fine.Waived ? 0 : fine.Amount - fine.PaidAmount,
        Waived = fine.Waived,
        WaiveReason = fine.WaiveReason,
        PaidAt = fine.PaidAt,
        PaidByName = fine.PaidByName,
        CreatedAt = fine.CreatedAt,
        Note = fine.Note
    };
}

internal static class VisitQuery
{
    /// <summary>Xem chú thích ở <see cref="LoanQuery.Base"/>.</summary>
    public static IQueryable<LibraryVisit> Base(IApplicationDbContext db) =>
        db.LibraryVisits.IgnoreQueryFilters().AsNoTracking().Where(visit => visit.DeletedAt == null);

    public static readonly Expression<Func<LibraryVisit, VisitRowDto>> Projection =
        visit => new VisitRowDto
        {
            Id = visit.Id,
            ReaderId = visit.ReaderId,
            ReaderCardNumber = visit.Reader!.CardNumber,
            ReaderName = visit.Reader!.FullName,
            ReaderTypeName = visit.Reader!.ReaderType!.Name,
            FacultyName = visit.Reader!.Faculty!.Name,
            LibraryId = visit.LibraryId,
            CheckinAt = visit.CheckinAt,
            CheckoutAt = visit.CheckoutAt,
            Gate = visit.Gate,
            Purpose = visit.Purpose
        };
}

/// <summary>Nhãn tiếng Việt của các giá trị enum trong phân hệ, dùng khi xuất báo cáo.</summary>
public static class CirculationLabels
{
    public static string LoanStatus(LoanStatus status) => status switch
    {
        Domain.Enums.LoanStatus.Active => "Đang mượn",
        Domain.Enums.LoanStatus.Returned => "Đã trả",
        Domain.Enums.LoanStatus.Overdue => "Quá hạn",
        Domain.Enums.LoanStatus.Lost => "Mất",
        Domain.Enums.LoanStatus.Damaged => "Hỏng",
        _ => status.ToString()
    };

    public static string LoanType(LoanType type) => type switch
    {
        Domain.Enums.LoanType.InHouse => "Đọc tại chỗ",
        Domain.Enums.LoanType.TakeHome => "Mượn về nhà",
        Domain.Enums.LoanType.SelfCheckout => "Tự phục vụ",
        _ => type.ToString()
    };

    public static string Channel(LoanChannel channel) => channel switch
    {
        LoanChannel.Desk => "Quầy",
        LoanChannel.Opac => "Trang tra cứu",
        LoanChannel.Mobile => "Ứng dụng di động",
        _ => channel.ToString()
    };

    public static string HoldStatus(HoldStatus status) => status switch
    {
        Domain.Enums.HoldStatus.Waiting => "Đang chờ",
        Domain.Enums.HoldStatus.Ready => "Sẵn sàng nhận",
        Domain.Enums.HoldStatus.Fulfilled => "Đã nhận",
        Domain.Enums.HoldStatus.Expired => "Hết hạn giữ",
        Domain.Enums.HoldStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    public static string FineType(FineType type) => type switch
    {
        Domain.Enums.FineType.Overdue => "Quá hạn",
        Domain.Enums.FineType.Lost => "Làm mất",
        Domain.Enums.FineType.Damaged => "Làm hỏng",
        Domain.Enums.FineType.Other => "Khác",
        _ => type.ToString()
    };

    public static string LockerStatus(LockerStatus status) => status switch
    {
        Domain.Enums.LockerStatus.Free => "Trống",
        Domain.Enums.LockerStatus.InUse => "Đang dùng",
        Domain.Enums.LockerStatus.Broken => "Hỏng",
        Domain.Enums.LockerStatus.Locked => "Khóa",
        _ => status.ToString()
    };
}
