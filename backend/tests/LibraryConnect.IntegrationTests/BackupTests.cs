using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Admin.Backups;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// E-HSMT mục 2.6 — sao lưu (I.5).
///
/// Lỗi H9: lượt sao lưu thủ công chạy thẳng trong lượt HTTP. Kho lớn kèm kho đối tượng vài GB vượt
/// 300 giây của proxy, việc bị bỏ dở và dòng nhật ký kẹt "Đang chạy" vĩnh viễn — đúng lớp lỗi số 4
/// của CLAUDE.md. Ba phép thử dưới đây giữ cho nó ở lại hàng đợi.
/// </summary>
[Collection(ApiCollection.Name)]
public class BackupTests
{
    private readonly LibraryConnectFactory _factory;

    public BackupTests(LibraryConnectFactory factory) => _factory = factory;

    [Fact]
    public async Task A_backup_request_returns_a_queued_job_instead_of_waiting_for_pg_dump()
    {
        await ClearBackupJobsAsync();
        var client = await AdminAsync();

        var response = await client.PostAsJsonAsync("/api/admin/backups",
            new { type = nameof(BackupType.DataOnly), includeObjectStorage = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<BackupJobDto>>(LibraryConnectFactory.JsonOptions);
        payload!.Success.Should().BeTrue(payload.Message);

        var job = payload.Data!;
        job.Status.Should().Be(BackupStatus.Pending,
            "lượt gọi phải trả về ngay khi đã xếp việc, không đứng chờ pg_dump chạy xong");
        job.FinishedAt.Should().BeNull();
        job.StatusLabel.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task A_second_backup_request_is_refused_while_one_is_still_queued()
    {
        await ClearBackupJobsAsync();
        await SeedRunningBackupAsync();
        var client = await AdminAsync();

        var response = await client.PostAsJsonAsync("/api/admin/backups",
            new { type = nameof(BackupType.DataOnly), includeObjectStorage = false });

        // Hai pg_dump chạy song song ghi cùng một thư mục và tranh nhau tài nguyên của máy chủ.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<BackupJobDto>>(
            LibraryConnectFactory.JsonOptions);
        payload!.Message.Should().Contain("sao lưu");
    }

    [Fact]
    public async Task The_queued_backup_finishes_on_its_own()
    {
        await ClearBackupJobsAsync();
        var client = await AdminAsync();

        var created = await client.PostAsJsonAsync("/api/admin/backups",
            new { type = nameof(BackupType.DataOnly), includeObjectStorage = false });
        created.EnsureSuccessStatusCode();

        var id = (await created.Content.ReadFromJsonAsync<ApiResponse<BackupJobDto>>(LibraryConnectFactory.JsonOptions))!.Data!.Id;

        // Trạng thái cuối là gì không quan trọng ở đây: máy chạy kiểm thử có thể không có pg_dump.
        // Điều cần chứng minh là **có người chạy nó** — dòng nhật ký không nằm mãi ở "đã xếp hàng".
        var job = await WaitForTerminalAsync(id, TimeSpan.FromMinutes(2));

        job.Status.Should().BeOneOf(BackupStatus.Success, BackupStatus.Failed);
        job.FinishedAt.Should().NotBeNull();
    }

    private Task<HttpClient> AdminAsync() => _factory.CreateAuthenticatedClientAsync(
        LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    /// <summary>Bộ kiểm thử dùng chung một cơ sở dữ liệu; khoá chống chạy trùng đọc mọi dòng đang mở.</summary>
    private async Task ClearBackupJobsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var open = await db.BackupJobs.ToListAsync();
        foreach (var job in open)
        {
            job.Status = BackupStatus.Failed;
            job.FinishedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task SeedRunningBackupAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        db.BackupJobs.Add(new BackupJob
        {
            Type = BackupType.Full,
            Status = BackupStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            IsAuto = false,
            TriggeredByName = "Phép thử"
        });

        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<BackupJob> WaitForTerminalAsync(Guid id, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var job = await db.BackupJobs.AsNoTracking().FirstAsync(entity => entity.Id == id);
            if (job.Status is BackupStatus.Success or BackupStatus.Failed)
            {
                return job;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new Xunit.Sdk.XunitException(
            $"Bản sao lưu {id} vẫn chưa xong sau {timeout.TotalSeconds:N0} giây — không ai nhặt việc khỏi hàng đợi.");
    }
}
