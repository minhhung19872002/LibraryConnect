using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hangfire;
using HealthChecks.UI.Client;
using LibraryConnect.Api.Health;
using LibraryConnect.Api.Middleware;
using LibraryConnect.Api.Security;
using LibraryConnect.Api.Swagger;
using LibraryConnect.Application;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Infrastructure;
using LibraryConnect.Infrastructure.Configuration;
using LibraryConnect.Infrastructure.Persistence;
using LibraryConnect.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// The whole product speaks Vietnamese; without this the Windows console renders the log messages
// as mojibake, which makes on-site troubleshooting needlessly hard.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration: every setting is overridable by an LC_-prefixed environment
// variable so a deployment needs no file edits (see .env.example).
// ---------------------------------------------------------------------------
builder.Configuration.AddEnvironmentVariables(prefix: "LC_");
builder.Configuration["ConnectionStrings:Default"] = BuildConnectionString(builder.Configuration);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "LibraryConnect.Api"));

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
// Phạm vi dữ liệu theo lượt gọi (mục 6.1): DbContext đọc từ đây để dựng bộ lọc truy vấn toàn cục.
builder.Services.AddScoped<IDataScopeContext, DataScopeContext>();

builder.Services
    .AddControllers(options =>
    {
        options.SuppressAsyncSuffixInActionNames = true;

        // Khung nền tự sinh thông báo lỗi bằng tiếng Anh khi thiếu tham số hay sai kiểu dữ liệu, và
        // câu ấy đi thẳng ra giao diện. Cả sản phẩm phải bằng tiếng Việt nên thay hết ở đây, một
        // chỗ, thay vì bắt từng controller tự bắt lỗi.
        var thongBao = options.ModelBindingMessageProvider;

        thongBao.SetValueIsInvalidAccessor(giaTri =>
            $"Giá trị {giaTri} không hợp lệ.");
        thongBao.SetValueMustNotBeNullAccessor(_ =>
            "Trường này không được để trống.");
        thongBao.SetMissingBindRequiredValueAccessor(truong =>
            $"Thiếu trường bắt buộc \"{truong}\".");
        thongBao.SetMissingKeyOrValueAccessor(() =>
            "Thiếu khóa hoặc giá trị.");
        thongBao.SetMissingRequestBodyRequiredValueAccessor(() =>
            "Yêu cầu thiếu phần nội dung.");
        thongBao.SetAttemptedValueIsInvalidAccessor((giaTri, truong) =>
            $"Giá trị \"{giaTri}\" không hợp lệ cho trường \"{truong}\".");
        thongBao.SetNonPropertyAttemptedValueIsInvalidAccessor(giaTri =>
            $"Giá trị \"{giaTri}\" không hợp lệ.");
        thongBao.SetUnknownValueIsInvalidAccessor(truong =>
            $"Giá trị của trường \"{truong}\" không hợp lệ.");
        thongBao.SetNonPropertyUnknownValueIsInvalidAccessor(() =>
            "Giá trị gửi lên không hợp lệ.");
        thongBao.SetValueMustBeANumberAccessor(truong =>
            $"Trường \"{truong}\" phải là một con số.");
        thongBao.SetNonPropertyValueMustBeANumberAccessor(() =>
            "Giá trị gửi lên phải là một con số.");
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Model-binding failures must use the same envelope as everything else.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                new ApiError(entry.Key, ThongBaoTiengViet(entry.Key, error.ErrorMessage))))
            .ToList();

        return new BadRequestObjectResult(ApiResponse.Fail("Dữ liệu không hợp lệ.", errors));
    };
});

AddAuthentication(builder);
AddCors(builder);
AddRateLimiting(builder);
AddHealthChecks(builder);
builder.Services.AddSwaggerDocumentation();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
// Tin proxy trong mạng nội bộ (mặc định ba dải RFC 1918), nếu không X-Forwarded-For của Nginx bị bỏ
// qua và mọi yêu cầu mang địa chỉ của Nginx: nhật ký, chữ chìm và bộ giới hạn tốc độ đều sai (H7).
app.UseForwardedHeaders(ForwardedHeadersSetup.Build(builder.Configuration));

// Ghi nhật ký yêu cầu đứng TRƯỚC bộ xử lý ngoại lệ: đứng sau thì nó nhìn thấy ngoại lệ nghiệp vụ
// bay ngang qua trước khi được đổi thành 400/401/404, và ghi "ERR … trả về 500" kèm vết ngăn xếp cho
// cả một lượt gõ sai mật khẩu. Lỗi 500 thật chìm trong đó (lỗi H1 đợt rà thứ ba).
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "{RequestMethod} {RequestPath} trả về {StatusCode} sau {Elapsed:0.0} ms";
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseResponseCompression();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwaggerDocumentation();
}

app.UseCors("LibraryConnect");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
// Sau xác thực, trước mọi handler: điền phạm vi kho / thư viện / dạng tài liệu của cán bộ.
app.UseMiddleware<DataScopeMiddleware>();

// Sau xác thực để đọc được thông tin tài khoản, trước khi vào bất kỳ endpoint nghiệp vụ nào.
app.UseMiddleware<PasswordChangeRequiredMiddleware>();

app.MapControllers();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
    DashboardTitle = "LibraryConnect — Tác vụ nền",
    IgnoreAntiforgeryToken = true
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

await InitialiseDatabaseAsync(app);

app.Run();

// ---------------------------------------------------------------------------
// Local functions
// ---------------------------------------------------------------------------

// Assembles the Npgsql connection string from the individual LC_DB_* variables, which is how the
// docker-compose deployment is configured. An explicit ConnectionStrings:Default still wins.
static string BuildConnectionString(IConfiguration configuration)
{
    var explicitValue = configuration.GetConnectionString("Default");
    if (!string.IsNullOrWhiteSpace(explicitValue))
    {
        return explicitValue;
    }

    var host = configuration["DB_HOST"] ?? "localhost";
    var port = configuration["DB_PORT"] ?? "5432";
    var database = configuration["DB_NAME"] ?? "libraryconnect";
    var user = configuration["DB_USER"] ?? "libraryconnect";
    var password = configuration["DB_PASSWORD"] ?? "libraryconnect";

    // Trần số kết nối tới cơ sở dữ liệu.
    //
    // Mặc định của Npgsql là 100 kết nối cho mỗi chuỗi kết nối, mà máy chủ tác vụ nền cũng mở kho
    // kết nối riêng của nó. Khi 200 bạn đọc cùng tra cứu, hai kho ấy đòi nhiều hơn số kết nối
    // PostgreSQL cho phép và máy chủ trả về lỗi "sorry, too many clients already" — người dùng thấy
    // lỗi hệ thống chứ không phải chờ lâu. Chặn ở mức thấp hơn hạn của PostgreSQL thì lượt gọi thứ
    // 61 xếp hàng đợi tới lượt, chậm hơn nhưng vẫn ra kết quả.
    var poolSize = configuration.GetValue("DB_MAX_POOL_SIZE", 60);

    return $"Host={host};Port={port};Database={database};Username={user};Password={password};" +
           $"Maximum Pool Size={poolSize};Connection Idle Lifetime=60;" +
           "Include Error Detail=true;Timezone=Asia/Ho_Chi_Minh";
}

static void AddAuthentication(WebApplicationBuilder builder)
{
    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

    if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
    {
        throw new InvalidOperationException(
            "LC_JWT_SECRET chưa được cấu hình hoặc quá ngắn (yêu cầu tối thiểu 32 ký tự). " +
            "Xem hướng dẫn trong .env.example.");
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                // Tokens are short lived; no slack is granted so a revoked session really expires.
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // The Hangfire dashboard is a plain browser navigation and cannot set headers.
                    if (string.IsNullOrEmpty(context.Token)
                        && context.Request.Path.StartsWithSegments("/hangfire"))
                    {
                        context.Token = context.Request.Query["access_token"];
                    }

                    return Task.CompletedTask;
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsJsonAsync(
                        ApiResponse.Fail("Phiên đăng nhập không hợp lệ hoặc đã hết hạn."));
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsJsonAsync(
                        ApiResponse.Fail("Bạn không có quyền thực hiện chức năng này."));
                }
            };
        });

    builder.Services.AddAuthorization();
}

static void AddCors(WebApplicationBuilder builder)
{
    // Configurable so the mobile app and any extra front-end host can be allowed later without a
    // code change (section 0.2).
    var origins = (builder.Configuration["CORS_ORIGINS"] ?? "http://localhost:5173,http://localhost:5174")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    builder.Services.AddCors(options => options.AddPolicy("LibraryConnect", policy =>
    {
        if (origins.Contains("*"))
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition");
    }));
}

static void AddRateLimiting(WebApplicationBuilder builder)
{
    // Configurable because a campus library typically reaches the server through a single NAT
    // address: a limit tuned for one person would then throttle the whole staff at shift change.
    var loginLimit = builder.Configuration.GetValue("RateLimit:LoginPerMinute", 20);
    var publicLimit = builder.Configuration.GetValue("RateLimit:PublicPerMinute", 300);

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Brute-force protection on the sign-in endpoints (section 6.4). Repeated failures on a
        // single account are additionally handled by the account lock-out in the login handler.
        options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

        // Anonymous OPAC traffic gets a generous but bounded budget.
        options.AddPolicy("public", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = publicLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

        options.OnRejected = async (context, ct) =>
        {
            // Nói rõ phải chờ bao lâu thay vì để máy khách tự đoán: ứng dụng di động và trang tra
            // cứu đều thử lại tự động, mà thử lại ngay chỉ làm cửa sổ chặn dài thêm.
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)Math.Ceiling(retryAfter.TotalSeconds))
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
            await context.HttpContext.Response.WriteAsJsonAsync(
                ApiResponse.Fail("Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau ít phút."), ct);
        };
    });
}

static void AddHealthChecks(WebApplicationBuilder builder)
{
    var redisSection = builder.Configuration.GetSection(RedisOptions.SectionName);
    var redis = redisSection["ConnectionString"];
    var redisEnabled = redisSection.GetValue("Enabled", true);

    var checks = builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("Default")!,
            name: "postgresql",
            tags: new[] { "ready" });

    // A deployment that runs without Redis (Redis:Enabled=false — the cache degrades to memory)
    // must not report 503 on /health/ready because of it. The default connection string is
    // localhost:6379, so without this guard readiness depends on whatever happens to listen there:
    // green on the development machine, red on a clean CI runner.
    if (redisEnabled && !string.IsNullOrWhiteSpace(redis))
    {
        checks.AddRedis(redis, name: "redis", tags: new[] { "ready" });
    }

    // Kho đối tượng (6.5): MinIO chết là ảnh bìa, tài liệu số và sao lưu đều hỏng.
    checks.AddCheck<MinioHealthCheck>("minio", tags: new[] { "ready" });
}

// Applies migrations and seeds the baseline data at start-up, so `docker compose up -d` is all an
// operator has to run (section 7).
static async Task InitialiseDatabaseAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("Database:AutoMigrate", true))
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await seeder.MigrateAsync();
        await seeder.SeedAsync();
        await seeder.RecoverInterruptedJobsAsync();
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Không khởi tạo được cơ sở dữ liệu");
        throw;
    }
}

// Lớp Program để mở (partial ở cuối tệp) cho bộ kiểm thử tích hợp dựng máy chủ bằng
// WebApplicationFactory.

// Đổi thông báo lỗi còn sót tiếng Anh của khung nền thành câu tiếng Việt.
//
// Đa số câu đã thay được bằng ModelBindingMessageProvider, nhưng bộ đọc JSON của .NET dựng câu
// riêng cho lỗi kiểu dữ liệu (The JSON value could not be converted to…) và không có chỗ nào cấu
// hình. Bắt ở đây là chốt chặn cuối trước khi câu ấy ra tới giao diện.
static string ThongBaoTiengViet(string truong, string? thongBao)
{
    if (string.IsNullOrWhiteSpace(thongBao))
    {
        return "Giá trị không hợp lệ.";
    }

    var ten = string.IsNullOrWhiteSpace(truong) ? "gửi lên" : $"\"{truong}\"";

    if (thongBao.Contains("JSON value could not be converted", StringComparison.OrdinalIgnoreCase)
        || thongBao.Contains("could not be converted", StringComparison.OrdinalIgnoreCase))
    {
        return $"Giá trị của trường {ten} sai kiểu dữ liệu.";
    }

    if (thongBao.Contains("is required", StringComparison.OrdinalIgnoreCase))
    {
        return $"Thiếu trường bắt buộc {ten}.";
    }

    if (thongBao.Contains("is invalid", StringComparison.OrdinalIgnoreCase))
    {
        return $"Giá trị của trường {ten} không hợp lệ.";
    }

    if (thongBao.Contains("unexpected character", StringComparison.OrdinalIgnoreCase)
        || thongBao.Contains("invalid start of a value", StringComparison.OrdinalIgnoreCase))
    {
        return "Nội dung gửi lên không phải JSON hợp lệ.";
    }

    // Câu do chính hệ thống đặt ra thì giữ nguyên; chỉ những câu còn chữ Anh mới phải thay.
    return thongBao;
}

public partial class Program { }
