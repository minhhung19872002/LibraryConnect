using System.Globalization;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Acquisition;

// ---------------------------------------------------------------------------------------------
// III.2 — Biên mục sơ lược. Form rút gọn nhưng lưu đúng cấu trúc MARC 21, rồi đẩy vào hàng đợi để
// biên mục chi tiết sau.
// ---------------------------------------------------------------------------------------------

/// <summary>Kết quả một lần biên mục sơ lược.</summary>
public class QuickCatalogResultDto
{
    public Guid BibId { get; set; }
    public string ControlNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    /// <summary>Biểu ghi đã có sẵn và được dùng lại thay vì tạo mới.</summary>
    public bool ReusedExisting { get; set; }
    public int CreatedItems { get; set; }
    public List<string> Barcodes { get; set; } = new();
}

/// <summary>
/// Biên mục sơ lược (III.2): khoảng mười trường, lưu thành biểu ghi MARC 21 đầy đủ.
/// </summary>
public class QuickCatalogCommand : IRequest<QuickCatalogResultDto>
{
    public string Title { get; set; } = string.Empty;
    public string? SubTitle { get; set; }
    public string? Author { get; set; }
    public string? PublisherName { get; set; }
    public string? PublishPlace { get; set; }
    public int? PublishYear { get; set; }
    public string? Isbn { get; set; }
    public int? Pages { get; set; }
    public decimal Price { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Ddc { get; set; }
    public string? Note { get; set; }

    /// <summary>Dòng đơn đặt mà biểu ghi này thuộc về; đặt thì dòng đó được nối vào biểu ghi.</summary>
    public Guid? OrderItemId { get; set; }

    /// <summary>Dùng lại biểu ghi đã có nếu tra ISBN thấy trùng, thay vì tạo bản ghi thứ hai.</summary>
    public bool ReuseDuplicate { get; set; } = true;

    /// <summary>Tạo luôn ĐKCB. Để 0 thì chỉ tạo biểu ghi.</summary>
    public int ItemQuantity { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? ShelfId { get; set; }
    public Guid? FundingSourceId { get; set; }
    public AcquisitionType AcquisitionType { get; set; } = AcquisitionType.Purchase;
}

public class QuickCatalogCommandValidator : AbstractValidator<QuickCatalogCommand>
{
    public QuickCatalogCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Chưa nhập nhan đề.").MaximumLength(1000);

        RuleFor(command => command.PublishYear)
            .InclusiveBetween(1400, 2200).When(command => command.PublishYear.HasValue)
            .WithMessage("Năm xuất bản phải nằm trong khoảng 1400 đến 2200.");

        RuleFor(command => command.Price).GreaterThanOrEqualTo(0).WithMessage("Giá không được âm.");

        RuleFor(command => command.ItemQuantity)
            .InclusiveBetween(0, 500).WithMessage("Số bản phải từ 0 đến 500.");

        RuleFor(command => command.WarehouseId)
            .NotEmpty().When(command => command.ItemQuantity > 0)
            .WithMessage("Tạo ĐKCB thì phải chọn kho.");
    }
}

public class QuickCatalogCommandHandler : IRequestHandler<QuickCatalogCommand, QuickCatalogResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IBibRecordWriter _writer;
    private readonly IPurchaseDuplicateFinder _duplicates;
    private readonly ICodeGenerator _codes;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;

    public QuickCatalogCommandHandler(
        IApplicationDbContext db,
        IBibRecordWriter writer,
        IPurchaseDuplicateFinder duplicates,
        ICodeGenerator codes,
        ISystemParameterService parameters,
        IDateTimeProvider clock)
    {
        _db = db;
        _writer = writer;
        _duplicates = duplicates;
        _codes = codes;
        _parameters = parameters;
        _clock = clock;
    }

    public async Task<QuickCatalogResultDto> Handle(QuickCatalogCommand command, CancellationToken ct)
    {
        var result = new QuickCatalogResultDto();
        BibRecord entity;

        var match = command.ReuseDuplicate
            ? await _duplicates.FindAsync(command.Isbn, command.Title, ct)
            : null;

        if (match is not null)
        {
            // Sách bổ sung thêm bản của một đầu đã có là chuyện thường xuyên; tạo biểu ghi thứ hai
            // cho cùng một cuốn mới là lỗi, vì OPAC sẽ hiện hai kết quả cho một cuốn sách.
            entity = await _db.BibRecords.FirstAsync(record => record.Id == match.BibId, ct);
            result.ReusedExisting = true;
        }
        else
        {
            entity = new BibRecord
            {
                Id = Guid.NewGuid(),
                Source = BibSource.Manual,
                Status = RecordStatus.Draft,
                DocumentTypeId = command.DocumentTypeId,
                LanguageId = command.LanguageId
            };

            var marc = BuildMarc(command);

            // Biểu ghi sơ lược vẫn phải có 008 hợp lệ: thiếu nó thì cán bộ biên mục mở ra ở trình
            // soạn MARC, bổ sung xong lại không lưu được vì bộ kiểm tra chặn ngay từ đầu.
            await Cataloging.Marc008Builder.EnsureAsync(
                marc, _parameters, _clock.Today, command.PublishYear, ct);

            await _writer.PrepareAsync(entity, marc, ct);
            _db.BibRecords.Add(entity);

            await _writer.ApplyAsync(entity, marc, isNew: true,
                changeNote: "Biên mục sơ lược từ màn hình bổ sung", ct);

            // Biểu ghi sơ lược chưa đủ để tra cứu tử tế nên phải vào hàng đợi ngay, nếu không nó sẽ
            // nằm mãi ở mức mười trường.
            if (!await _db.CatalogQueue.AnyAsync(item => item.BibId == entity.Id, ct))
            {
                _db.CatalogQueue.Add(new CatalogQueueItem
                {
                    Id = Guid.NewGuid(),
                    BibId = entity.Id,
                    Status = CatalogQueueStatus.Pending,
                    Priority = 2,
                    Note = "Biên mục sơ lược khi nhập kho, cần biên mục chi tiết"
                });

                await _db.SaveChangesAsync(ct);
            }
        }

        result.BibId = entity.Id;
        result.ControlNumber = entity.ControlNumber;
        result.Title = entity.Title;

        if (command.OrderItemId is not null)
        {
            var line = await _db.PurchaseOrderItems
                .FirstOrDefaultAsync(item => item.Id == command.OrderItemId, ct)
                ?? throw new NotFoundException("dòng đơn đặt", command.OrderItemId);

            line.BibId = entity.Id;
            await _db.SaveChangesAsync(ct);
        }

        if (command.ItemQuantity > 0)
        {
            var created = await ItemCreator.CreateAsync(
                _db, _codes, _parameters, _clock,
                new ItemCreator.Request(
                    entity,
                    command.ItemQuantity,
                    command.WarehouseId!.Value,
                    command.ShelfId,
                    command.Price,
                    command.FundingSourceId,
                    command.AcquisitionType,
                    OrderId: null,
                    Note: command.Note),
                ct);

            result.CreatedItems = created.Count;
            result.Barcodes = created.ToList();
        }

        return result;
    }

    /// <summary>
    /// Dựng biểu ghi MARC 21 từ mười trường của form rút gọn.
    ///
    /// Form ngắn nhưng cái lưu xuống là MARC thật: 020 ISBN, 100 tác giả, 245 nhan đề, 260 xuất bản,
    /// 300 mô tả vật lý, 082 phân loại. Nhờ vậy khi cán bộ biên mục mở nó ra ở trình soạn MARC thì
    /// không phải gõ lại, chỉ bổ sung.
    /// </summary>
    private static MarcRecord BuildMarc(QuickCatalogCommand command)
    {
        var record = new MarcRecord();
        record.Leader.RecordStatus = 'n';
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';
        record.Leader.CharacterCodingScheme = 'a';
        // Mức biên mục 3 = biên mục rút gọn; đúng nghĩa đen của biểu ghi này và là dấu hiệu cho cán
        // bộ biên mục biết nó chưa xong.
        record.Leader.EncodingLevel = '3';

        if (!string.IsNullOrWhiteSpace(command.Isbn))
        {
            record.AddField("020").AddSubfield('a', command.Isbn.Trim());
        }

        if (!string.IsNullOrWhiteSpace(command.Ddc))
        {
            record.AddField("082", '0', '4').AddSubfield('a', command.Ddc.Trim());
        }

        if (!string.IsNullOrWhiteSpace(command.Author))
        {
            record.AddField("100", '1').AddSubfield('a', command.Author.Trim());
        }

        var titleSubfields = new List<(char, string)>();
        var title = command.Title.Trim();

        titleSubfields.Add(('a', string.IsNullOrWhiteSpace(command.SubTitle) ? title : title + " :"));

        if (!string.IsNullOrWhiteSpace(command.SubTitle))
        {
            titleSubfields.Add(('b', command.SubTitle.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(command.Author))
        {
            titleSubfields.Add(('c', command.Author.Trim()));
        }

        var titleField = record.AddField("245", string.IsNullOrWhiteSpace(command.Author) ? '0' : '1', '0');

        foreach (var (code, value) in titleSubfields)
        {
            titleField.AddSubfield(code, value);
        }

        var publication = new List<(char, string)>();

        if (!string.IsNullOrWhiteSpace(command.PublishPlace))
        {
            publication.Add(('a', command.PublishPlace.Trim() + " :"));
        }

        if (!string.IsNullOrWhiteSpace(command.PublisherName))
        {
            publication.Add(('b', command.PublisherName.Trim() + ","));
        }

        if (command.PublishYear is not null)
        {
            publication.Add(('c', command.PublishYear.Value.ToString(CultureInfo.InvariantCulture)));
        }

        if (publication.Count > 0)
        {
            var publicationField = record.AddField("260");

            foreach (var (code, value) in publication)
            {
                publicationField.AddSubfield(code, value);
            }
        }

        if (command.Pages is > 0)
        {
            record.AddField("300").AddSubfield('a', $"{command.Pages} tr.");
        }

        if (!string.IsNullOrWhiteSpace(command.Note))
        {
            record.AddField("500").AddSubfield('a', command.Note.Trim());
        }

        return record;
    }
}

// ---------------------------------------------------------------------------------------------
// III.1 → III.2 — Tạo ĐKCB từ đơn đặt đã nhận hàng.
// ---------------------------------------------------------------------------------------------

/// <summary>Tạo ĐKCB cho các dòng đã nhận của một đơn đặt.</summary>
public class CreateItemsFromOrderCommand : IRequest<CreateItemsFromOrderResultDto>
{
    public Guid OrderId { get; set; }
    /// <summary>Bỏ trống thì làm cho mọi dòng đã nhận và đã có biểu ghi.</summary>
    public List<Guid> OrderItemIds { get; set; } = new();
    public Guid WarehouseId { get; set; }
    public Guid? ShelfId { get; set; }
    public Guid? FundingSourceId { get; set; }
    public DateOnly? AcquisitionDate { get; set; }
    public AcquisitionType AcquisitionType { get; set; } = AcquisitionType.Purchase;
    /// <summary>Mở khóa ngay, bỏ qua bước kiểm nhận. Mặc định là chờ kiểm nhận.</summary>
    public bool UnlockImmediately { get; set; }
}

public class CreateItemsFromOrderResultDto
{
    public int CreatedItems { get; set; }
    public List<string> Barcodes { get; set; } = new();
    /// <summary>Dòng chưa biên mục nên chưa tạo ĐKCB được.</summary>
    public List<string> PendingCataloging { get; set; } = new();
}

public class CreateItemsFromOrderCommandValidator : AbstractValidator<CreateItemsFromOrderCommand>
{
    public CreateItemsFromOrderCommandValidator()
    {
        RuleFor(command => command.WarehouseId).NotEmpty().WithMessage("Chưa chọn kho nhập.");
    }
}

public class CreateItemsFromOrderCommandHandler
    : IRequestHandler<CreateItemsFromOrderCommand, CreateItemsFromOrderResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;

    public CreateItemsFromOrderCommandHandler(
        IApplicationDbContext db,
        ICodeGenerator codes,
        ISystemParameterService parameters,
        IDateTimeProvider clock)
    {
        _db = db;
        _codes = codes;
        _parameters = parameters;
        _clock = clock;
    }

    public async Task<CreateItemsFromOrderResultDto> Handle(
        CreateItemsFromOrderCommand command, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(entity => entity.Id == command.OrderId, ct)
            ?? throw new NotFoundException("đơn đặt", command.OrderId);

        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(entity => entity.Id == command.WarehouseId, ct)
            ?? throw new NotFoundException("kho", command.WarehouseId);

        if (warehouse.IsClosedForInventory)
        {
            throw new ConflictException($"Kho {warehouse.Name} đang đóng để kiểm kê nên chưa nhập được.");
        }

        var lines = await _db.PurchaseOrderItems
            .Include(line => line.Bib)
            .Where(line => line.OrderId == order.Id && line.ReceivedQuantity > 0)
            .ToListAsync(ct);

        if (command.OrderItemIds.Count > 0)
        {
            lines = lines.Where(line => command.OrderItemIds.Contains(line.Id)).ToList();
        }

        var result = new CreateItemsFromOrderResultDto();

        foreach (var line in lines)
        {
            if (line.BibId is null || line.Bib is null)
            {
                result.PendingCataloging.Add(line.Title);
                continue;
            }

            // Đã tạo bao nhiêu bản cho dòng này rồi thì chỉ tạo nốt phần thiếu — bấm nút hai lần
            // không được sinh ra gấp đôi số sách.
            var already = await _db.Items.CountAsync(
                item => item.OrderId == order.Id && item.BibId == line.BibId, ct);

            var remaining = line.ReceivedQuantity - already;

            if (remaining <= 0)
            {
                continue;
            }

            var barcodes = await ItemCreator.CreateAsync(
                _db, _codes, _parameters, _clock,
                new ItemCreator.Request(
                    line.Bib,
                    remaining,
                    command.WarehouseId,
                    command.ShelfId,
                    line.UnitPrice,
                    command.FundingSourceId ?? order.FundingSourceId,
                    command.AcquisitionType,
                    order.Id,
                    Note: $"Nhập theo đơn {order.Code}",
                    AcquisitionDate: command.AcquisitionDate,
                    SupplierId: order.SupplierId,
                    UnlockImmediately: command.UnlockImmediately),
                ct);

            result.CreatedItems += barcodes.Count;
            result.Barcodes.AddRange(barcodes);
        }

        if (result.CreatedItems == 0 && result.PendingCataloging.Count == 0)
        {
            throw new ConflictException(
                "Không có dòng nào để nhập kho: các dòng đã nhận đều đã tạo đủ ĐKCB.");
        }

        return result;
    }
}

/// <summary>
/// Sinh ĐKCB cho một biểu ghi.
///
/// Dùng chung cho biên mục sơ lược, nhập kho theo đơn đặt và tạo bản từ màn hình biên mục, để số
/// mã vạch, số ĐKCB và ký hiệu xếp giá luôn theo cùng một quy tắc dù đi vào bằng đường nào.
/// </summary>
public static class ItemCreator
{
    public record Request(
        BibRecord Bib,
        int Quantity,
        Guid WarehouseId,
        Guid? ShelfId,
        decimal Price,
        Guid? FundingSourceId,
        AcquisitionType AcquisitionType,
        Guid? OrderId,
        string? Note,
        DateOnly? AcquisitionDate = null,
        Guid? SupplierId = null,
        bool UnlockImmediately = false);

    public static async Task<IReadOnlyList<string>> CreateAsync(
        IApplicationDbContext db,
        ICodeGenerator codes,
        ISystemParameterService parameters,
        IDateTimeProvider clock,
        Request request,
        CancellationToken ct)
    {
        var warehouse = await db.Warehouses.FirstOrDefaultAsync(entity => entity.Id == request.WarehouseId, ct)
            ?? throw new NotFoundException("kho", request.WarehouseId);

        if (request.ShelfId is not null)
        {
            var belongs = await db.Shelves.AnyAsync(
                shelf => shelf.Id == request.ShelfId && shelf.WarehouseId == warehouse.Id, ct);

            if (!belongs)
            {
                throw new Common.Exceptions.ValidationException(
                    "shelfId", $"Giá đã chọn không thuộc kho {warehouse.Name}.");
            }
        }

        var pattern = string.IsNullOrWhiteSpace(warehouse.CallNumberRule)
            ? await parameters.GetAsync("CATALOG.CALL_NUMBER_PATTERN", CallNumberBuilder.DefaultPattern, ct)
            : warehouse.CallNumberRule;

        var callNumber = CallNumberBuilder.Build(pattern, new CallNumberBuilder.Context(
            request.Bib.Ddc, request.Bib.AuthorMain, request.Bib.Title, request.Bib.PublishYear, 1));

        var barcodes = await codes.NextBatchAsync("BARCODE", request.Quantity, ct);
        var registerNumbers = await codes.NextBatchAsync("REGISTER", request.Quantity, ct);

        var lastCopy = await db.Items
            .Where(item => item.BibId == request.Bib.Id)
            .MaxAsync(item => (int?)item.CopyNumber, ct) ?? 0;

        var acquisitionDate = request.AcquisitionDate ?? clock.Today;

        for (var index = 0; index < request.Quantity; index++)
        {
            db.Items.Add(new Item
            {
                Id = Guid.NewGuid(),
                BibId = request.Bib.Id,
                Barcode = barcodes[index],
                RegisterNumber = registerNumbers[index],
                WarehouseId = request.WarehouseId,
                ShelfId = request.ShelfId,
                CallNumber = callNumber,
                Price = request.Price,
                FundingSourceId = request.FundingSourceId,
                AcquisitionDate = acquisitionDate,
                AcquisitionType = request.AcquisitionType,
                OrderId = request.OrderId,
                SupplierId = request.SupplierId,
                Status = request.UnlockImmediately ? ItemStatus.InStock : ItemStatus.PendingInspection,
                IsLocked = !request.UnlockImmediately,
                LockReason = request.UnlockImmediately ? null : "Chờ kiểm nhận",
                CopyNumber = lastCopy + index + 1,
                Note = request.Note
            });
        }

        await db.SaveChangesAsync(ct);

        await BibItemCounter.RefreshAsync(db, new[] { request.Bib.Id }, ct);

        if (request.ShelfId is not null)
        {
            await ShelfCounter.RefreshAsync(db, new[] { request.ShelfId.Value }, ct);
        }

        return barcodes;
    }
}
