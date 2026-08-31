using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Web;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cms;

// ---------------------------------------------------------------------------------------------
// VIII.1 — Màn hình "Thông tin trang thư viện".
//
// Hai kho cấu hình cùng hiện trên một màn hình vì với cán bộ chúng là một việc, còn với hệ thống
// thì khác nhau: tên, địa chỉ, logo là tham số hệ thống (cả phần mềm dùng, kể cả biểu mẫu in), còn
// khẩu hiệu, giờ mở cửa, mạng xã hội chỉ trang tra cứu dùng nên nằm ở bảng riêng của phân hệ này.
// ---------------------------------------------------------------------------------------------

/// <summary>Nơi lưu một ô cấu hình.</summary>
public static class CmsSettingStores
{
    /// <summary>Tham số hệ thống (sys.system_parameters).</summary>
    public const string Parameter = "PARAMETER";

    /// <summary>Cấu hình riêng của trang tra cứu (web.cms_settings).</summary>
    public const string Cms = "CMS";
}

/// <summary>
/// Danh sách các ô cấu hình thuộc riêng trang tra cứu.
///
/// Khai báo tại đây chứ không nằm rải rác trong mã: thêm một ô mới là thêm một dòng, giao diện tự
/// dựng đúng điều khiển nhập theo kiểu dữ liệu, và bản cài mới tự có đủ ô nhờ phần nạp dữ liệu ban
/// đầu đọc chính danh sách này.
/// </summary>
public static class CmsSettingCatalog
{
    public record Definition(
        string Key,
        string GroupCode,
        string GroupName,
        string Name,
        string? Description,
        string DataType,
        string? DefaultValue,
        int SortOrder);

    public const string Slogan = "SITE.SLOGAN";
    public const string FaviconUrl = "SITE.FAVICON_URL";
    public const string HeroImageUrl = "SITE.HERO_IMAGE_URL";
    public const string FooterText = "SITE.FOOTER_TEXT";
    public const string OpeningHours = "SITE.OPENING_HOURS";
    public const string MapEmbedUrl = "SITE.MAP_EMBED_URL";
    public const string ContactNote = "SITE.CONTACT_NOTE";
    public const string FacebookUrl = "SOCIAL.FACEBOOK";
    public const string YoutubeUrl = "SOCIAL.YOUTUBE";
    public const string ZaloUrl = "SOCIAL.ZALO";
    public const string NewsPerPage = "SITE.NEWS_PER_PAGE";
    public const string ShowNewBooks = "SITE.SHOW_NEW_BOOKS";
    public const string ShowPopularBooks = "SITE.SHOW_POPULAR_BOOKS";
    public const string ShowInterlibrary = "SITE.SHOW_INTERLIBRARY";

    public static readonly IReadOnlyList<Definition> All = new List<Definition>
    {
        new(Slogan, "SITE", "Thương hiệu trang tra cứu", "Khẩu hiệu",
            "Dòng chữ hiện dưới tên thư viện ở đầu trang tra cứu.", "text", "", 10),
        new(FaviconUrl, "SITE", "Thương hiệu trang tra cứu", "Biểu tượng trên thẻ trình duyệt",
            "Ảnh vuông, thường 32×32 hoặc 64×64.", "image", "", 20),
        new(HeroImageUrl, "SITE", "Thương hiệu trang tra cứu", "Ảnh nền khối tìm kiếm",
            "Ảnh lớn phía sau ô tìm kiếm ở trang chủ.", "image", "", 30),
        new(FooterText, "SITE", "Thương hiệu trang tra cứu", "Dòng chân trang",
            "Ví dụ: đơn vị chủ quản, giấy phép.", "text", "", 40),

        new(OpeningHours, "CONTACT", "Liên hệ và giờ mở cửa", "Giờ mở cửa",
            "Mỗi cơ sở một dòng, ví dụ: Trụ sở chính — Thứ 2 đến Thứ 6: 7h30–17h00.", "multiline", "", 10),
        new(ContactNote, "CONTACT", "Liên hệ và giờ mở cửa", "Ghi chú liên hệ",
            "Hiện trên trang Liên hệ, dưới địa chỉ và số điện thoại.", "multiline", "", 20),
        new(MapEmbedUrl, "CONTACT", "Liên hệ và giờ mở cửa", "Địa chỉ bản đồ nhúng",
            "Đường dẫn nhúng bản đồ; để trống thì trang Liên hệ không hiện bản đồ.", "text", "", 30),

        new(FacebookUrl, "SOCIAL", "Mạng xã hội", "Facebook", null, "text", "", 10),
        new(YoutubeUrl, "SOCIAL", "Mạng xã hội", "YouTube", null, "text", "", 20),
        new(ZaloUrl, "SOCIAL", "Mạng xã hội", "Zalo", null, "text", "", 30),

        new(NewsPerPage, "LAYOUT", "Bố cục trang chủ", "Số tin mỗi trang", null, "number", "9", 10),
        new(ShowNewBooks, "LAYOUT", "Bố cục trang chủ", "Hiện khối Sách mới bổ sung",
            null, "boolean", "true", 20),
        new(ShowPopularBooks, "LAYOUT", "Bố cục trang chủ", "Hiện khối Sách được mượn nhiều",
            null, "boolean", "true", 30),
        new(ShowInterlibrary, "LAYOUT", "Bố cục trang chủ", "Hiện mục Tìm ở thư viện khác",
            "Tắt khi thư viện chưa khai báo máy chủ liên thư viện nào.", "boolean", "true", 40)
    };

    public static Definition? Find(string key) =>
        All.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Đọc toàn bộ cấu hình hiển thị của trang tra cứu, gom theo nhóm.</summary>
public record GetCmsSettingsQuery : IRequest<IReadOnlyList<CmsSettingGroupDto>>;

public class GetCmsSettingsQueryHandler
    : IRequestHandler<GetCmsSettingsQuery, IReadOnlyList<CmsSettingGroupDto>>
{
    /// <summary>Các nhóm tham số hệ thống cũng thuộc màn hình này.</summary>
    private static readonly string[] ParameterGroups = { "LIBRARY", "OPAC" };

    private readonly IApplicationDbContext _db;

    public GetCmsSettingsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CmsSettingGroupDto>> Handle(
        GetCmsSettingsQuery query, CancellationToken ct)
    {
        var parameters = await _db.SystemParameters.AsNoTracking()
            .Where(parameter => ParameterGroups.Contains(parameter.GroupCode) && parameter.IsEditable)
            .OrderBy(parameter => parameter.SortOrder)
            .ToListAsync(ct);

        var stored = await _db.CmsSettings.AsNoTracking().ToListAsync(ct);

        var groups = new List<CmsSettingGroupDto>();

        foreach (var group in parameters.GroupBy(parameter => parameter.GroupCode))
        {
            groups.Add(new CmsSettingGroupDto(
                group.Key,
                group.First().GroupName,
                group.Select(parameter => new CmsSettingItemDto(
                        parameter.Key,
                        parameter.Value,
                        parameter.Name,
                        parameter.Description,
                        parameter.DataType.ToString().ToLowerInvariant(),
                        parameter.GroupCode,
                        parameter.GroupName,
                        CmsSettingStores.Parameter,
                        parameter.SortOrder))
                    .ToList()));
        }

        foreach (var group in CmsSettingCatalog.All.GroupBy(item => item.GroupCode))
        {
            groups.Add(new CmsSettingGroupDto(
                group.Key,
                group.First().GroupName,
                group.OrderBy(item => item.SortOrder)
                    .Select(item => new CmsSettingItemDto(
                        item.Key,
                        stored.FirstOrDefault(row => row.Key == item.Key)?.Value ?? item.DefaultValue,
                        item.Name,
                        item.Description,
                        item.DataType,
                        item.GroupCode,
                        item.GroupName,
                        CmsSettingStores.Cms,
                        item.SortOrder))
                    .ToList()));
        }

        return groups;
    }
}

/// <summary>Lưu các ô cấu hình đã sửa. Mỗi ô tự biết mình về kho nào.</summary>
public record UpdateCmsSettingsCommand(IReadOnlyList<CmsSettingValueDto> Items) : IRequest;

public record CmsSettingValueDto(string Key, string? Value);

public class UpdateCmsSettingsCommandHandler : IRequestHandler<UpdateCmsSettingsCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;

    public UpdateCmsSettingsCommandHandler(
        IApplicationDbContext db, ISystemParameterService parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    public async Task Handle(UpdateCmsSettingsCommand command, CancellationToken ct)
    {
        foreach (var item in command.Items)
        {
            var definition = CmsSettingCatalog.Find(item.Key);

            if (definition is not null)
            {
                await WriteCmsAsync(definition, item.Value, ct);
                continue;
            }

            var parameter = await _db.SystemParameters
                .FirstOrDefaultAsync(row => row.Key == item.Key, ct);

            if (parameter is null)
            {
                throw new NotFoundException($"Không có ô cấu hình mang mã \"{item.Key}\".");
            }

            if (!parameter.IsEditable)
            {
                throw new ConflictException($"Tham số \"{parameter.Name}\" không cho sửa từ giao diện.");
            }

            await _parameters.SetAsync(item.Key, item.Value, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task WriteCmsAsync(
        CmsSettingCatalog.Definition definition, string? value, CancellationToken ct)
    {
        var row = await _db.CmsSettings.FirstOrDefaultAsync(entity => entity.Key == definition.Key, ct);

        if (row is null)
        {
            row = new CmsSetting { Key = definition.Key };
            _db.CmsSettings.Add(row);
        }

        row.Value = value;
        row.GroupCode = definition.GroupCode;
        row.Name = definition.Name;
        row.Description = definition.Description;
        row.DataType = definition.DataType;
        row.SortOrder = definition.SortOrder;
    }
}

/// <summary>
/// Đọc nhanh một nhóm cấu hình của trang tra cứu, dùng cho các endpoint công khai.
///
/// Ô chưa từng được lưu thì trả về giá trị mặc định trong bảng khai báo, nên trang tra cứu chạy
/// đúng ngay từ lần cài đầu tiên, chưa cần ai vào cấu hình gì.
/// </summary>
public interface ICmsSettingReader
{
    Task<IReadOnlyDictionary<string, string?>> GetAllAsync(CancellationToken ct = default);
}

public class CmsSettingReader : ICmsSettingReader
{
    private readonly IApplicationDbContext _db;

    public CmsSettingReader(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<string, string?>> GetAllAsync(CancellationToken ct = default)
    {
        var stored = await _db.CmsSettings.AsNoTracking()
            .ToDictionaryAsync(row => row.Key, row => row.Value, ct);

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in CmsSettingCatalog.All)
        {
            result[definition.Key] = stored.TryGetValue(definition.Key, out var value)
                                     && !string.IsNullOrWhiteSpace(value)
                ? value
                : definition.DefaultValue;
        }

        return result;
    }
}
