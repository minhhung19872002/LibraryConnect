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
    public static IQueryable<Loan> Base(IApplicationDbContext db) =>
        db.Loans.AsNoTracking();

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
    public static IQueryable<Hold> Base(IApplicationDbContext db) =>
        db.Holds.AsNoTracking();

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
    public static IQueryable<Fine> Base(IApplicationDbContext db) =>
        db.Fines.AsNoTracking();

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
    public static IQueryable<LibraryVisit> Base(IApplicationDbContext db) =>
        db.LibraryVisits.AsNoTracking();

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
