# LibraryConnect — Phần mềm Thư viện số chuẩn kết nối liên thư viện

Hệ thống Thư viện Tích hợp (ILS) cho các trường đại học và thư viện tại Việt Nam: biên mục MARC 21,
bổ sung – kho, ấn phẩm định kỳ, tài liệu số, bạn đọc, lưu thông, OPAC và kết nối liên thư viện qua
Z39.50 / SRU / OAI-PMH.

Toàn bộ giao diện, thông báo và tài liệu bàn giao bằng **tiếng Việt**. Mọi chuỗi ký tự dùng Unicode
UTF-8 theo TCVN 6909:2001.

---

## 1. Kiến trúc

| Thành phần | Công nghệ |
|---|---|
| Backend | .NET 8 (ASP.NET Core Web API), Clean Architecture + CQRS (MediatR) |
| ORM | Entity Framework Core 8 + Npgsql, code-first migrations |
| CSDL | PostgreSQL 16 (UTF-8, collation ICU `vi-VN`) |
| Cache / hàng đợi | Redis 7 |
| Tìm kiếm | PostgreSQL Full-Text Search + `unaccent` + `pg_trgm` (tra cứu được cả khi gõ không dấu) |
| Lưu trữ tệp | MinIO (S3-compatible) |
| Tác vụ nền | Hangfire (PostgreSQL storage) |
| Frontend | React 18 + TypeScript + Vite + Ant Design 5 |
| Báo cáo | QuestPDF (PDF) + ClosedXML (Excel) + Recharts (biểu đồ) |
| Nhật ký | Serilog (console + file JSON) |
| Xác thực | JWT access token + refresh token, RBAC chi tiết đến từng chức năng |

Backend chia 3 tầng tách bạch — frontend **không** truy cập cơ sở dữ liệu trực tiếp:

```
LibraryConnect.Domain          Thực thể, enum, quy tắc nghiệp vụ thuần
LibraryConnect.Application     Use case (Command/Query + Handler + Validator + DTO)
LibraryConnect.Infrastructure  EF Core, Redis, MinIO, email, Hangfire, seeding
LibraryConnect.Marc            MARC 21 / ISO 2709 / MARCXML / Z39.50 / OAI-PMH
LibraryConnect.Reporting       Mẫu PDF (QuestPDF) và Excel (ClosedXML)
LibraryConnect.Api             Controller, middleware, Swagger, health check
```

---

## 2. Chạy nhanh bằng Docker

```bash
cp .env.example .env
# Bắt buộc sửa trong .env: POSTGRES_PASSWORD, MINIO_ROOT_PASSWORD, LC_Jwt__Secret
docker compose up -d
```

Sau khi các container báo `healthy`:

| Địa chỉ | Nội dung |
|---|---|
| http://localhost | Trang tra cứu OPAC |
| http://localhost/admin | Giao diện quản trị |
| http://localhost:8080/swagger | Tài liệu API (Swagger UI) |
| http://localhost:8080/health | Health check (liveness) |
| http://localhost:8080/health/ready | Health check (readiness: PostgreSQL, Redis) |
| http://localhost:9001 | MinIO Console |

Tài khoản quản trị được tạo tự động ở lần khởi động đầu tiên:

```
Tên đăng nhập: admin
Mật khẩu tạm : LibraryConnect@2025
```

Hệ thống **bắt buộc đổi mật khẩu ngay ở lần đăng nhập đầu tiên**.

> Nếu một cổng đã bị chiếm trên máy chủ, đổi biến tương ứng trong `.env`
> (`POSTGRES_PORT`, `REDIS_PORT`, `MINIO_API_PORT`, `API_PORT`, `HTTP_PORT`) rồi chạy lại
> `docker compose up -d`.

---

## 3. Chạy khi phát triển

Cần: .NET 8 SDK, Node.js 20+, Docker (cho PostgreSQL/Redis/MinIO).

```bash
# 1. Hạ tầng
docker compose up -d postgres redis minio

# 2. Backend  (http://localhost:8080)
cd backend
dotnet run --project src/LibraryConnect.Api

# 3. Giao diện quản trị (http://localhost:5174)
cd frontend-admin && npm install && npm run dev

# 4. OPAC (http://localhost:5173)
cd frontend-opac && npm install && npm run dev
```

Backend tự động chạy migration và seed dữ liệu nền khi khởi động
(tắt bằng `LC_Database__AutoMigrate=false`).

### Migration

```bash
cd backend
dotnet ef migrations add <TênCóNghĩa> \
  --project src/LibraryConnect.Infrastructure \
  --startup-project src/LibraryConnect.Infrastructure \
  --output-dir Persistence/Migrations
```

Không sửa migration đã commit — luôn tạo migration mới.

### Kiểm thử

```bash
cd backend && dotnet test          # xUnit + FluentAssertions + Testcontainers
cd frontend-admin && npm test      # Vitest
```

---

## 4. Cấu hình

Mọi tham số vận hành đọc từ biến môi trường tiền tố `LC_` (xem `.env.example` có chú thích đầy đủ
bằng tiếng Việt). Các giá trị **nghiệp vụ** — tên thư viện, quy tắc sinh mã ĐKCB / số thẻ, chính sách
mật khẩu, cấu hình lưu thông, giao diện OPAC — nằm trong bảng `sys.system_parameters` và sửa được
trực tiếp trên giao diện quản trị, **không hardcode trong mã nguồn**.

Nhờ vậy sản phẩm triển khai lại được cho khách hàng khác chỉ bằng cách đổi tham số.

---

## 5. Tài liệu

| Tệp | Nội dung |
|---|---|
| `docs/01-huong-dan-su-dung.md` | Hướng dẫn sử dụng theo từng phân hệ và vai trò |
| `docs/02-tai-lieu-quan-tri.md` | Kiến trúc, cấu hình, giám sát, xử lý sự cố |
| `docs/03-sao-luu-phuc-hoi.md` | Quy trình sao lưu và phục hồi |
| `docs/04-cai-dat-cau-hinh.md` | Yêu cầu hạ tầng và các bước triển khai |
| `docs/05-api-reference.md` | Tài liệu API, kèm chương "API cho ứng dụng khách" |
| `docs/06-kich-ban-kiem-thu.md` | Kịch bản kiểm thử dùng làm phụ lục nghiệm thu |
| `docs/07-bang-dap-ung-ky-thuat.md` | Bảng đối chiếu đáp ứng yêu cầu kỹ thuật E-HSMT |

`CLAUDE.md` ở thư mục gốc là đặc tả đầy đủ của sản phẩm.

---

## 6. Tình trạng triển khai

Đợt build hiện tại xây dựng phần **Web** (backend API + Admin SPA + OPAC SPA). Ứng dụng di động
(Phân hệ XI) thuộc đợt sau; backend đã hoàn thiện sẵn nhóm endpoint `/api/reader/*` để ứng dụng cắm
vào mà không phải sửa lại — xem `mobile/README.md`.

| Phase | Nội dung | Trạng thái |
|---|---|---|
| 1 | Nền móng: Clean Architecture, EF Core, JWT, RBAC, nhật ký tự động, health check, Docker | ✅ Hoàn thành |
| 2 | Phân hệ I — Quản trị hệ thống | 🔄 Đang thực hiện |
| 3 | Danh mục | ⏳ |
| 4 | MARC Core (ISO 2709, MARCXML) | ⏳ |
| 5 | Phân hệ II — Biên mục | ⏳ |
| 6 | Phân hệ III — Bổ sung & Kho | ⏳ |
| 7 | Phân hệ IV — Ấn phẩm định kỳ | ⏳ |
| 8 | Phân hệ VI — Bạn đọc | ⏳ |
| 9 | Phân hệ VII — Lưu thông | ⏳ |
| 10 | Phân hệ V — Tài liệu số | ⏳ |
| 11 | Liên thư viện: Z39.50, SRU, OAI-PMH | ⏳ |
| 12 | Phân hệ VIII, IX — OPAC và quản trị nội dung | ⏳ |
| 13 | Phân hệ X — Tài liệu môn học | ⏳ |
| 14 | Hoàn thiện, tài liệu bàn giao, kịch bản kiểm thử | ⏳ |
