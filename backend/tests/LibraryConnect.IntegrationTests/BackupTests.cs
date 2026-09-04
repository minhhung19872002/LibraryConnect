using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Admin.Backups;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Infrastructure.Configuration;
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

    [Fact]
    public async Task Phuc_hoi_tai_lai_tep_tai_lieu_so_chu_khong_chi_co_so_du_lieu()
    {
        // I.5 và mục 2.6 của E-HSMT: phục hồi phải trả hệ thống về đúng như lúc sao lưu. Trước
        // 04/09/2026 lượt phục hồi chỉ chạy pg_restore: cơ sở dữ liệu nói biểu ghi có toàn văn,
        // còn kho đối tượng trống, nên bạn đọc bấm đọc chỉ nhận lỗi. Hướng dẫn bảo tự chạy `mc
        // mirror` bằng tay — mà một bước bằng tay trong quy trình khẩn cấp là một bước bị quên.
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var options = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<
                LibraryConnect.Infrastructure.Configuration.MinioOptions>>().Value;
        var mirror = scope.ServiceProvider.GetRequiredService<IObjectStorageMirror>();

        var objectName = $"kiem-thu/phuc-hoi-{Guid.NewGuid():N}.txt";
        var content = System.Text.Encoding.UTF8.GetBytes("Toàn văn tài liệu số dùng để kiểm phục hồi");

        await storage.EnsureBucketAsync(options.DocumentsBucket);
        await storage.UploadAsync(options.DocumentsBucket, objectName, new MemoryStream(content), "text/plain");

        var folder = Path.Combine(Path.GetTempPath(), "lc-restore-" + Guid.NewGuid().ToString("N"));

        try
        {
            await mirror.MirrorAsync(folder, CancellationToken.None);

            // Kho đối tượng mất tệp — đúng cảnh sau một lượt phục hồi cơ sở dữ liệu.
            await storage.DeleteAsync(options.DocumentsBucket, objectName);
            (await storage.ExistsAsync(options.DocumentsBucket, objectName)).Should().BeFalse();

            var restored = await mirror.RestoreAsync(folder, CancellationToken.None);

            restored.Should().BeGreaterThan(0);
            (await storage.ExistsAsync(options.DocumentsBucket, objectName)).Should().BeTrue(
                "tệp phải quay lại đúng bucket và đúng tên object");

            await using var back = await storage.DownloadAsync(options.DocumentsBucket, objectName);
            using var buffer = new MemoryStream();
            await back.CopyToAsync(buffer);

            buffer.ToArray().Should().Equal(content, "nội dung tệp phải nguyên vẹn");
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            await storage.DeleteAsync(options.DocumentsBucket, objectName);
        }
    }

    [Fact]
    public async Task Sao_luu_kem_tep_tai_lieu_so_phai_chep_tep_that_chu_khong_chi_ghi_README()
    {
        // I.5: "Kèm backup file MinIO". Trước 04/09/2026 hàm sao lưu tệp chỉ ghi một README ghi tên
        // bucket và tổng dung lượng rồi trả 0 — giao diện vẫn có ô "Sao lưu kèm tệp tài liệu số".
        // Máy chạy kiểm thử có thể không có pg_dump nên thử thẳng bộ sao chép, không đi qua job.
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LibraryConnect.Infrastructure.Configuration.MinioOptions>>().Value;
        var mirror = scope.ServiceProvider.GetRequiredService<IObjectStorageMirror>();

        var objectName = $"kiem-thu/sao-luu-{Guid.NewGuid():N}.txt";
        var content = System.Text.Encoding.UTF8.GetBytes("Nội dung tệp tài liệu số để kiểm sao lưu");
        await storage.EnsureBucketAsync(options.DocumentsBucket);
        await storage.UploadAsync(options.DocumentsBucket, objectName, new MemoryStream(content), "text/plain");

        var folder = Path.Combine(Path.GetTempPath(), "lc-mirror-" + Guid.NewGuid().ToString("N"));
        try
        {
            var copied = await mirror.MirrorAsync(folder, CancellationToken.None);

            copied.Should().BeGreaterThan(0, "phải có ít nhất tệp vừa tải lên được chép về");
            var expected = Path.Combine(folder, options.DocumentsBucket, objectName.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(expected).Should().BeTrue("tệp phải nằm đúng đường dẫn <bucket>/<tên object>: " + expected);
            (await File.ReadAllBytesAsync(expected)).Should().Equal(content);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            await storage.DeleteAsync(options.DocumentsBucket, objectName);
        }
    }

    /// <summary>Mật khẩu hiện hành của tài khoản quản trị — bộ dựng đã đổi mật khẩu tạm ở lượt đăng nhập đầu.</summary>
    private string AdminPasswordNow() =>
        _factory.CurrentPassword(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

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

    // ------------------------------------------------------------------
    // Phục hồi (I.5) — cùng lớp lỗi với sao lưu: chạy lâu hơn giới hạn của proxy.
    //
    // Không phép thử nào dưới đây để lượt phục hồi thật chạy: `pg_restore` sẽ ghi đè chính cơ sở dữ
    // liệu mà cả bộ kiểm thử đang dùng chung. Mỗi phép thử dừng lại trước bước xếp việc.
    // ------------------------------------------------------------------

    [Fact]
    public async Task Restoring_with_the_wrong_password_is_refused_before_anything_is_queued()
    {
        var job = await SeedRestorableBackupAsync();
        var client = await AdminAsync();

        var response = await client.PostAsJsonAsync($"/api/admin/backups/{job}/restore",
            new { confirmPassword = "sai-mat-khau" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Sai mật khẩu thì không được đánh dấu là "đang phục hồi" — nếu không, lượt sau bị chặn oan.
        var status = await RestoreStatusAsync();
        (status?.State).Should().NotBe("Running");
    }

    [Fact]
    public async Task Restoring_a_backup_that_never_succeeded_is_refused()
    {
        Guid id;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var job = new BackupJob
            {
                Type = BackupType.Full,
                Status = BackupStatus.Failed,
                StartedAt = DateTimeOffset.UtcNow,
                FinishedAt = DateTimeOffset.UtcNow,
                TriggeredByName = "Phép thử"
            };

            db.BackupJobs.Add(job);
            await db.SaveChangesAsync(CancellationToken.None);
            id = job.Id;
        }

        var client = await AdminAsync();

        var response = await client.PostAsJsonAsync($"/api/admin/backups/{id}/restore",
            new { confirmPassword = AdminPasswordNow() });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_second_restore_is_refused_while_one_is_still_running()
    {
        var id = await SeedRestorableBackupAsync();
        await SetRestoreStatusAsync(new RestoreStatusDto
        {
            State = "Running",
            ArchiveName = "dang-chay.dump",
            StartedAt = DateTimeOffset.UtcNow
        });

        var client = await AdminAsync();

        var response = await client.PostAsJsonAsync($"/api/admin/backups/{id}/restore",
            new { confirmPassword = AdminPasswordNow() });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await ClearRestoreStatusAsync();
    }

    [Fact]
    public async Task The_restore_progress_is_readable_while_the_database_is_being_overwritten()
    {
        // Tiến độ nằm ngoài cơ sở dữ liệu, vì chính cơ sở dữ liệu đang bị ghi đè.
        await SetRestoreStatusAsync(new RestoreStatusDto
        {
            State = "Running",
            ArchiveName = "libraryconnect-full-20260903.dump",
            StartedAt = DateTimeOffset.UtcNow,
            StartedByName = "Quản trị viên"
        });

        var client = await AdminAsync();

        var response = await client.GetAsync("/api/admin/backups/restore-status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<RestoreStatusDto>>(
            LibraryConnectFactory.JsonOptions);

        payload!.Data!.State.Should().Be("Running");
        payload.Data.ArchiveName.Should().Be("libraryconnect-full-20260903.dump");

        await ClearRestoreStatusAsync();
    }

    /// <summary>Một bản sao lưu "thành công" kèm tệp thật trên đĩa, đủ điều kiện để phục hồi.</summary>
    private async Task<Guid> SeedRestorableBackupAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var options = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<BackupOptions>>().Value;

        Directory.CreateDirectory(options.Directory);
        var path = Path.Combine(options.Directory, $"kiem-thu-{Guid.NewGuid():N}.dump");
        await File.WriteAllTextAsync(path, "khong-phai-dump-that");

        var job = new BackupJob
        {
            Type = BackupType.Full,
            Status = BackupStatus.Success,
            FilePath = path,
            FileName = Path.GetFileName(path),
            SizeBytes = 20,
            StartedAt = DateTimeOffset.UtcNow,
            FinishedAt = DateTimeOffset.UtcNow,
            TriggeredByName = "Phép thử"
        };

        db.BackupJobs.Add(job);
        await db.SaveChangesAsync(CancellationToken.None);

        return job.Id;
    }

    private async Task SetRestoreStatusAsync(RestoreStatusDto status)
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cache.SetAsync(BackupRunner.RestoreStatusKey, status, TimeSpan.FromMinutes(10));
    }

    private async Task ClearRestoreStatusAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cache.RemoveAsync(BackupRunner.RestoreStatusKey);
    }

    private async Task<RestoreStatusDto?> RestoreStatusAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        return await cache.GetAsync<RestoreStatusDto>(BackupRunner.RestoreStatusKey);
    }

    [Fact]
    public async Task Doi_lich_sao_luu_tren_giao_dien_thi_viec_dinh_ky_doi_theo_ngay()
    {
        // I.5 nói lịch sao lưu tự động cấu hình được. Trước 04/09/2026 việc định kỳ chỉ được đăng ký
        // một lần lúc máy chủ khởi động: đổi giờ trên màn hình Tham số chỉ ghi vào cơ sở dữ liệu,
        // còn bộ chạy nền giữ nguyên giờ cũ tới lần khởi động lại — mà màn hình vẫn hiện giờ mới.
        var client = await AdminAsync();

        var cron = $"{DateTime.UtcNow.Second % 60} 3 * * *";

        (await client.PutAsJsonAsync("/api/admin/parameters", new
        {
            parameters = new[]
            {
                new { key = "BACKUP.AUTO_ENABLED", value = "true" },
                new { key = "BACKUP.SCHEDULE_CRON", value = cron }
            }
        })).EnsureSuccessStatusCode();

        var storage = (await client.GetFromJsonAsync<ApiResponse<BackupStorageDto>>(
            "/api/admin/backups/storage", LibraryConnectFactory.JsonOptions))!.Data!;

        storage.ScheduleCron.Should().Be(cron);
        storage.ScheduledCron.Should().Be(cron,
            "việc định kỳ phải được đăng ký lại ngay, không đợi tới lần khởi động sau");

        // Tắt sao lưu tự động thì việc phải biến mất khỏi bộ chạy nền, không chỉ đổi một dòng dữ liệu.
        (await client.PutAsJsonAsync("/api/admin/parameters", new
        {
            parameters = new[] { new { key = "BACKUP.AUTO_ENABLED", value = "false" } }
        })).EnsureSuccessStatusCode();

        var off = (await client.GetFromJsonAsync<ApiResponse<BackupStorageDto>>(
            "/api/admin/backups/storage", LibraryConnectFactory.JsonOptions))!.Data!;

        off.ScheduledCron.Should().BeNull("tắt rồi thì không còn việc định kỳ nào");

        // Trả lại như cũ để các kịch bản khác không bị ảnh hưởng.
        (await client.PutAsJsonAsync("/api/admin/parameters", new
        {
            parameters = new[]
            {
                new { key = "BACKUP.AUTO_ENABLED", value = "true" },
                new { key = "BACKUP.SCHEDULE_CRON", value = "0 2 * * *" }
            }
        })).EnsureSuccessStatusCode();
    }
}
