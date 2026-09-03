# Quy trình sao lưu và phục hồi — LibraryConnect

Tài liệu dành cho quản trị viên hệ thống. Áp dụng cho Phân hệ I.5 và mục kiểm thử 2.6 của E-HSMT.

---

## 1. Hệ thống sao lưu những gì

| Thành phần | Công cụ | Nội dung |
|---|---|---|
| Cơ sở dữ liệu PostgreSQL | `pg_dump` định dạng custom (`-Fc`, nén mức 6) | Toàn bộ biểu ghi, ĐKCB, bạn đọc, giao dịch mượn trả, tham số, nhật ký |
| Tệp tài liệu số | Kho MinIO | Tệp PDF/DOCX/EPUB/video và các bản dẫn xuất (thumbnail, bản xem trước) |

Bản sao lưu cơ sở dữ liệu là một tệp `.dump` chuẩn của PostgreSQL. Quản trị viên có thể mở, kiểm tra
hoặc phục hồi nó bằng `pg_restore` từ dòng lệnh mà không cần đến LibraryConnect — đây là chủ ý, để
dữ liệu của thư viện không bị khóa vào một định dạng riêng.

---

## 2. Sao lưu thủ công

**Đường dẫn:** Quản trị hệ thống → Sao lưu cơ sở dữ liệu → **Sao lưu ngay**

1. Chọn **Loại sao lưu**:
   - *Toàn bộ* — cấu trúc và dữ liệu. Đây là lựa chọn khuyến nghị và là loại duy nhất dùng để phục
     hồi lên một máy chủ trống.
   - *Chỉ dữ liệu* — không kèm cấu trúc bảng. Chỉ dùng khi nạp dữ liệu vào một hệ thống đã có sẵn
     cấu trúc cùng phiên bản.
2. Chọn có **sao lưu kèm tệp tài liệu số** hay không.
3. Bấm **Bắt đầu sao lưu**. Lượt sao lưu được **xếp vào hàng đợi nền**: lệnh trả về ngay, đóng trình
   duyệt cũng không làm nó dừng. Với cơ sở dữ liệu vài trăm nghìn biểu ghi, `pg_dump` mất từ vài giây
   đến vài phút.

Danh sách tự cập nhật trong lúc chạy: trạng thái đi từ *Đã xếp hàng* → *Đang chạy* → *Thành công*,
rồi hiện tên tệp, dung lượng, mã kiểm tra SHA-256 và người thực hiện. Đang có một lượt chưa xong thì
lượt thứ hai bị từ chối — hai `pg_dump` cùng ghi một thư mục chỉ tranh nhau tài nguyên.

**Bản sao lưu không chứa hàng đợi việc.** Schema `hangfire` bị loại ra khỏi bản dump: nó là danh sách
việc đang chờ chứ không phải dữ liệu của thư viện, và phục hồi lại nó sẽ khiến những việc của hôm sao
lưu chạy lại một lần nữa.

**Quyền cần có:** `SYSTEM.BACKUP.CREATE`.

---

## 3. Sao lưu tự động

**Đường dẫn cấu hình:** Quản trị hệ thống → Tham số hệ thống → **Cấu hình sao lưu**

| Tham số | Ý nghĩa | Mặc định |
|---|---|---|
| Bật sao lưu tự động | Bật/tắt lịch chạy | Bật |
| Lịch sao lưu (cron) | Biểu thức 5 thành phần | `0 2 * * *` (2 giờ sáng hằng ngày) |
| Số bản sao lưu giữ lại | Bản cũ hơn sẽ bị xóa tự động | 30 |
| Sao lưu kèm tệp tài liệu số | | Bật |
| Email nhận cảnh báo khi sao lưu lỗi | Cần cấu hình SMTP trước | trống |

Một vài biểu thức cron thường dùng:

| Biểu thức | Nghĩa |
|---|---|
| `0 2 * * *` | 2:00 mỗi ngày |
| `0 2 * * 0` | 2:00 mỗi Chủ nhật |
| `0 */6 * * *` | Mỗi 6 giờ |
| `30 1 1 * *` | 1:30 ngày mùng 1 hằng tháng |

Lịch được nạp lại khi dịch vụ API khởi động. Sau khi đổi lịch, khởi động lại `api`
(`docker compose restart api`) để lịch mới có hiệu lực ngay; nếu không, lịch cũ vẫn chạy đến lần
khởi động kế tiếp.

Tiến trình và lịch sử chạy của tác vụ nền xem tại `http://<máy-chủ>/hangfire` (yêu cầu quyền
`SYSTEM.JOB.VIEW`).

---

## 4. Tải bản sao lưu ra khỏi máy chủ

Một bản sao lưu nằm cùng máy chủ với dữ liệu gốc **không bảo vệ được trước hỏng ổ đĩa hay mất máy**.
Vì vậy quy trình vận hành nên bao gồm việc đưa bản sao lưu ra nơi khác:

- Trên giao diện: cột **Thao tác** → **Tải về**. Mỗi lần tải về đều được ghi vào nhật ký hệ thống.
- Trên máy chủ: các tệp nằm trong volume `backup-data`, tương ứng thư mục
  `/var/lib/libraryconnect/backups` bên trong container.

```bash
# Sao chép toàn bộ bản sao lưu ra thư mục hiện tại của máy chủ
docker compose cp api:/var/lib/libraryconnect/backups ./backup-copy
```

Khuyến nghị: đồng bộ thư mục này sang một máy chủ hoặc thiết bị lưu trữ khác mỗi ngày, và giữ ít
nhất một bản ở vị trí vật lý khác với máy chủ chính.

### 4.1. Script sao lưu ra ngoài

Kèm theo mã nguồn có sẵn một script chạy từ máy chủ, dùng khi cần chủ động sao lưu ngoài giờ hoặc khi
muốn đưa bản sao ra ổ cứng rời, ổ mạng:

```bash
./deploy/scripts/backup.sh                  # lưu vào ./backups
./deploy/scripts/backup.sh /mnt/nas/thuvien # lưu vào thư mục chỉ định
```

Mỗi lần chạy sinh ra bốn tệp:

| Tệp | Nội dung |
|---|---|
| `libraryconnect-db-<thời-điểm>.dump` | Cơ sở dữ liệu, định dạng `pg_restore` đọc được |
| `libraryconnect-db-<thời-điểm>.dump.sha256` | Mã kiểm tra để đối chiếu khi phục hồi |
| `libraryconnect-files-<thời-điểm>.tar.gz` | Kho tệp tài liệu số |
| `libraryconnect-files-<thời-điểm>.tar.gz.sha256` | Mã kiểm tra tương ứng |

Đặt vào `crontab` của máy chủ để chạy hằng đêm:

```cron
30 2 * * * cd /opt/libraryconnect && ./deploy/scripts/backup.sh /mnt/nas/thuvien >> /var/log/lc-backup.log 2>&1
```

Sao lưu bằng script và sao lưu bằng giao diện dùng chung định dạng, nên bản nào cũng phục hồi được
bằng cả hai đường.

---

## 5. Phục hồi

> **Thao tác phục hồi ghi đè toàn bộ dữ liệu hiện tại.** Mọi biểu ghi, bạn đọc và giao dịch phát sinh
> sau thời điểm của bản sao lưu sẽ mất.

**Đường dẫn:** Quản trị hệ thống → Sao lưu cơ sở dữ liệu → dòng cần phục hồi → **Phục hồi**

Quy trình gồm hai bước có chủ ý:

1. **Bước cảnh báo** — hiển thị thông tin bản sao lưu (tên tệp, thời điểm, dung lượng, loại) và nhắc
   rằng dữ liệu hiện tại sẽ bị ghi đè.
2. **Bước xác nhận** — yêu cầu nhập lại mật khẩu của chính người đang đăng nhập. Chỉ có quyền
   `SYSTEM.BACKUP.RESTORE` là chưa đủ.

Hệ thống chạy `pg_restore` với `--single-transaction --exit-on-error`: quá trình phục hồi hoặc thành
công trọn vẹn, hoặc thất bại và **cơ sở dữ liệu giữ nguyên như trước**. Không có trạng thái nửa vời.

Lượt phục hồi cũng chạy ở **tiến trình nền**, vì `pg_restore` một kho vài GB lâu hơn thời gian một
lượt gọi HTTP được phép mở. Hộp thoại chuyển sang màn hình theo dõi và không đóng được khi đang chạy;
nếu lỡ đóng trình duyệt, mở lại màn hình sao lưu là thấy tiếp. Tiến độ ấy đọc từ bộ nhớ đệm chứ không
từ cơ sở dữ liệu — chính cơ sở dữ liệu đang bị ghi đè, nên mọi dòng ghi tiến độ vào đó đều bị xoá
đúng lúc cần đọc nhất.

Trong lúc phục hồi, **các màn hình khác tạm thời không dùng được**: `pg_restore` giữ khoá trên toàn bộ
bảng cho tới khi giao dịch kết thúc.

Sau khi phục hồi:

1. Đăng xuất và đăng nhập lại — tài khoản, mật khẩu và phân quyền lúc này là của thời điểm sao lưu.
2. Kiểm tra nhanh: số biểu ghi, số bạn đọc, vài giao dịch mượn trả gần nhất.
3. Nếu bản sao lưu có kèm tài liệu số, phục hồi thư mục tệp tương ứng vào MinIO.

**Quyền cần có:** `SYSTEM.BACKUP.RESTORE`. Cả lệnh bắt đầu phục hồi lẫn kết quả đều được ghi vào nhật
ký hệ thống trước khi `pg_restore` chạy, để dấu vết của quyết định tồn tại kể cả khi bản sao lưu ghi
đè lên chính bảng nhật ký.

---

## 6. Phục hồi bằng dòng lệnh

Dùng khi không truy cập được giao diện, hoặc khi dựng lại hệ thống trên máy chủ mới.

```bash
# 1. Dừng dịch vụ API để không có kết nối nào đang ghi
docker compose stop api

# 2. Phục hồi
docker compose exec -T postgres pg_restore \
  --clean --if-exists --no-owner --no-privileges \
  --single-transaction --exit-on-error \
  --username=libraryconnect --dbname=libraryconnect \
  /var/lib/libraryconnect/backups/<tên-tệp>.dump

# 3. Khởi động lại
docker compose start api
```

Mã thoát `0` nghĩa là phục hồi thành công. Mã thoát khác `0` nghĩa là toàn bộ giao dịch đã được hoàn
tác và cơ sở dữ liệu không thay đổi.

### 6.1. Phục hồi từ bản sao lưu do script tạo ra

```bash
./deploy/scripts/restore.sh backups/libraryconnect-db-20260901-023000.dump
```

Script làm đủ các bước theo đúng thứ tự: đối chiếu mã kiểm tra của tệp, hỏi lại một lần bằng cách bắt
gõ đúng chữ `PHUC-HOI`, dừng dịch vụ API, ngắt các kết nối còn sót, chạy `pg_restore`, phục hồi kho
tệp tài liệu số nếu có, bật lại API rồi chờ tới khi hệ thống trả lời được.

Tệp kho tệp đi kèm được tự tìm theo dấu thời gian trong tên; muốn chỉ định tay thì truyền thêm tham
số thứ hai.

---

## 7. Kiểm chứng bản sao lưu (khuyến nghị định kỳ)

Một bản sao lưu chưa từng được phục hồi thử thì chưa chắc dùng được. Nên kiểm chứng mỗi quý theo
kịch bản sau, **trên một máy chủ thử nghiệm chứ không phải máy chủ đang chạy**:

1. Dựng một hệ thống LibraryConnect trống bằng `docker compose up -d`.
2. Chép bản sao lưu mới nhất sang máy đó.
3. Phục hồi bằng dòng lệnh ở mục 6.
4. Đăng nhập, đối chiếu số liệu với hệ thống thật:

```sql
SELECT 'biểu ghi',  count(*) FROM bib.bib_records  WHERE deleted_at IS NULL
UNION ALL SELECT 'ĐKCB',     count(*) FROM acq.items       WHERE deleted_at IS NULL
UNION ALL SELECT 'bạn đọc',  count(*) FROM rdr.readers     WHERE deleted_at IS NULL
UNION ALL SELECT 'mượn trả', count(*) FROM cir.loans       WHERE deleted_at IS NULL;
```

5. Ghi kết quả vào biên bản kiểm chứng.

---

## 8. Tình huống khẩn cấp

### 8.1. Xóa nhầm dữ liệu

Hệ thống dùng **xóa mềm**: mọi thao tác xóa chỉ đánh dấu `deleted_at`, dữ liệu vẫn nằm nguyên trong
cơ sở dữ liệu. Trong đa số trường hợp không cần phục hồi từ bản sao lưu.

```sql
-- Ví dụ: khôi phục một nhóm người dùng bị xóa nhầm
UPDATE sys.user_groups SET deleted_at = NULL, deleted_by = NULL WHERE code = 'CATALOGER';
```

Tra nhật ký hệ thống (lọc theo hành động *Xóa*) để biết chính xác ai xóa, lúc nào và bản ghi nào.

### 8.2. Hỏng cơ sở dữ liệu

1. Dừng `api` để tránh ghi thêm.
2. Nếu vẫn kết nối được, sao lưu ngay hiện trạng — kể cả hỏng một phần, nó vẫn có thể chứa dữ liệu
   mới hơn bản sao lưu gần nhất.
3. Phục hồi từ bản sao lưu gần nhất theo mục 6.
4. Đối chiếu khoảng dữ liệu bị mất (từ thời điểm sao lưu đến lúc sự cố) và nhập bù thủ công.

### 8.3. Mất toàn bộ máy chủ

1. Dựng máy chủ mới theo `docs/04-cai-dat-cau-hinh.md`.
2. Khôi phục tệp `.env` (hoặc tạo lại, đặt đúng `LC_Jwt__Secret` cũ nếu muốn giữ các phiên đăng nhập
   hiện có; đặt khóa mới cũng được, người dùng chỉ cần đăng nhập lại).
3. Phục hồi cơ sở dữ liệu theo mục 6.
4. Phục hồi thư mục tài liệu số vào MinIO.

---

## 9. Nhật ký liên quan

Mọi thao tác trong tài liệu này đều để lại dấu vết tra được tại
Quản trị hệ thống → Nhật ký hệ thống, lọc theo hành động:

| Hành động | Ghi khi nào |
|---|---|
| *Sao lưu* | Mỗi lần sao lưu, kể cả tự động và kể cả khi thất bại |
| *Phục hồi* | Khi bắt đầu phục hồi và khi có kết quả |
| *Xuất dữ liệu* | Khi tải một bản sao lưu về máy |
| *Xóa* | Khi xóa một bản sao lưu |
