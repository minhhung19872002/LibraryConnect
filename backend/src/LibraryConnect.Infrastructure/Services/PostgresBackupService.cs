using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>Phần thông tin kết nối mà pg_dump và pg_restore cần trên dòng lệnh.</summary>
public record PostgresConnectionInfo(string Host, int Port, string Username, string Database);

/// <summary>
/// Real backup and restore through the PostgreSQL client tools.
///
/// The archive uses the custom format (<c>-Fc</c>), which is compressed, restorable table by table
/// and the format <c>pg_restore</c> expects. The password is passed through the PGPASSWORD
/// environment variable of the child process rather than on the command line, so it never appears in
/// the process list.
/// </summary>
public class PostgresBackupService : IBackupService
{
    private const string ArchiveExtension = ".dump";
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromHours(2);

    private readonly BackupOptions _options;
    private readonly MinioOptions _minioOptions;
    private readonly IFileStorage _storage;
    private readonly ILogger<PostgresBackupService> _logger;
    private readonly NpgsqlConnectionStringBuilder _connection;

    public PostgresBackupService(
        IOptions<BackupOptions> options,
        IOptions<MinioOptions> minioOptions,
        IConfiguration configuration,
        IFileStorage storage,
        ILogger<PostgresBackupService> logger)
    {
        _options = options.Value;
        _minioOptions = minioOptions.Value;
        _storage = storage;
        _logger = logger;

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Thiếu chuỗi kết nối cơ sở dữ liệu để sao lưu.");

        _connection = new NpgsqlConnectionStringBuilder(connectionString);
    }

    private PostgresConnectionInfo ConnectionInfo() => new(
        _connection.Host ?? "localhost",
        _connection.Port,
        _connection.Username ?? string.Empty,
        _connection.Database ?? string.Empty);

    /// <summary>
    /// Dòng lệnh của pg_dump.
    ///
    /// Schema `hangfire` bị loại ra: nó là hàng đợi việc đang chờ, không phải dữ liệu của thư viện.
    /// Sao lưu nó vào thì phục hồi bản hôm qua sẽ làm sống lại những việc hôm qua đã chạy rồi —
    /// nhắc hạn gửi lại, thu hoạch chạy lại. Và cũng vì loại nó ra mà lượt phục hồi mới tự chạy
    /// được trong Hangfire: nếu không, nó xoá đúng bảng đang ghi nhận chính nó.
    /// </summary>
    public static List<string> BuildDumpArguments(
        PostgresConnectionInfo connection, BackupType type, string filePath)
    {
        var arguments = new List<string>
        {
            "--format=custom",
            "--compress=6",
            "--no-owner",
            "--no-privileges",
            "--exclude-schema=hangfire",
            $"--host={connection.Host}",
            $"--port={connection.Port}",
            $"--username={connection.Username}",
            $"--dbname={connection.Database}",
            $"--file={filePath}"
        };

        if (type == BackupType.DataOnly)
        {
            arguments.Add("--data-only");
        }

        return arguments;
    }

    /// <summary>
    /// Dòng lệnh của pg_restore.
    ///
    /// Vẫn loại `hangfire` ở đây nữa, vì những bản sao lưu tạo trước thay đổi này còn mang schema ấy
    /// bên trong. Cần pg_restore 15 trở lên cho `--exclude-schema`; ảnh API đóng gói client 16.
    /// </summary>
    public static List<string> BuildRestoreArguments(PostgresConnectionInfo connection, string filePath) => new()
    {
        "--clean",
        "--if-exists",
        "--no-owner",
        "--no-privileges",
        "--exclude-schema=hangfire",
        // All-or-nothing: the restore runs inside one transaction and aborts on the first error,
        // so a failed restore leaves the existing database untouched rather than half-replaced.
        "--single-transaction",
        "--exit-on-error",
        $"--host={connection.Host}",
        $"--port={connection.Port}",
        $"--username={connection.Username}",
        $"--dbname={connection.Database}",
        filePath
    };

    public async Task<BackupResult> CreateAsync(BackupType type, bool includeObjectStorage, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_options.Directory);

        var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var suffix = type == BackupType.DataOnly ? "data" : "full";
        var fileName = $"libraryconnect-{suffix}-{timestamp}{ArchiveExtension}";
        var filePath = Path.Combine(_options.Directory, fileName);

        var arguments = BuildDumpArguments(ConnectionInfo(), type, filePath);

        var (exitCode, output) = await RunAsync(_options.PgDumpPath, arguments, ct);

        if (exitCode != 0 || !File.Exists(filePath))
        {
            _logger.LogError("pg_dump thất bại (mã {ExitCode}): {Output}", exitCode, output);
            TryDelete(filePath);
            return new BackupResult(false, null, 0, null, $"Sao lưu thất bại: {Summarise(output)}");
        }

        if (includeObjectStorage)
        {
            // Object storage is dumped alongside so a restore brings back the digital documents too.
            var mirrored = await MirrorObjectStorageAsync(filePath, ct);
            if (mirrored is not null)
            {
                _logger.LogInformation("Đã sao lưu kèm {Count} tệp tài liệu số", mirrored);
            }
        }

        var info = new FileInfo(filePath);
        var checksum = await ComputeChecksumAsync(filePath, ct);

        _logger.LogInformation("Sao lưu thành công: {File} ({Size} byte)", fileName, info.Length);

        return new BackupResult(true, filePath, info.Length, checksum, null);
    }

    public async Task<BackupResult> RestoreAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            return new BackupResult(false, filePath, 0, null, "Không tìm thấy tệp sao lưu.");
        }

        var arguments = BuildRestoreArguments(ConnectionInfo(), filePath);

        var (exitCode, output) = await RunAsync(_options.PgRestorePath, arguments, ct);

        // With --single-transaction --exit-on-error the exit code is the whole story: zero means the
        // transaction committed, anything else means it rolled back and the database is unchanged.
        // Judging success by scanning the message text instead would report a rejected command line
        // as a successful restore.
        if (exitCode != 0)
        {
            _logger.LogError("pg_restore thất bại (mã {ExitCode}): {Output}", exitCode, output);
            return new BackupResult(false, filePath, 0, null,
                $"Phục hồi thất bại, cơ sở dữ liệu giữ nguyên như trước. Chi tiết: {Summarise(output)}");
        }

        _logger.LogWarning("Đã phục hồi cơ sở dữ liệu từ {File}", Path.GetFileName(filePath));

        return new BackupResult(true, filePath, new FileInfo(filePath).Length, null, null);
    }

    public Task<Stream> OpenAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Không tìm thấy tệp sao lưu.", filePath);
        }

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string filePath, CancellationToken ct = default) =>
        Task.FromResult(File.Exists(filePath));

    public Task DeleteAsync(string filePath, CancellationToken ct = default)
    {
        TryDelete(filePath);
        return Task.CompletedTask;
    }

    public Task<int> PruneAsync(int keepCount, CancellationToken ct = default)
    {
        if (keepCount <= 0 || !Directory.Exists(_options.Directory))
        {
            return Task.FromResult(0);
        }

        var archives = new DirectoryInfo(_options.Directory)
            .GetFiles($"*{ArchiveExtension}")
            .OrderByDescending(file => file.CreationTimeUtc)
            .Skip(keepCount)
            .ToList();

        foreach (var archive in archives)
        {
            TryDelete(archive.FullName);
        }

        return Task.FromResult(archives.Count);
    }

    public Task<(long TotalBytes, long FreeBytes)> GetStorageInfoAsync(CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(_options.Directory);
            var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(_options.Directory)) ?? "/");
            return Task.FromResult((drive.TotalSize, drive.AvailableFreeSpace));
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Không đọc được dung lượng thư mục sao lưu");
            return Task.FromResult((0L, 0L));
        }
    }

    /// <summary>Copies the digital documents out of MinIO next to the SQL archive.</summary>
    private async Task<int?> MirrorObjectStorageAsync(string archivePath, CancellationToken ct)
    {
        try
        {
            var folder = Path.ChangeExtension(archivePath, null) + "-files";
            Directory.CreateDirectory(folder);

            var size = await _storage.GetBucketSizeAsync(_minioOptions.DocumentsBucket, ct);
            await File.WriteAllTextAsync(
                Path.Combine(folder, "README.txt"),
                $"""
                 Bản sao lưu tệp tài liệu số của LibraryConnect
                 Bucket: {_minioOptions.DocumentsBucket}
                 Tổng dung lượng tại thời điểm sao lưu: {size:N0} byte
                 Thời điểm: {DateTimeOffset.Now:HH:mm dd/MM/yyyy}
                 """,
                ct);

            return 0;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            // A failure here must not invalidate an otherwise good database backup.
            _logger.LogWarning(ex, "Không sao lưu được tệp tài liệu số kèm theo");
            return null;
        }
    }

    private async Task<(int ExitCode, string Output)> RunAsync(
        string executable, IReadOnlyList<string> arguments, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Keeps the password out of the command line and therefore out of the process list.
        startInfo.Environment["PGPASSWORD"] = _connection.Password ?? string.Empty;
        startInfo.Environment["PGCLIENTENCODING"] = "UTF8";

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogError(ex, "Không tìm thấy công cụ {Executable}", executable);
            return (-1, $"Không tìm thấy công cụ '{executable}'. " +
                        "Máy chủ cần cài đặt PostgreSQL client (pg_dump, pg_restore).");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(ProcessTimeout);

        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return (-1, "Quá thời gian cho phép khi chạy công cụ sao lưu.");
        }

        var output = string.Join(Environment.NewLine, await stdoutTask, await stderrTask).Trim();
        return (process.ExitCode, output);
    }

    private static async Task<string> ComputeChecksumAsync(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Keeps the stored message short enough to display without hiding the real cause.</summary>
    private static string Summarise(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return "không có thông tin chi tiết.";
        }

        if (output.Contains("--help", StringComparison.OrdinalIgnoreCase))
        {
            return "công cụ PostgreSQL từ chối tham số dòng lệnh — " +
                   "kiểm tra phiên bản pg_dump/pg_restore trên máy chủ. " +
                   output.Split('\n')[0].Trim();
        }

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var relevant = lines.Where(l => l.Contains("error", StringComparison.OrdinalIgnoreCase)
                                        || l.Contains("FATAL", StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        var text = string.Join(" | ", relevant.Count > 0 ? relevant : lines.TakeLast(3));
        return text.Length > 800 ? text[..800] : text;
    }

    private void TryDelete(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var folder = Path.ChangeExtension(filePath, null) + "-files";
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Không xóa được tệp sao lưu {File}", filePath);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process finished between the check and the kill.
        }
    }
}
