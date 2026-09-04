using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Entities.Dig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LibraryConnect.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Xoá cache kết quả tra cứu OPAC (tiền tố <c>search:</c>) ngay khi biểu ghi, ĐKCB hay tài liệu số
/// thay đổi. Cache tra cứu chỉ sống 60 giây, nhưng "duyệt xong là bạn đọc tra ra ngay" là hành vi
/// được cam kết (kịch bản II.4) và ba phép thử hàng đợi biên mục đã đỏ khi chỉ dựa vào hạn giờ.
/// Làm ở interceptor thay vì ở từng handler để không handler nào quên.
/// </summary>
public class SearchCacheInvalidationInterceptor : SaveChangesInterceptor
{
    private static readonly Type[] SearchAffecting =
    {
        typeof(BibRecord), typeof(Item), typeof(DigitalDocument),
    };

    private readonly ICacheService _cache;
    private bool _pending;

    public SearchCacheInvalidationInterceptor(ICacheService cache) => _cache = cache;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        _pending = Touches(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        _pending = Touches(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (_pending)
        {
            _pending = false;
            _cache.RemoveByPrefixAsync(CacheKeyPrefixes.Search).GetAwaiter().GetResult();
        }

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (_pending)
        {
            _pending = false;
            await _cache.RemoveByPrefixAsync(CacheKeyPrefixes.Search, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private static bool Touches(DbContext? context) =>
        context is not null && context.ChangeTracker.Entries().Any(entry =>
            entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
            && SearchAffecting.Any(type => type.IsInstanceOfType(entry.Entity)));
}
