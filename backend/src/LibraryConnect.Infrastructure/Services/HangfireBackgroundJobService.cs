using System.Linq.Expressions;
using Hangfire;
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

    public HangfireBackgroundJobService(IBackgroundJobClient client, IRecurringJobManager recurring)
    {
        _client = client;
        _recurring = recurring;
    }

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) => _client.Enqueue(methodCall);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) =>
        _client.Schedule(methodCall, delay);

    public void AddOrUpdateRecurring<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression) =>
        _recurring.AddOrUpdate(jobId, methodCall, cronExpression, new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Local
        });

    public void RemoveRecurring(string jobId) => _recurring.RemoveIfExists(jobId);
}
