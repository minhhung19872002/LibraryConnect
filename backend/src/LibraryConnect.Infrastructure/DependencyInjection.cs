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

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Thiếu chuỗi kết nối cơ sở dữ liệu. Hãy đặt biến môi trường LC_DB_HOST / LC_DB_NAME / LC_DB_USER / LC_DB_PASSWORD.");

        AddPersistence(services, connectionString);
        AddCaching(services, configuration);
        AddObjectStorage(services);
        AddBackgroundJobs(services, connectionString, configuration);

        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        services.AddScoped<ISystemParameterService, SystemParameterService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICodeGenerator, CodeGenerator>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<INotificationSender, EmailNotificationSender>();
        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();
        services.AddScoped<IBackupService, PostgresBackupService>();

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

        services.AddDbContext<LibraryConnectDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "sys");
                npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            });

            options.AddInterceptors(
                provider.GetRequiredService<AuditableEntityInterceptor>(),
                provider.GetRequiredService<AuditLogInterceptor>());
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
    }
}
