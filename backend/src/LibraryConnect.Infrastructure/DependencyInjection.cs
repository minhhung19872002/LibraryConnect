using Hangfire;
using Hangfire.PostgreSql;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Admin.AuditLogs;
using LibraryConnect.Infrastructure.Configuration;
using LibraryConnect.Infrastructure.Jobs;
using LibraryConnect.Infrastructure.Persistence;
using LibraryConnect.Infrastructure.Persistence.Interceptors;
using LibraryConnect.Infrastructure.Persistence.Seeding;
using LibraryConnect.Infrastructure.Services;
using LibraryConnect.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace LibraryConnect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<BackupOptions>(configuration.GetSection(BackupOptions.SectionName));
        services.Configure<FcmOptions>(configuration.GetSection(FcmOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Thiếu chuỗi kết nối cơ sở dữ liệu. Hãy đặt biến môi trường LC_DB_HOST / LC_DB_NAME / LC_DB_USER / LC_DB_PASSWORD.");

        AddPersistence(services, connectionString);
        AddCaching(services, configuration);
        AddObjectStorage(services);
        AddInterLibrary(services);
        AddBackgroundJobs(services, connectionString, configuration);

        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        services.AddScoped<ISystemParameterService, SystemParameterService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICodeGenerator, CodeGenerator>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<INotificationSender, ReaderNotificationSender>();
        services.AddScoped<IPushSender, FcmPushSender>();
        services.AddSingleton<IImageResizer, SkiaImageResizer>();
        services.AddHttpClient("fcm", client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();
        services.AddScoped<IBackupService, PostgresBackupService>();
        services.AddScoped<IObjectStorageMirror, ObjectStorageMirror>();
        services.AddSingleton<IHtmlSanitizer, HtmlSanitizerService>();
        services.AddScoped<Application.Features.Cataloging.ICustomIndexHarvester, CustomIndexHarvester>();
        services.AddScoped<Application.Features.Cataloging.ICoverImageFinder, CoverImageFinder>();

        services.AddReporting();

        // One instance serves both roles: the interceptor reads through IAuditSettingsCache and the
        // settings screen drops the cache through IAuditSettingsInvalidator.
        services.AddSingleton<AuditSettingsCache>();
        services.AddSingleton<IAuditSettingsCache>(p => p.GetRequiredService<AuditSettingsCache>());
        services.AddSingleton<IAuditSettingsInvalidator>(p => p.GetRequiredService<AuditSettingsCache>());
        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    private static void AddPersistence(IServiceCollection services, string connectionString)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<AuditLogInterceptor>();
        services.AddScoped<SearchCacheInvalidationInterceptor>();

        services.AddDbContext<LibraryConnectDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "sys");
                npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            });

            options.AddInterceptors(
                provider.GetRequiredService<AuditableEntityInterceptor>(),
                provider.GetRequiredService<AuditLogInterceptor>(),
                provider.GetRequiredService<SearchCacheInvalidationInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<LibraryConnectDbContext>());
    }

    /// <summary>
    /// Hangfire runs the long jobs: imports, nightly overdue calculation, reminder emails, automatic
    /// backups and OAI harvesting. Its own schema lives beside the business data so one pg_dump
    /// captures everything.
    /// </summary>
    private static void AddBackgroundJobs(IServiceCollection services, string connectionString, IConfiguration configuration)
    {
        // A deployment may run several API instances behind the load balancer while only one of
        // them processes jobs; Hangfire:ServerEnabled turns the worker off on the others.
        var serverEnabled = configuration.GetValue("Hangfire:ServerEnabled", true);

        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                postgres => postgres.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire",
                    PrepareSchemaIfNecessary = true,
                    QueuePollInterval = TimeSpan.FromSeconds(15)
                }));

        services.AddScoped<SystemMaintenanceJobs>();
        // Gói "Xuất toàn bộ dữ liệu hệ thống" (V.3) — ở tầng hạ tầng vì cần thư mục sao lưu.
        services.AddScoped<LibraryConnect.Application.Features.Digital.IFullSystemExportRunner, FullSystemExportRunner>();

        if (serverEnabled)
        {
            services.AddHangfireServer(options =>
            {
                options.ServerName = $"libraryconnect-{Environment.MachineName}";
                options.WorkerCount = Math.Min(Environment.ProcessorCount * 2, 8);
            });

            // Only the instance that actually processes jobs registers the schedules.
            services.AddHostedService<RecurringJobRegistrar>();
        }
    }

    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        var redisConnection = configuration.GetSection(RedisOptions.SectionName)["ConnectionString"];
        var enabled = configuration.GetSection(RedisOptions.SectionName).GetValue("Enabled", true);

        if (enabled && !string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<RedisCacheService>>();
                var config = ConfigurationOptions.Parse(redisConnection);
                config.AbortOnConnectFail = false;
                config.ConnectRetry = 3;
                config.ConnectTimeout = 5000;

                try
                {
                    return ConnectionMultiplexer.Connect(config);
                }
                catch (RedisConnectionException ex)
                {
                    // The cache is an optimisation, not a dependency: start anyway and degrade to
                    // the in-process cache until Redis comes back.
                    logger.LogWarning(ex, "Không kết nối được Redis, hệ thống chuyển sang cache nội bộ");
                    return ConnectionMultiplexer.Connect(config);
                }
            });
        }

        services.AddScoped<ICacheService, RedisCacheService>();
    }

    private static void AddObjectStorage(IServiceCollection services)
    {
        // The provider decides whether a usable client can be built; nothing else in the graph needs
        // to know, and a missing configuration no longer breaks unrelated screens.
        services.AddSingleton<MinioClientProvider>();
        services.AddScoped<IFileStorage, MinioFileStorage>();

        // Xử lý tệp tài liệu số: đếm trang, rút chữ, kết xuất trang thành ảnh, nhận dạng ký tự.
        services.AddScoped<IDocumentProcessor, DocumentProcessor>();
    }

    /// <summary>
    /// Liên thư viện: tra cứu sang thư viện bạn, thu hoạch OAI-PMH và máy chủ Z39.50 của mình.
    ///
    /// Một bộ gọi HTTP riêng cho phần này vì máy chủ ngoài hay chậm và hay hỏng: đặt thời gian chờ
    /// rộng tay ở đây không được kéo theo phần còn lại của hệ thống.
    /// </summary>
    private static void AddInterLibrary(IServiceCollection services)
    {
        services.AddHttpClient("interlibrary", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // IX.1 — lấy index.html của trang tra cứu để chèn thẻ meta. Máy chủ này nằm ngay cạnh trong
        // cùng mạng Docker, nên chờ lâu là vô ích: hỏng thì trả trang tối thiểu còn hơn để máy thu
        // thập treo.
        services.AddHttpClient("opac-shell", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddScoped<Application.Features.InterLibrary.IRemoteCatalogSearcher,
            RemoteCatalogSearcher>();
        services.AddScoped<Application.Features.InterLibrary.IOaiHarvester, OaiHarvester>();
        services.AddScoped<Application.Features.InterLibrary.IOpenLibraryHarvester, OpenLibraryHarvester>();

        services.AddHostedService<Z3950ServerHost>();
    }
}
