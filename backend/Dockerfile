# ---------- Giai đoạn build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy file dự án trước để tận dụng cache lớp restore.
COPY LibraryConnect.sln Directory.Build.props ./
COPY src/LibraryConnect.Domain/*.csproj src/LibraryConnect.Domain/
COPY src/LibraryConnect.Application/*.csproj src/LibraryConnect.Application/
COPY src/LibraryConnect.Infrastructure/*.csproj src/LibraryConnect.Infrastructure/
COPY src/LibraryConnect.Marc/*.csproj src/LibraryConnect.Marc/
COPY src/LibraryConnect.Reporting/*.csproj src/LibraryConnect.Reporting/
COPY src/LibraryConnect.Api/*.csproj src/LibraryConnect.Api/
COPY tests/LibraryConnect.UnitTests/*.csproj tests/LibraryConnect.UnitTests/
COPY tests/LibraryConnect.IntegrationTests/*.csproj tests/LibraryConnect.IntegrationTests/
RUN dotnet restore src/LibraryConnect.Api/LibraryConnect.Api.csproj

COPY . .
RUN dotnet publish src/LibraryConnect.Api/LibraryConnect.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Giai đoạn chạy ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# postgresql-client: cần cho pg_dump / pg_restore của chức năng sao lưu (I.5).
# icu-libs + tzdata: sắp xếp và hiển thị đúng tiếng Việt, múi giờ Việt Nam.
# fontconfig + ttf-dejavu: QuestPDF cần font để kết xuất PDF.
RUN apk add --no-cache \
        postgresql16-client \
        icu-libs \
        icu-data-full \
        tzdata \
        fontconfig \
        ttf-dejavu \
        curl \
    && mkdir -p /var/lib/libraryconnect/backups /app/logs

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_URLS=http://+:8080 \
    TZ=Asia/Ho_Chi_Minh

COPY --from=build /app/publish .

# Chạy bằng người dùng không phải root.
RUN addgroup -S lc && adduser -S lc -G lc \
    && chown -R lc:lc /app /var/lib/libraryconnect
USER lc

EXPOSE 8080
ENTRYPOINT ["dotnet", "LibraryConnect.Api.dll"]
