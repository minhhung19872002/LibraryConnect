using System.Linq.Expressions;
using Hangfire;
using Hangfire.Storage;
using LibraryConnect.Application.Common.Interfaces;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Thin wrapper over Hangfire so use-case handlers can queue work without depending on it directly.
/// Long imports, backups, OAI harvests and the nightly overdue calculation all go through here.
/// </summary>
public class HangfireBackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _client;
    private readonly IRecurringJobManager _recurring;
    private readonly JobStorage _storage;

    public HangfireBackgroundJobService(
        IBackgroundJobClient client, IRecurringJobManager recurring, JobStorage storage)
    {
        _client = client;
        _recurring = recurring;
        _storage = storage;
    }

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) => _client.Enqueue(methodCall);

    public BackgroundJobState? GetState(string jobId)
    {
        var details = _storage.GetMonitoringApi().JobDetails(jobId);

        // Hangfire xoá bản ghi sau khi hết hạn lưu; lúc ấy coi như không còn biết gì.
        var last = details?.History?.FirstOrDefault();

        return last is null
            ? null
            : new BackgroundJobState(last.StateName, last.Reason, last.CreatedAt);
    }

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) =>
        _client.Schedule(methodCall, delay);

    public void AddOrUpdateRecurring<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression) =>
        _recurring.AddOrUpdate(jobId, methodCall, cronExpression, new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Local
        });

    public void RemoveRecurring(string jobId) => _recurring.RemoveIfExists(jobId);

    public string? GetRecurringCron(string jobId)
    {
        using var connection = _storage.GetConnection();

        // Hangfire giữ việc định kỳ trong một hash tên "recurring-job:<id>"; khoá "Cron" là lịch.
        var hash = connection.GetAllEntriesFromHash($"recurring-job:{jobId}");

        return hash is not null && hash.TryGetValue("Cron", out var cron) ? cron : null;
    }
}
