using System.Globalization;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Acquisition;

// ---------------------------------------------------------------------------------------------
// III.1 — Nhập danh sách đề nghị mua từ Excel.
// ---------------------------------------------------------------------------------------------

/// <summary>Các cột của tệp mẫu đề nghị mua.</summary>
internal static class PurchaseExcelColumns
{
    public const string Title = "Nhan đề";
    public const string Author = "Tác giả";
    public const string Publisher = "Nhà xuất bản";
    public const string Year = "Năm xuất bản";
    public const string Isbn = "ISBN";
    public const string Issn = "ISSN";
    public const string Quantity = "Số lượng";
    public const string UnitPrice = "Đơn giá";
    public const string Supplier = "Nhà cung cấp";
    public const string Note = "Ghi chú";

    public static readonly IReadOnlyList<ExcelTemplateColumn> Template = new List<ExcelTemplateColumn>
    {
        new(Title, "Tên tài liệu đề nghị mua.", Required: true, Example: "Giáo trình cơ sở dữ liệu"),
        new(Author, "Tác giả chính.", Example: "Nguyễn Văn A"),
        new(Publisher, "Nhà xuất bản.", Example: "NXB Giáo dục"),
        new(Year, "Năm xuất bản, chỉ nhập số.", Example: "2024"),
        new(Isbn, "ISBN, có hay không có dấu gạch đều được.", Example: "978-604-01-2345-6"),
        new(Issn, "ISSN, dùng cho báo và tạp chí.", Example: "1234-5678"),
        new(Quantity, "Số bản đề nghị mua. Bỏ trống thì hiểu là 1.", Example: "5"),
        new(UnitPrice, "Đơn giá dự kiến một bản, đơn vị đồng.", Example: "120000"),
        new(Supplier, "Tên nhà cung cấp gợi ý; phải trùng tên trong danh mục nhà cung cấp.",
            Example: "Công ty Sách Việt"),
        new(Note, "Ghi chú cho dòng đề nghị.")
    };
}

/// <summary>Tệp Excel mẫu để nhập danh sách đề nghị mua.</summary>
public record GetPurchaseRequestTemplateQuery : IRequest<PrintedFileDto>;

public class GetPurchaseRequestTemplateQueryHandler
    : IRequestHandler<GetPurchaseRequestTemplateQuery, PrintedFileDto>
{
    private readonly IExcelService _excel;

    public GetPurchaseRequestTemplateQueryHandler(IExcelService excel) => _excel = excel;

    public Task<PrintedFileDto> Handle(GetPurchaseRequestTemplateQuery query, CancellationToken ct)
    {
        var sample = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                [PurchaseExcelColumns.Title] = "Giáo trình cơ sở dữ liệu",
                [PurchaseExcelColumns.Author] = "Nguyễn Văn A",
                [PurchaseExcelColumns.Publisher] = "NXB Giáo dục",
                [PurchaseExcelColumns.Year] = "2024",
                [PurchaseExcelColumns.Isbn] = "978-604-01-2345-6",
                [PurchaseExcelColumns.Quantity] = "5",
                [PurchaseExcelColumns.UnitPrice] = "120000"
            }
        };

        var content = _excel.WriteTemplate("Đề nghị mua", PurchaseExcelColumns.Template, sample);

        return Task.FromResult(new PrintedFileDto(
            content,
            "mau-de-nghi-mua.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
    }
}

public class ImportPurchaseLinesResultDto
{
    public Guid RequestId { get; set; }
    public int Imported { get; set; }
    public int DuplicateWarnings { get; set; }
    public decimal TotalAmount { get; set; }
    public List<ImportPurchaseLineErrorDto> Errors { get; set; } = new();
}

public record ImportPurchaseLineErrorDto(int RowNumber, string Message);

/// <summary>
/// Đọc danh sách đề nghị mua từ Excel vào một yêu cầu.
///
/// Bỏ trống <paramref name="RequestId"/> thì hệ thống tạo một yêu cầu nháp mới; đó là cách một
/// người đề nghị dán cả danh sách của khoa vào rồi sửa lại phần đầu sau.
/// </summary>
public record ImportPurchaseRequestLinesCommand(Guid? RequestId, byte[] Content)
    : IRequest<ImportPurchaseLinesResultDto>;

public class ImportPurchaseRequestLinesCommandHandler
    : IRequestHandler<ImportPurchaseRequestLinesCommand, ImportPurchaseLinesResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;
    private readonly ICodeGenerator _codes;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IPurchaseDuplicateFinder _duplicates;

    public ImportPurchaseRequestLinesCommandHandler(
        IApplicationDbContext db,
        IExcelService excel,
        ICodeGenerator codes,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IPurchaseDuplicateFinder duplicates)
    {
        _db = db;
        _excel = excel;
        _codes = codes;
        _currentUser = currentUser;
        _clock = clock;
        _duplicates = duplicates;
    }

    public async Task<ImportPurchaseLinesResultDto> Handle(
        ImportPurchaseRequestLinesCommand command, CancellationToken ct)
    {
        using var stream = new MemoryStream(command.Content);
        var sheet = _excel.Read(stream);

        if (sheet.Rows.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("file", "Tệp không có dòng dữ liệu nào.");
        }

        PurchaseRequest request;

        if (command.RequestId is null)
        {
            request = new PurchaseRequest
            {
                Id = Guid.NewGuid(),
                Code = await _codes.NextAsync("REQUEST", ct),
                Type = PurchaseRequestType.Monograph,
                RequesterId = _currentUser.UserId,
                RequesterName = _currentUser.FullName ?? "Không rõ",
                RequestDate = _clock.Today,
                Status = PurchaseRequestStatus.Draft,
                Reason = "Nhập từ tệp Excel"
            };

            _db.PurchaseRequests.Add(request);
        }
        else
        {
            request = await _db.PurchaseRequests
                .FirstOrDefaultAsync(entity => entity.Id == command.RequestId, ct)
                ?? throw new NotFoundException("yêu cầu đặt mua", command.RequestId);

            if (request.Status != PurchaseRequestStatus.Draft)
            {
                throw new ConflictException(
                    $"Yêu cầu {request.Code} đã gửi duyệt nên không thêm dòng được nữa.");
            }
        }

        // Nhà cung cấp so theo tên đã bỏ dấu: tệp từ khoa gửi lên hay gõ thiếu dấu hoặc thừa khoảng
        // trắng, mà bắt người dùng sửa tên cho khớp từng ký tự thì tệp nào cũng phải sửa tay.
        var suppliers = await _db.Suppliers
            .AsNoTracking()
            .Select(supplier => new { supplier.Id, supplier.Name })
            .ToListAsync(ct);

        var supplierIndex = suppliers
            .GroupBy(supplier => VietnameseText.NormaliseForComparison(supplier.Name))
            .ToDictionary(group => group.Key, group => group.First().Id);

        var result = new ImportPurchaseLinesResultDto { RequestId = request.Id };

        foreach (var row in sheet.Rows)
        {
            if (row.IsEmpty)
            {
                continue;
            }

            var title = row.Get(PurchaseExcelColumns.Title).Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                result.Errors.Add(new ImportPurchaseLineErrorDto(row.RowNumber, "Dòng không có nhan đề."));
                continue;
            }

            var quantity = ParseInt(row.Get(PurchaseExcelColumns.Quantity)) ?? 1;

            if (quantity is < 1 or > 10000)
            {
                result.Errors.Add(new ImportPurchaseLineErrorDto(
                    row.RowNumber, $"Số lượng \"{row.Get(PurchaseExcelColumns.Quantity)}\" không hợp lệ."));
                continue;
            }

            var unitPrice = ParseDecimal(row.Get(PurchaseExcelColumns.UnitPrice)) ?? 0;

            if (unitPrice < 0)
            {
                result.Errors.Add(new ImportPurchaseLineErrorDto(row.RowNumber, "Đơn giá không được âm."));
                continue;
            }

            Guid? supplierId = null;
            var supplierName = row.Get(PurchaseExcelColumns.Supplier).Trim();

            if (!string.IsNullOrWhiteSpace(supplierName))
            {
                if (supplierIndex.TryGetValue(VietnameseText.NormaliseForComparison(supplierName), out var id))
                {
                    supplierId = id;
                }
                else
                {
                    result.Errors.Add(new ImportPurchaseLineErrorDto(
                        row.RowNumber,
                        $"Không có nhà cung cấp \"{supplierName}\" trong danh mục; dòng vẫn được nhập nhưng chưa gán nhà cung cấp."));
                }
            }

            var isbn = row.Get(PurchaseExcelColumns.Isbn).Trim();
            var match = await _duplicates.FindAsync(isbn, title, ct);

            if (match is not null)
            {
                result.DuplicateWarnings++;
            }

            _db.PurchaseRequestItems.Add(new PurchaseRequestItem
            {
                Id = Guid.NewGuid(),
                RequestId = request.Id,
                Title = title,
                Author = Trimmed(row.Get(PurchaseExcelColumns.Author)),
                PublisherName = Trimmed(row.Get(PurchaseExcelColumns.Publisher)),
                PublishYear = ParseInt(row.Get(PurchaseExcelColumns.Year)),
                Isbn = Trimmed(isbn),
                Issn = Trimmed(row.Get(PurchaseExcelColumns.Issn)),
                Quantity = quantity,
                UnitPrice = unitPrice,
                EstimatedAmount = quantity * unitPrice,
                SupplierId = supplierId,
                BibId = match?.BibId,
                IsDuplicate = match is not null,
                Note = Trimmed(row.Get(PurchaseExcelColumns.Note))
            });

            result.Imported++;
            result.TotalAmount += quantity * unitPrice;
        }

        if (result.Imported == 0)
        {
            throw new Common.Exceptions.ValidationException(
                "file",
                "Không đọc được dòng nào. Hãy kiểm tra tệp có đúng cột \"Nhan đề\" như tệp mẫu không.");
        }

        await _db.SaveChangesAsync(ct);

        // Tổng tiền của yêu cầu được cộng lại từ các dòng đã lưu, kể cả dòng có sẵn từ trước — tệp
        // này có thể là lần nhập thứ hai vào cùng một yêu cầu.
        request.TotalAmount = await _db.PurchaseRequestItems
            .Where(line => line.RequestId == request.Id)
            .SumAsync(line => (decimal?)line.EstimatedAmount, ct) ?? 0;

        await _db.SaveChangesAsync(ct);

        return result;
    }

    private static string? Trimmed(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? ParseInt(string value)
    {
        var cleaned = new string(value.Where(char.IsDigit).ToArray());

        return int.TryParse(cleaned, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    /// <summary>
    /// Đọc số tiền từ ô Excel.
    ///
    /// Ô tiền trong tệp thật xuất hiện đủ kiểu: "120000", "120.000", "120,000", "120 000 đ". Chỉ giữ
    /// chữ số là cách đọc đúng cả bốn, vì đơn giá sách ở Việt Nam luôn là số nguyên đồng.
    /// </summary>
    private static decimal? ParseDecimal(string value)
    {
        var cleaned = new string(value.Where(char.IsDigit).ToArray());

        return decimal.TryParse(cleaned, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }
}
