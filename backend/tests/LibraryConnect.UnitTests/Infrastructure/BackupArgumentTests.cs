using FluentAssertions;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Infrastructure.Services;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Dòng lệnh gửi cho pg_dump và pg_restore.
///
/// Hangfire giữ hàng đợi việc trong schema `hangfire` của **chính cơ sở dữ liệu này**. Nếu bản sao
/// lưu ôm luôn schema ấy thì hai chuyện hỏng cùng lúc: phục hồi bản hôm qua làm sống lại hàng đợi
/// của hôm qua và chạy lại những việc đã chạy rồi; và lượt phục hồi không thể tự chạy trong Hangfire
/// vì nó xoá đúng cái bảng đang ghi nhận nó.
/// </summary>
public class BackupArgumentTests
{
    private static readonly PostgresConnectionInfo Connection =
        new("db", 5432, "libraryconnect", "libraryconnect");

    [Theory]
    [InlineData(BackupType.Full)]
    [InlineData(BackupType.DataOnly)]
    public void The_dump_leaves_the_job_queue_out(BackupType type)
    {
        var arguments = PostgresBackupService.BuildDumpArguments(Connection, type, "/backup/a.dump");

        arguments.Should().Contain("--exclude-schema=hangfire");
    }

    [Fact]
    public void The_restore_leaves_the_job_queue_alone()
    {
        var arguments = PostgresBackupService.BuildRestoreArguments(Connection, "/backup/a.dump");

        // Bản sao lưu cũ vẫn còn schema ấy bên trong, nên phía phục hồi cũng phải loại ra.
        arguments.Should().Contain("--exclude-schema=hangfire");
    }

    [Fact]
    public void The_restore_stays_all_or_nothing()
    {
        var arguments = PostgresBackupService.BuildRestoreArguments(Connection, "/backup/a.dump");

        arguments.Should().Contain("--single-transaction").And.Contain("--exit-on-error");
    }

    [Fact]
    public void Only_a_data_only_dump_passes_the_data_only_flag()
    {
        PostgresBackupService.BuildDumpArguments(Connection, BackupType.DataOnly, "/x.dump")
            .Should().Contain("--data-only");

        PostgresBackupService.BuildDumpArguments(Connection, BackupType.Full, "/x.dump")
            .Should().NotContain("--data-only");
    }
}
