namespace LibraryConnect.Infrastructure.Configuration;

/// <summary>
/// JWT settings. All values come from the LC_ environment variables so a deployment never needs a
/// code change; see .env.example for the documented list.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "LibraryConnect";
    public string Audience { get; set; } = "LibraryConnect";
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 30;
}

public class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    public string DocumentsBucket { get; set; } = "lc-documents";
    public string ImagesBucket { get; set; } = "lc-images";
    public string BackupsBucket { get; set; } = "lc-backups";
}

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";
    /// <summary>Key prefix so several products can share one Redis instance.</summary>
    public string KeyPrefix { get; set; } = "lc:";
    public int DefaultTtlMinutes { get; set; } = 30;
    /// <summary>When Redis is unavailable the cache degrades to an in-memory one instead of failing.</summary>
    public bool Enabled { get; set; } = true;
}

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "LibraryConnect";
    public bool Enabled { get; set; }
}

public class BackupOptions
{
    public const string SectionName = "Backup";

    /// <summary>Directory inside the container, mounted to a host volume in docker-compose.</summary>
    public string Directory { get; set; } = "/var/lib/libraryconnect/backups";
    public string PgDumpPath { get; set; } = "pg_dump";
    public string PgRestorePath { get; set; } = "pg_restore";
    public string PsqlPath { get; set; } = "psql";
    public int KeepCount { get; set; } = 30;
    public string ScheduleCron { get; set; } = "0 2 * * *";
    public bool AutoEnabled { get; set; } = true;
    public bool IncludeObjectStorage { get; set; } = true;
}

/// <summary>
/// Firebase Cloud Messaging (Phase 15). Khai bằng biến môi trường <c>LC_Fcm__ProjectId</c> và
/// <c>LC_Fcm__ServiceAccountFile</c> (đường dẫn tệp JSON tài khoản dịch vụ, gắn vào container) hoặc
/// <c>LC_Fcm__ServiceAccountJson</c> (nội dung tệp). Để trống là không đẩy thông báo, không lỗi.
/// </summary>
public class FcmOptions
{
    public const string SectionName = "Fcm";

    public bool Enabled { get; set; } = true;
    public string ProjectId { get; set; } = string.Empty;
    public string ServiceAccountFile { get; set; } = string.Empty;
    public string ServiceAccountJson { get; set; } = string.Empty;
}
