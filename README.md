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
| http://localhost/admin | Giao diện quản trị |
| http://localhost | Trang tra cứu OPAC dành cho bạn đọc |
| http://localhost/swagger | Tài liệu API (Swagger UI) |
| http://localhost/health | Health check (liveness) |
| http://localhost/health/ready | Health check (readiness: PostgreSQL, Redis) |
| http://localhost:9001 | MinIO Console |

Tài khoản quản trị được tạo tự động ở lần khởi động đầu tiên, tên đăng nhập `admin`. **Mật khẩu tạm
sinh ngẫu nhiên riêng cho từng bản cài** và chỉ hiện đúng một lần trong nhật ký khởi động của máy
chủ:

```bash
docker compose logs api | grep -A 6 "TÀI KHOẢN QUẢN TRỊ"
```

Hệ thống **bắt buộc đổi mật khẩu ngay ở lần đăng nhập đầu tiên**; đổi xong thì chuỗi in trong nhật
ký không dùng lại được nữa.

> Cài đặt tự động bằng kịch bản thì đặt trước `LC_SEED_ADMIN_PASSWORD` trong `.env` — lúc ấy máy chủ
> dùng đúng giá trị ấy và không in gì ra nhật ký.

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

# 4. Trang tra cứu OPAC (http://localhost:5175)
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
cd backend && dotnet test          # 415 unit test + 331 integration test
cd frontend-admin && npm test      # 138 test giao diện quản trị
cd frontend-opac && npm test       # 22 test giao diện tra cứu
```

Integration test tự khởi tạo một container PostgreSQL 16 riêng, chạy migration, nạp dữ liệu nền rồi
gọi API qua đúng giao diện HTTP mà trình duyệt dùng — không thành phần nào bị giả lập. Vì vậy cần
Docker đang chạy khi thực hiện `dotnet test`.

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

Phần **Web** (backend API + Admin SPA + OPAC SPA) và **ứng dụng di động Android** (Phân hệ XI,
Flutter trong `mobile/`) đã dựng và kiểm thử; bản iOS dùng cùng mã nguồn, chờ máy Mac để dựng IPA.
Hướng dẫn dựng, cấu hình endpoint và chạy 12 luồng đầu-cuối: `mobile/README.md`.

| Phase | Nội dung | Trạng thái |
|---|---|---|
| 1 | Nền móng: Clean Architecture, EF Core, JWT, RBAC, nhật ký tự động, health check, Docker | ✅ Hoàn thành |
| 2 | Phân hệ I — Quản trị hệ thống (nhóm quyền, người dùng, tham số, nhật ký, sao lưu/phục hồi) | ✅ Hoàn thành |
| 3 | Danh mục nghiệp vụ (20 danh mục dùng chung một màn hình, nhập/xuất Excel, gộp trùng) | ✅ Hoàn thành |
| 4 | MARC Core: mô hình MARC 21, đọc/ghi ISO 2709 và MARCXML, bộ định nghĩa 220 trường, trình soạn MARC | ✅ Hoàn thành |
| 5 | Phân hệ II — Biên mục (trình soạn MARC, ĐKCB, lịch sử phiên bản, hàng đợi, nhập/xuất ISO 2709 và Excel, danh mục tự tạo, in phích) | ✅ Hoàn thành |
| 6 | Phân hệ III — Bổ sung & Kho (đơn đặt, nhập kho, xếp giá, in tem mã vạch và nhãn gáy, chuyển kho, kiểm kê, mẫu biểu in, báo cáo) | ✅ Hoàn thành |
| 7 | Phân hệ IV — Ấn phẩm định kỳ (khai kỳ hạn, sinh số dự kiến, ghi nhận, khiếu nại, bài trích, đóng tập, báo cáo) | ✅ Hoàn thành |
| 8 | Phân hệ VI — Bạn đọc (hồ sơ, ảnh chân dung, thẻ, in thẻ, nhập xuất, đồng bộ, báo cáo) | ✅ Hoàn thành |
| 9 | Phân hệ VII — Lưu thông (chính sách và lịch nghỉ, quầy ghi mượn/ghi trả bằng bàn phím, đặt giữ chỗ, tiền phạt, cổng ra vào, tủ gửi đồ, 7 báo cáo, nhóm `/api/reader/*`) | ✅ Hoàn thành |
| 10 | Phân hệ V — Tài liệu số (bộ sưu tập, tải tệp lớn theo mảnh, nhận dạng ký tự tiếng Việt, trình đọc có chữ chìm, duyệt yêu cầu đọc, nhập xuất, 4 báo cáo) | ✅ Hoàn thành |
| 11 | Liên thư viện: Z39.50 hai chiều, SRU, OAI-PMH provider và harvester, nhập biểu ghi từ thư viện bạn | ✅ Hoàn thành |
| 12 | Phân hệ VIII, IX — Quản trị nội dung và trang tra cứu OPAC (tra cứu không dấu, facet, duyệt danh mục, trích dẫn, trang cá nhân bạn đọc, trình đọc tài liệu số, tìm ở thư viện khác, SEO) | ✅ Hoàn thành |
| 13 | Phân hệ X — Tài liệu môn học (ngành, môn học nhiều-nhiều, gán giáo trình và tài liệu tham khảo, nhập từ Excel, 3 báo cáo, duyệt theo ngành trên trang tra cứu) | ✅ Hoàn thành |
| 14 | Hoàn thiện web: dữ liệu minh họa, hiệu năng trên 500.000 biểu ghi, rà soát bảo mật, 7 tài liệu bàn giao, cấu hình chạy thật, script sao lưu/phục hồi | ✅ Hoàn thành |
| 15 | Ứng dụng di động (Phân hệ XI): tra cứu, quét mã, duyệt, thẻ điện tử, sách của tôi, đặt giữ, gia hạn, mượn tự phục vụ, tài liệu số + ngoại tuyến, thông báo, tài khoản | ✅ Android dựng và kiểm trên máy ảo (12 luồng đầu-cuối, `docs/06` MB.01–MB.33); iOS chưa dựng (không có máy Mac); thông báo đẩy thật và máy thật chưa kiểm |

Bộ dữ liệu minh họa đi kèm bản cài đặt: 200 biểu ghi thư mục, 500 ĐKCB, 50 bạn đọc, 100 lượt mượn
trả, 5 đầu ấn phẩm định kỳ với 113 số, 6 tài liệu số và 52 liên kết tài liệu môn học — đủ để demo
mọi phân hệ ngay sau `docker compose up -d`. Tắt bằng `LC_SEED_DEMO=false` khi bàn giao cho thư viện
dùng thật.
