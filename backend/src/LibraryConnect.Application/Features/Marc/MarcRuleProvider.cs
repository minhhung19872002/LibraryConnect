using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Marc;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Marc;

/// <summary>
/// Cung cấp bộ quy tắc MARC đang hiệu lực cho bộ kiểm tra biểu ghi.
/// </summary>
public interface IMarcRuleProvider
{
    Task<MarcValidator> GetValidatorAsync(CancellationToken ct = default);

    /// <summary>Xóa bộ nhớ đệm sau khi cán bộ sửa định nghĩa trường.</summary>
    Task InvalidateAsync(CancellationToken ct = default);
}

/// <summary>
/// Đọc định nghĩa trường từ cơ sở dữ liệu và giữ trong bộ nhớ đệm.
///
/// Every record saved and every record imported is validated against these rules, so reading 220
/// rows on each one would be wasteful. The definitions change only when a librarian edits them, and
/// that path clears the cache, so a short time to live is enough to cover a second application
/// instance that did not handle the edit itself.
/// </summary>
public class MarcRuleProvider : IMarcRuleProvider
{
    private const string CacheKey = CacheKeyPrefixes.MarcRules + "all";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private readonly IApplicationDbContext _db;
    private readonly ICacheService _cache;

    public MarcRuleProvider(IApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<MarcValidator> GetValidatorAsync(CancellationToken ct = default)
    {
        var rules = await _cache.GetOrCreateAsync(CacheKey, LoadAsync, Ttl, ct);

        return new MarcValidator(rules);
    }

    public Task InvalidateAsync(CancellationToken ct = default) => _cache.RemoveByPrefixAsync(CacheKeyPrefixes.MarcRules, ct);

    private async Task<List<MarcFieldRule>> LoadAsync(CancellationToken ct)
    {
        var definitions = await _db.MarcFieldDefinitions
            .AsNoTracking()
            .Where(field => field.IsActive)
            .OrderBy(field => field.Tag)
            .ToListAsync(ct);

        return definitions.Select(MarcFieldMapping.ToRule).ToList();
    }
}
