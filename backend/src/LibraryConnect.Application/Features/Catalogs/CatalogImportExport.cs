using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Admin.AuditLogs;
using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Catalogs;

/// <summary>Column headers shared by the template, the import and the export of every catalogue.</summary>
internal static class CatalogColumns
{
    public const string Code = "Mã";
    public const string Name = "Tên";
    public const string NameEn = "Tên tiếng Anh";
    public const string Description = "Mô tả";
    public const string Parent = "Thuộc cấp trên (mã)";
    public const string SortOrder = "Thứ tự";
    public const string Active = "Đang dùng";
}

// ---------------------------------------------------------------------------

/// <summary>Tệp Excel mẫu để nhập một danh mục, kèm sheet hướng dẫn từng cột.</summary>
public record GetCatalogTemplateQuery(string Catalog) : IRequest<ExportedFile>;

public class GetCatalogTemplateQueryHandler : IRequestHandler<GetCatalogTemplateQuery, ExportedFile>
{
    private readonly IExcelService _excel;

    public GetCatalogTemplateQueryHandler(IExcelService excel) => _excel = excel;

    public Task<ExportedFile> Handle(GetCatalogTemplateQuery request, CancellationToken ct)
    {
        var definition = CatalogRegistry.Require(request.Catalog);

        var columns = new List<ExcelTemplateColumn>
        {
            new(CatalogColumns.Code,
                "Mã duy nhất trong danh mục. Nếu mã đã tồn tại, dòng đó sẽ cập nhật giá trị hiện có thay vì tạo mới. " +
                "Để trống thì hệ thống tự sinh mã từ tên.",
                Required: false, Example: "SACH"),
            new(CatalogColumns.Name, "Tên hiển thị. Bắt buộc.", Required: true, Example: "Sách"),
            new(CatalogColumns.NameEn, "Tên tiếng Anh, dùng cho giao diện song ngữ.", Example: "Book"),
            new(CatalogColumns.Description, "Mô tả ngắn."),
            new(CatalogColumns.SortOrder, "Số nguyên, quyết định thứ tự hiển thị. Bỏ trống hiểu là 0.", Example: "10"),
            new(CatalogColumns.Active, "Có / Không. Bỏ trống hiểu là Có.", Example: "Có")
        };

        if (definition.IsHierarchical)
        {
            columns.Insert(4, new ExcelTemplateColumn(
                CatalogColumns.Parent,
                "Mã của giá trị cấp trên. Để trống nếu đây là giá trị gốc. " +
                "Giá trị cấp trên phải nằm ở dòng phía trên trong cùng tệp, hoặc đã có sẵn trong hệ thống.",
                Example: "000"));
        }

        columns.AddRange(definition.Fields.Select(field => new ExcelTemplateColumn(
            field.Label,
            DescribeField(field),
            field.Required)));

        var content = _excel.WriteTemplate(definition.PluralName, columns);
        var fileName = $"mau-{definition.Code}.xlsx";

        return Task.FromResult(new ExportedFile(content, fileName, ExportedFile.ExcelContentType));
    }

    private static string DescribeField(CatalogField field)
    {
        var description = field.Description ?? string.Empty;

        return field.Type switch
        {
            CatalogFieldType.Boolean => $"{description} Nhập Có hoặc Không.".Trim(),
            CatalogFieldType.Number or CatalogFieldType.Decimal => $"{description} Nhập số.".Trim(),
            CatalogFieldType.Select =>
                $"{description} Nhận một trong các giá trị: {string.Join(", ", field.Options.Select(o => o.Value))}.".Trim(),
            _ => description
        };
    }
}

// ---------------------------------------------------------------------------

/// <summary>Xuất toàn bộ giá trị của một danh mục ra Excel.</summary>
public record ExportCatalogQuery(string Catalog) : IRequest<ExportedFile>;

public class ExportCatalogQueryHandler : IRequestHandler<ExportCatalogQuery, ExportedFile>
{
    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;
    private readonly IAuditService _audit;

    public ExportCatalogQueryHandler(IApplicationDbContext db, IExcelService excel, IAuditService audit)
    {
        _db = db;
        _excel = excel;
        _audit = audit;
    }

    public async Task<ExportedFile> Handle(ExportCatalogQuery request, CancellationToken ct)
    {
        var definition = CatalogRegistry.Require(request.Catalog);
        var rows = await definition.ExecuteAsync(_db, new ExportOperation(), ct);

        var columns = new List<ExcelColumn<CatalogExportRow>>
        {
            new(CatalogColumns.Code, row => row.Code, 20),
            new(CatalogColumns.Name, row => row.Name, 42),
            new(CatalogColumns.NameEn, row => row.NameEn ?? string.Empty, 30),
            new(CatalogColumns.Description, row => row.Description ?? string.Empty, 40)
        };

        if (definition.IsHierarchical)
        {
            columns.Add(new ExcelColumn<CatalogExportRow>(CatalogColumns.Parent, row => row.ParentCode ?? string.Empty, 20));
        }

        columns.Add(new ExcelColumn<CatalogExportRow>(CatalogColumns.SortOrder, row => row.SortOrder, 10));
        columns.Add(new ExcelColumn<CatalogExportRow>(CatalogColumns.Active, row => row.IsActive, 12));

        // The exported file uses the same headers as the template, so it can be edited and imported
        // straight back — which is how a librarian does a bulk correction.
        columns.AddRange(definition.Fields.Select(field =>
            new ExcelColumn<CatalogExportRow>(field.Label, row => row.Extras.GetValueOrDefault(field.Key) ?? string.Empty, 24)));

        var content = _excel.Write(definition.PluralName, columns, rows);
        var fileName = $"{definition.Code}-{DateTimeOffset.Now:yyyyMMdd-HHmm}.xlsx";

        await _audit.LogAsync(AuditAction.Export, definition.EntityType.Name, null, definition.PluralName,
            message: $"Xuất {rows.Count} giá trị danh mục {definition.PluralName}", ct: ct);

        return new ExportedFile(content, fileName, ExportedFile.ExcelContentType);
    }

    internal sealed class CatalogExportRow
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string? Description { get; set; }
        public string? ParentCode { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, string?> Extras { get; set; } = new();
    }

    private sealed class ExportOperation : ICatalogOperation<List<CatalogExportRow>>
    {
        public async Task<List<CatalogExportRow>> ExecuteAsync<TEntity>(
            DbSet<TEntity> set, CatalogDefinition definition, CancellationToken ct)
            where TEntity : CatalogEntity, new()
        {
            var entities = await set
                .AsNoTracking()
                .OrderBy(entity => entity.SortOrder)
                .ThenBy(entity => entity.Name)
                .ToListAsync(ct);

            var codesById = entities.ToDictionary(entity => entity.Id, entity => entity.Code);

            return entities.Select(entity =>
            {
                var row = new CatalogExportRow
                {
                    Code = entity.Code,
                    Name = entity.Name,
                    NameEn = entity.NameEn,
                    Description = entity.Description,
                    SortOrder = entity.SortOrder,
                    IsActive = entity.IsActive
                };

                if (entity is HierarchicalCatalogEntity hierarchical && hierarchical.ParentId is { } parentId)
                {
                    row.ParentCode = codesById.GetValueOrDefault(parentId);
                }

                foreach (var field in definition.Fields)
                {
                    row.Extras[field.Key] = field.Read(entity);
                }

                return row;
            }).ToList();
        }
    }
}

// ---------------------------------------------------------------------------

/// <summary>
/// Nhập một danh mục từ Excel.
///
/// A row whose code already exists updates that value instead of creating a duplicate, which makes
/// the export-edit-import loop the natural way to correct a list in bulk. As with the user import,
/// <paramref name="DryRun"/> validates the whole file and writes nothing.
/// </summary>
public record ImportCatalogCommand(string Catalog, Stream FileStream, string FileName, bool DryRun)
    : IRequest<CatalogImportResultDto>;

public class ImportCatalogCommandHandler : IRequestHandler<ImportCatalogCommand, CatalogImportResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;
    private readonly ICacheService _cache;
    private readonly IAuditService _audit;

    public ImportCatalogCommandHandler(
        IApplicationDbContext db, IExcelService excel, ICacheService cache, IAuditService audit)
    {
        _db = db;
        _excel = excel;
        _cache = cache;
        _audit = audit;
    }

    public async Task<CatalogImportResultDto> Handle(ImportCatalogCommand request, CancellationToken ct)
    {
        var definition = CatalogRegistry.Require(request.Catalog);
        var sheet = _excel.Read(request.FileStream);

        var result = await definition.ExecuteAsync(
            _db, new ImportOperation(sheet, request.DryRun, _db), ct);

        if (!request.DryRun && (result.CreatedRows > 0 || result.UpdatedRows > 0))
        {
            await _cache.RemoveByPrefixAsync(Common.Extensions.CacheKeyPrefixes.Catalogs, ct);

            await _audit.LogAsync(AuditAction.Import, definition.EntityType.Name, null, request.FileName,
                newValue: new { result.TotalRows, result.CreatedRows, result.UpdatedRows, result.ErrorRows },
                message: $"Nhập danh mục {definition.PluralName} từ '{request.FileName}': " +
                         $"thêm {result.CreatedRows}, cập nhật {result.UpdatedRows}, lỗi {result.ErrorRows}",
                ct: ct);
        }

        return result;
    }

    private sealed class ImportOperation : ICatalogOperation<CatalogImportResultDto>
    {
        private readonly ExcelSheet _sheet;
        private readonly bool _dryRun;
        private readonly IApplicationDbContext _db;

        public ImportOperation(ExcelSheet sheet, bool dryRun, IApplicationDbContext db)
        {
            _sheet = sheet;
            _dryRun = dryRun;
            _db = db;
        }

        public async Task<CatalogImportResultDto> ExecuteAsync<TEntity>(
            DbSet<TEntity> set, CatalogDefinition definition, CancellationToken ct)
            where TEntity : CatalogEntity, new()
        {
            var result = new CatalogImportResultDto { TotalRows = _sheet.Rows.Count };

            if (!_sheet.Headers.Contains(CatalogColumns.Name, StringComparer.OrdinalIgnoreCase))
            {
                result.Errors.Add(new CatalogImportErrorDto
                {
                    Row = 1,
                    Column = CatalogColumns.Name,
                    Message = $"Tệp thiếu cột bắt buộc '{CatalogColumns.Name}'. Vui lòng tải lại tệp mẫu."
                });

                result.ErrorRows = result.TotalRows;
                return result;
            }

            var existing = await set.ToDictionaryAsync(entity => entity.Code, StringComparer.OrdinalIgnoreCase, ct);
            var pending = new List<(TEntity Entity, bool IsNew, string? ParentCode)>();
            var codesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in _sheet.Rows)
            {
                var name = row.Get(CatalogColumns.Name);
                var code = row.Get(CatalogColumns.Code);

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add(Error(row.RowNumber, CatalogColumns.Name, name, "Chưa nhập tên."));
                    result.ErrorRows++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    code = CatalogCodeGenerator.Slugify(name);
                }

                if (!codesInFile.Add(code))
                {
                    result.Errors.Add(Error(row.RowNumber, CatalogColumns.Code, code,
                        "Mã bị lặp lại trong cùng tệp."));
                    result.ErrorRows++;
                    continue;
                }

                var isNew = !existing.TryGetValue(code, out var entity);

                if (isNew)
                {
                    entity = new TEntity { Id = Guid.NewGuid(), Code = code };
                }

                entity!.Name = name.Trim();
                entity.NameEn = NullIfBlank(row.Get(CatalogColumns.NameEn));
                entity.Description = NullIfBlank(row.Get(CatalogColumns.Description));
                entity.SortOrder = ParseInt(row.Get(CatalogColumns.SortOrder));
                entity.IsActive = ParseBoolean(row.Get(CatalogColumns.Active));

                var fieldError = ApplyFields(entity, row, definition, result);
                if (fieldError)
                {
                    result.ErrorRows++;
                    continue;
                }

                pending.Add((entity, isNew, definition.IsHierarchical ? NullIfBlank(row.Get(CatalogColumns.Parent)) : null));

                if (isNew)
                {
                    // Registered immediately so a later row can reference it as a parent.
                    existing[code] = entity;
                }
            }

            // Parents are resolved after every row is known, so the file does not have to be sorted
            // with parents before children.
            foreach (var (entity, _, parentCode) in pending.Where(p => p.ParentCode is not null))
            {
                if (existing.TryGetValue(parentCode!, out var parent))
                {
                    ((HierarchicalCatalogEntity)(object)entity).ParentId = parent.Id;
                }
                else
                {
                    result.Errors.Add(new CatalogImportErrorDto
                    {
                        Row = 0,
                        Column = CatalogColumns.Parent,
                        Value = parentCode,
                        Message = $"Không tìm thấy giá trị cấp trên có mã '{parentCode}'."
                    });
                }
            }

            result.CreatedRows = pending.Count(p => p.IsNew);
            result.UpdatedRows = pending.Count(p => !p.IsNew);

            if (_dryRun || pending.Count == 0)
            {
                return result;
            }

            set.AddRange(pending.Where(p => p.IsNew).Select(p => p.Entity));
            await _db.SaveChangesAsync(ct);

            foreach (var (entity, _, _) in pending)
            {
                if (entity is HierarchicalCatalogEntity hierarchical)
                {
                    await CatalogMapper.UpdatePathAsync(set, hierarchical, ct);
                }
            }

            await _db.SaveChangesAsync(ct);
            return result;
        }

        /// <summary>Writes the catalogue-specific columns, reporting any value the field rejects.</summary>
        private static bool ApplyFields(
            CatalogEntity entity, ExcelRow row, CatalogDefinition definition, CatalogImportResultDto result)
        {
            var hasError = false;

            foreach (var field in definition.Fields)
            {
                var raw = row.Get(field.Label);

                if (string.IsNullOrWhiteSpace(raw))
                {
                    if (field.Required)
                    {
                        result.Errors.Add(Error(row.RowNumber, field.Label, raw, $"Chưa nhập {field.Label.ToLowerInvariant()}."));
                        hasError = true;
                    }

                    continue;
                }

                if (field.Type == CatalogFieldType.Select
                    && field.Options.Count > 0
                    && !field.Options.Any(option => string.Equals(option.Value, raw, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Errors.Add(Error(row.RowNumber, field.Label, raw,
                        $"Giá trị không hợp lệ. Chỉ nhận: {string.Join(", ", field.Options.Select(o => o.Value))}."));
                    hasError = true;
                    continue;
                }

                field.Write(entity, raw);
            }

            return hasError;
        }

        private static CatalogImportErrorDto Error(int row, string column, string? value, string message) =>
            new() { Row = row, Column = column, Value = value, Message = message };

        private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static int ParseInt(string value) =>
            int.TryParse(value, out var parsed) ? parsed : 0;

        private static bool ParseBoolean(string value) =>
            string.IsNullOrWhiteSpace(value)
            || value.Trim().ToLowerInvariant() is "có" or "co" or "x" or "1" or "true" or "yes";
    }
}
