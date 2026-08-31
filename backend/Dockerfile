# ---------- Giai đoạn build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
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
# Nền Debian chứ không phải Alpine: thư viện kết xuất PDF (PDFium) và bộ nhận dạng ký tự Tesseract
# chỉ có bản dựng cho glibc. Xem docs/00-quyet-dinh-ky-thuat.md, quyết định của Phase 10.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

# postgresql-client-16: pg_dump / pg_restore của chức năng sao lưu (I.5). Phải đúng phiên bản 16 vì
#   pg_dump đời cũ từ chối kết xuất máy chủ đời mới hơn, nên lấy từ kho chính thức của PostgreSQL.
# tesseract-ocr + tesseract-ocr-vie: nhận dạng ký tự tiếng Việt cho tài liệu số bản quét (V.1).
# libfontconfig1 + fonts-dejavu-core: QuestPDF và SkiaSharp cần font để kết xuất PDF và đóng chữ chìm.
# locales + tzdata: sắp xếp, hiển thị đúng tiếng Việt và múi giờ Việt Nam.
RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends ca-certificates curl gnupg; \
    install -d /usr/share/postgresql-common/pgdg; \
    curl -fsSL -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc \
        https://www.postgresql.org/media/keys/ACCC4CF8.asc; \
    echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] http://apt.postgresql.org/pub/repos/apt bookworm-pgdg main" \
        > /etc/apt/sources.list.d/pgdg.list; \
    apt-get update; \
    apt-get install -y --no-install-recommends \
        postgresql-client-16 \
        tesseract-ocr \
        tesseract-ocr-vie \
        libfontconfig1 \
        fonts-dejavu-core \
        tzdata; \
    apt-get purge -y --auto-remove gnupg; \
    rm -rf /var/lib/apt/lists/*; \
    mkdir -p /var/lib/libraryconnect/backups /app/logs

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_URLS=http://+:8080 \
    TZ=Asia/Ho_Chi_Minh

COPY --from=build /app/publish .

# Chạy bằng người dùng không phải root.
RUN groupadd --system lc \
    && useradd --system --gid lc --home-dir /app --shell /usr/sbin/nologin lc \
    && chown -R lc:lc /app /var/lib/libraryconnect
USER lc

EXPOSE 8080
ENTRYPOINT ["dotnet", "LibraryConnect.Api.dll"]
