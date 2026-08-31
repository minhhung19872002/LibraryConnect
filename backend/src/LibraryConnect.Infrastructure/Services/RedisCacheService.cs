using System.Text.Json;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Redis-backed cache for catalogues, resolved permissions, system parameters and hot OPAC queries.
/// If Redis is unreachable the service transparently falls back to an in-process cache: a cache
/// outage must never take the library system down (24/7 operation requirement).
/// </summary>
public class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer? _redis;
    private readonly IMemoryCache _fallback;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IMemoryCache fallback,
        IOptions<RedisOptions> options,
        ILogger<RedisCacheService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _fallback = fallback;
        _options = options.Value;
        _logger = logger;
        _redis = redis;
    }

    private string Prefixed(string key) => _options.KeyPrefix + key;

    private IDatabase? Database
    {
        get
        {
            if (_redis is null || !_options.Enabled || !_redis.IsConnected)
            {
                return null;
            }

            return _redis.GetDatabase();
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var full = Prefixed(key);
        var db = Database;

        if (db is null)
        {
            return _fallback.TryGetValue(full, out T? cached) ? cached : default;
        }

        try
        {
            var value = await db.StringGetAsync(full);
            return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value!, SerializerOptions);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis read failed for {Key}, serving from the in-process cache", full);
            return _fallback.TryGetValue(full, out T? cached) ? cached : default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var full = Prefixed(key);
        var expiry = ttl ?? TimeSpan.FromMinutes(_options.DefaultTtlMinutes);

        _fallback.Set(full, value, expiry);

        var db = Database;
        if (db is null)
        {
            return;
        }

        try
        {
            await db.StringSetAsync(full, JsonSerializer.Serialize(value, SerializerOptions), expiry);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis write failed for {Key}; the in-process cache still holds the value", full);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        var full = Prefixed(key);
        _fallback.Remove(full);

        var db = Database;
        if (db is null)
        {
            return;
        }

        try
        {
            await db.KeyDeleteAsync(full);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis delete failed for {Key}", full);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var full = Prefixed(prefix);

        if (_redis is null || !_options.Enabled || !_redis.IsConnected)
        {
            // IMemoryCache cannot enumerate keys, so a prefix invalidation clears everything it holds.
            if (_fallback is MemoryCache memoryCache)
            {
                memoryCache.Clear();
            }

            return;
        }

        try
        {
            foreach (var endpoint in _redis.GetEndPoints())
            {
                var server = _redis.GetServer(endpoint);
                if (!server.IsConnected || server.IsReplica)
                {
                    continue;
                }

                foreach (var key in server.Keys(pattern: full + "*"))
                {
                    await _redis.GetDatabase().KeyDeleteAsync(key);
                    _fallback.Remove(key.ToString());
                }
            }
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis prefix invalidation failed for {Prefix}", full);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(ct);
        if (value is not null)
        {
            await SetAsync(key, value, ttl, ct);
        }

        return value;
    }
}

/// <summary>Cache key prefixes, kept together so invalidation never has to guess at a string.</summary>
public static class CacheKeys
{
    public const string Parameters = "params:";
    public const string Permissions = "perms:";
    public const string Catalogs = "catalog:";
    public const string Search = "search:";
    public const string CmsSettings = "cms:settings";
}
