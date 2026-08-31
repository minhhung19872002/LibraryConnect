using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cms;
using MediatR;

namespace LibraryConnect.Application.Features.Public;

/// <summary>
/// Branding and contact details of the library operating this installation. Read by the admin shell
/// and by every OPAC page, and open to anonymous callers because the OPAC renders it before login.
///
/// Everything comes from sys.system_parameters — the product ships with no customer name compiled in.
/// </summary>
public record GetPublicSettingsQuery : IRequest<PublicSettingsDto>;

public class PublicSettingsDto
{
    public string LibraryName { get; set; } = string.Empty;
    public string? LibraryNameEn { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    /// <summary>Product attribution in the OPAC footer, switchable per customer.</summary>
    public bool ShowPoweredBy { get; set; }
    public int OpacPageSize { get; set; }
    public bool AllowHold { get; set; }
    public bool AllowReview { get; set; }

    // ---- Cấu hình riêng của trang tra cứu, lấy từ web.cms_settings (VIII.1) ----
    public string? Slogan { get; set; }
    public string? FaviconUrl { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? FooterText { get; set; }
    /// <summary>Giờ mở cửa, mỗi cơ sở một dòng.</summary>
    public string? OpeningHours { get; set; }
    public string? ContactNote { get; set; }
    public string? MapEmbedUrl { get; set; }
    public string? Facebook { get; set; }
    public string? Youtube { get; set; }
    public string? Zalo { get; set; }
    public int NewsPerPage { get; set; } = 9;
    public bool ShowNewBooks { get; set; } = true;
    public bool ShowPopularBooks { get; set; } = true;
    public bool ShowInterlibrary { get; set; } = true;
}

public class GetPublicSettingsQueryHandler : IRequestHandler<GetPublicSettingsQuery, PublicSettingsDto>
{
    private const string DefaultLibraryName = "Thư viện";

    private readonly ISystemParameterService _parameters;
    private readonly ICmsSettingReader _site;

    public GetPublicSettingsQueryHandler(ISystemParameterService parameters, ICmsSettingReader site)
    {
        _parameters = parameters;
        _site = site;
    }

    public async Task<PublicSettingsDto> Handle(GetPublicSettingsQuery request, CancellationToken ct)
    {
        var name = await _parameters.GetAsync(ParameterKeysBridge.LibraryName, ct);
        var site = await _site.GetAllAsync(ct);

        string? Text(string key) =>
            site.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

        bool Flag(string key, bool fallback) =>
            bool.TryParse(Text(key), out var parsed) ? parsed : fallback;

        return new PublicSettingsDto
        {
            LibraryName = string.IsNullOrWhiteSpace(name) ? DefaultLibraryName : name,
            LibraryNameEn = await _parameters.GetAsync(ParameterKeysBridge.LibraryNameEn, ct),
            Address = await _parameters.GetAsync(ParameterKeysBridge.LibraryAddress, ct),
            Phone = await _parameters.GetAsync(ParameterKeysBridge.LibraryPhone, ct),
            Email = await _parameters.GetAsync(ParameterKeysBridge.LibraryEmail, ct),
            Website = await _parameters.GetAsync(ParameterKeysBridge.LibraryWebsite, ct),
            LogoUrl = await _parameters.GetAsync(ParameterKeysBridge.LibraryLogoUrl, ct),
            ShowPoweredBy = await _parameters.GetAsync(ParameterKeysBridge.OpacShowPoweredBy, true, ct),
            OpacPageSize = await _parameters.GetAsync(ParameterKeysBridge.OpacPageSize, 20, ct),
            AllowHold = await _parameters.GetAsync(ParameterKeysBridge.OpacAllowHold, true, ct),
            AllowReview = await _parameters.GetAsync(ParameterKeysBridge.OpacAllowReview, false, ct),
            Slogan = Text(CmsSettingCatalog.Slogan),
            FaviconUrl = Text(CmsSettingCatalog.FaviconUrl),
            HeroImageUrl = Text(CmsSettingCatalog.HeroImageUrl),
            FooterText = Text(CmsSettingCatalog.FooterText),
            OpeningHours = Text(CmsSettingCatalog.OpeningHours),
            ContactNote = Text(CmsSettingCatalog.ContactNote),
            MapEmbedUrl = Text(CmsSettingCatalog.MapEmbedUrl),
            Facebook = Text(CmsSettingCatalog.FacebookUrl),
            Youtube = Text(CmsSettingCatalog.YoutubeUrl),
            Zalo = Text(CmsSettingCatalog.ZaloUrl),
            NewsPerPage = int.TryParse(Text(CmsSettingCatalog.NewsPerPage), out var perPage)
                ? Math.Clamp(perPage, 3, 60)
                : 9,
            ShowNewBooks = Flag(CmsSettingCatalog.ShowNewBooks, true),
            ShowPopularBooks = Flag(CmsSettingCatalog.ShowPopularBooks, true),
            ShowInterlibrary = Flag(CmsSettingCatalog.ShowInterlibrary, true)
        };
    }
}

/// <summary>
/// The parameter key constants live in the Infrastructure layer next to their service; repeating the
/// few the Application layer needs here keeps the dependency direction clean.
/// </summary>
internal static class ParameterKeysBridge
{
    public const string LibraryName = "LIBRARY.NAME";
    public const string LibraryNameEn = "LIBRARY.NAME_EN";
    public const string LibraryAddress = "LIBRARY.ADDRESS";
    public const string LibraryPhone = "LIBRARY.PHONE";
    public const string LibraryEmail = "LIBRARY.EMAIL";
    public const string LibraryWebsite = "LIBRARY.WEBSITE";
    public const string LibraryLogoUrl = "LIBRARY.LOGO_URL";
    public const string OpacShowPoweredBy = "OPAC.SHOW_POWERED_BY";
    public const string OpacPageSize = "OPAC.PAGE_SIZE";
    public const string OpacAllowHold = "OPAC.ALLOW_HOLD";
    public const string OpacAllowReview = "OPAC.ALLOW_REVIEW";
}
