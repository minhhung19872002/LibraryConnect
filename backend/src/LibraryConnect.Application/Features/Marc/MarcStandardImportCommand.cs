using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Bib;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Marc;

/// <summary>
/// Nạp lại bộ định nghĩa MARC 21 chuẩn (II.5 — "Import bộ định nghĩa MARC21 chuẩn").
///
/// Hai chế độ:
///
/// * <c>Overwrite = false</c> — chỉ thêm những tag còn thiếu. Đây là việc bộ nạp dữ liệu vẫn làm ở
///   mỗi lần khởi động, nay cán bộ gọi được từ giao diện mà không phải khởi động lại máy chủ.
/// * <c>Overwrite = true</c> — ghi đè cả những trường đã có bằng bản chuẩn. Dùng khi sửa hỏng một
///   trường và muốn về nguyên trạng; mọi thay đổi riêng của thư viện trên các trường ấy sẽ mất, nên
///   giao diện phải hỏi lại trước khi gọi.
///
/// Trường thư viện **tự thêm** (tag không có trong bộ chuẩn) không bao giờ bị đụng tới ở cả hai chế
/// độ: chúng là dữ liệu nghiệp vụ, không phải bản sao của bộ chuẩn.
/// </summary>
public record ImportStandardMarcFieldsCommand(bool Overwrite = false) : IRequest<MarcStandardImportResultDto>;

public class MarcStandardImportResultDto
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    /// <summary>Số trường của thư viện nằm ngoài bộ chuẩn, được giữ nguyên.</summary>
    public int Custom { get; set; }
}

public class ImportStandardMarcFieldsCommandHandler
    : IRequestHandler<ImportStandardMarcFieldsCommand, MarcStandardImportResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IMarcStandardFields _standard;
    private readonly IAuditService _audit;

    public ImportStandardMarcFieldsCommandHandler(
        IApplicationDbContext db, IMarcStandardFields standard, IAuditService audit)
    {
        _db = db;
        _standard = standard;
        _audit = audit;
    }

    public async Task<MarcStandardImportResultDto> Handle(
        ImportStandardMarcFieldsCommand request, CancellationToken ct)
    {
        var standard = _standard.Load();

        // Kể cả trường đã xoá mềm: tag là khoá duy nhất, thêm lại một tag đang nằm trong thùng rác
        // sẽ đụng ràng buộc duy nhất ở cơ sở dữ liệu.
        var existing = await _db.MarcFieldDefinitions
            .IgnoreQueryFilters()
            .ToDictionaryAsync(field => field.Tag, StringComparer.Ordinal, ct);

        var result = new MarcStandardImportResultDto
        {
            Custom = existing.Keys.Count(tag => standard.All(field => field.Tag != tag))
        };

        foreach (var field in standard)
        {
            if (!existing.TryGetValue(field.Tag, out var entity))
            {
                _db.MarcFieldDefinitions.Add(new MarcFieldDefinition
                {
                    Id = Guid.NewGuid(),
                    Tag = field.Tag,
                    Name = field.Name,
                    NameEn = field.NameEn,
                    Description = field.Description,
                    IsControl = field.IsControl,
                    IsRepeatable = field.IsRepeatable,
                    IsRequired = field.IsRequired,
                    IsRecommended = field.IsRecommended,
                    SortOrder = field.SortOrder,
                    IsActive = true,
                    Indicators = field.IndicatorsJson,
                    Subfields = field.SubfieldsJson
                });

                result.Added++;
                continue;
            }

            if (!request.Overwrite)
            {
                result.Unchanged++;
                continue;
            }

            entity.Name = field.Name;
            entity.NameEn = field.NameEn;
            entity.Description = field.Description;
            entity.IsControl = field.IsControl;
            entity.IsRepeatable = field.IsRepeatable;
            entity.IsRequired = field.IsRequired;
            entity.IsRecommended = field.IsRecommended;
            entity.SortOrder = field.SortOrder;
            entity.Indicators = field.IndicatorsJson;
            entity.Subfields = field.SubfieldsJson;

            // Trường bị xoá mềm rồi nạp lại thì phải sống lại, nếu không giao diện vẫn không thấy nó.
            entity.DeletedAt = null;
            entity.DeletedBy = null;
            entity.IsActive = true;

            result.Updated++;
        }

        if (result.Added + result.Updated > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        await _audit.LogAsync(
            Domain.Enums.AuditAction.Import,
            "MarcFieldDefinition",
            null,
            message: request.Overwrite
                ? $"Khôi phục bộ định nghĩa MARC 21 chuẩn: thêm {result.Added}, ghi đè {result.Updated}"
                : $"Nạp bổ sung định nghĩa MARC 21 chuẩn: thêm {result.Added}",
            ct: ct);

        return result;
    }
}
