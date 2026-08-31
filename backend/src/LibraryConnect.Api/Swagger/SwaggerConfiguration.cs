using Microsoft.OpenApi.Models;

namespace LibraryConnect.Api.Swagger;

/// <summary>
/// Swagger is part of the deliverable: it is the API reference handed to whoever writes the Flutter
/// client in the next release (section 0.2 and 10.5).
/// </summary>
public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LibraryConnect API",
                Version = "v1",
                Description = """
                    API của Phần mềm Thư viện số **LibraryConnect**.

                    - Mọi phản hồi dùng chung định dạng `{ success, data, message, errors }`.
                    - Danh sách luôn phân trang phía máy chủ: `{ items, totalCount, page, pageSize }`.
                    - Xác thực bằng JWT: gửi header `Authorization: Bearer <access_token>`.
                    - Nhóm **Bạn đọc (API cho ứng dụng khách)** là hợp đồng dùng chung cho OPAC và
                      ứng dụng di động; xem thêm `docs/05-api-reference.md`.
                    """,
                Contact = new OpenApiContact { Name = "LibraryConnect" }
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Nhập access token nhận được từ POST /api/auth/login."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFile = Path.Combine(AppContext.BaseDirectory,
                $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");

            if (File.Exists(xmlFile))
            {
                options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
            }

            options.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "LibraryConnect API v1");
            options.DocumentTitle = "LibraryConnect API";
            options.DefaultModelsExpandDepth(1);
            options.DisplayRequestDuration();
        });

        return app;
    }
}
