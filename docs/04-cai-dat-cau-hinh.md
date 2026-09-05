# Hướng dẫn cài đặt và cấu hình — LibraryConnect

Tài liệu dành cho quản trị viên hệ thống triển khai Phần mềm Thư viện số LibraryConnect.

---

## 1. Yêu cầu hạ tầng

### 1.1. Máy chủ

Hệ thống chạy được trên máy chủ vật lý lẫn máy ảo, trên Linux hoặc Windows Server 2019 trở lên.

| Quy mô | CPU | RAM | Ổ đĩa | Ghi chú |
|---|---|---|---|---|
| Tối thiểu (≤ 50.000 biểu ghi) | 4 nhân | 8 GB | 100 GB SSD | Đủ cho một cơ sở |
| Khuyến nghị (≤ 500.000 biểu ghi, 200 người dùng đồng thời) | 8 nhân | 16 GB | 500 GB SSD | Đáp ứng yêu cầu hiệu năng mục 6.3 |
| Có nhiều tài liệu số | 8 nhân | 16 GB | 500 GB + dung lượng tài liệu số | Tài liệu số lưu trong MinIO |

Dung lượng ổ đĩa cần tính thêm phần cho bản sao lưu: mỗi bản sao lưu toàn bộ chiếm khoảng 15–25%
kích thước cơ sở dữ liệu, và hệ thống giữ mặc định 30 bản gần nhất.

### 1.2. Phần mềm

- **Docker Engine 24+** và **Docker Compose v2** (cách triển khai khuyến nghị), hoặc
- **.NET 8 Runtime**, **PostgreSQL 16**, **Redis 7**, **MinIO** cài trực tiếp trên máy chủ.

### 1.3. Trình duyệt phía người dùng

Chrome, Edge, Firefox, Safari — hai phiên bản gần nhất. Giao diện quản trị thiết kế cho độ phân giải
tối thiểu 1366×768; trang tra cứu OPAC hỗ trợ cả điện thoại.

---

## 2. Triển khai bằng Docker (khuyến nghị)

### 2.1. Chuẩn bị

```bash
git clone https://github.com/minhhung19872002/LibraryConnect.git
cd LibraryConnect
cp .env.example .env
```

### 2.2. Sửa tệp `.env`

Ba giá trị **bắt buộc phải đổi** trước khi chạy thật:

| Biến | Ý nghĩa | Cách sinh giá trị |
|---|---|---|
| `POSTGRES_PASSWORD` | Mật khẩu tài khoản cơ sở dữ liệu | Chuỗi ngẫu nhiên ≥ 16 ký tự |
| `MINIO_ROOT_PASSWORD` | Mật khẩu kho lưu trữ tệp | Chuỗi ngẫu nhiên ≥ 16 ký tự |
| `LC_Jwt__Secret` | Khóa ký JWT | `openssl rand -base64 48` |

> Nếu `LC_Jwt__Secret` để trống hoặc ngắn hơn 32 ký tự, dịch vụ API sẽ dừng ngay khi khởi động kèm
> thông báo hướng dẫn — đây là chủ ý, để một hệ thống thiếu cấu hình bảo mật không bao giờ chạy lên.

Các biến khác cần chú ý:

| Biến | Mặc định | Khi nào cần đổi |
|---|---|---|
| `POSTGRES_PORT`, `REDIS_PORT`, `MINIO_API_PORT`, `API_PORT`, `HTTP_PORT` | 5432 / 6379 / 9000 / 8080 / 80 | Khi cổng đã bị dịch vụ khác chiếm trên máy chủ |
| `LC_CORS_ORIGINS` | localhost | Khi truy cập qua tên miền thật; thêm origin của ứng dụng di động ở đợt sau |
| `LC_RateLimit__LoginPerMinute` | 20 | Khi cả thư viện đi ra Internet qua **một** địa chỉ NAT: tăng theo số cán bộ đăng nhập cùng lúc |
| `LC_Backup__ScheduleCron` | `0 2 * * *` | Đổi giờ chạy sao lưu tự động |
| `TZ` | `Asia/Ho_Chi_Minh` | Không cần đổi khi triển khai trong nước |

### 2.3. Khởi động

```bash
docker compose up -d
docker compose ps          # tất cả phải ở trạng thái healthy
docker compose logs -f api # theo dõi quá trình migration và seed
```

Lần khởi động đầu tiên, dịch vụ API tự động:

1. Tạo toàn bộ cấu trúc cơ sở dữ liệu (chạy migration).
2. Tạo hàm tra cứu tiếng Việt không dấu và các chỉ mục tìm kiếm.
3. Nạp 161 mã quyền, 5 nhóm người dùng nghiệp vụ và tài khoản quản trị.
4. Nạp bộ tham số hệ thống mặc định.

Quá trình mất khoảng 30–60 giây. Khi `docker compose ps` báo `lc-api` là `healthy` là hệ thống đã sẵn sàng.

### 2.4. Địa chỉ truy cập

| Địa chỉ | Nội dung |
|---|---|
| `http://<máy-chủ>/admin` | Giao diện quản trị |
| `http://<máy-chủ>/swagger` | Tài liệu API |
| `http://<máy-chủ>/health` | Kiểm tra dịch vụ còn sống |
| `http://<máy-chủ>/health/ready` | Kiểm tra kết nối PostgreSQL và Redis |
| `http://<máy-chủ>:9001` | MinIO Console (quản lý tệp tài liệu số) |

### 2.5. Đăng nhập lần đầu

Tên đăng nhập là `admin`. Mật khẩu tạm **sinh ngẫu nhiên riêng cho từng bản cài**, không có giá trị
mặc định nào chung cho mọi nơi, và chỉ hiện đúng một lần trong nhật ký khởi động của máy chủ:

```bash
docker compose logs api | grep -A 6 "TÀI KHOẢN QUẢN TRỊ"
```

Khối in ra có dạng:

```
==================================================================
  TÀI KHOẢN QUẢN TRỊ ĐẦU TIÊN — CHÉP LẠI NGAY
    Tên đăng nhập : admin
    Mật khẩu tạm  : <chuỗi ngẫu nhiên 16 ký tự>
...
```

Chép lại rồi đăng nhập ngay. Hệ thống **bắt buộc đổi mật khẩu** trước khi cho vào bất kỳ chức năng
nào; sau khi đổi, mọi phiên đăng nhập cũ bị thu hồi và chuỗi trong nhật ký không dùng lại được.

Ràng buộc này do máy chủ giữ chứ không chỉ do giao diện: tài khoản chưa đổi mật khẩu tạm gọi thẳng
API cũng bị từ chối, chỉ còn đúng đường đăng nhập, đổi mật khẩu và đăng xuất là mở.

**Cài đặt tự động.** Kịch bản bàn giao không đọc được nhật ký container thì đặt trước biến
`LC_SEED_ADMIN_PASSWORD` trong `.env`. Máy chủ dùng đúng giá trị ấy, không in gì ra nhật ký, và vẫn
bắt buộc đổi ở lần đăng nhập đầu.

> **Vì sao không còn mật khẩu mặc định in trong tài liệu.** Bản trước đặt một mật khẩu cố định trong
> mã nguồn và in ra `README.md`. Kho mã để công khai, nên chuỗi ấy mở được cửa của mọi bản cài chưa
> ai đăng nhập lần nào — không riêng máy của người viết.

Quy tắc áp cho mọi tài khoản có mật khẩu tạm, kể cả tài khoản cán bộ do quản trị viên tạo và tài
khoản bạn đọc vừa được đặt lại mật khẩu.

---

## 3. Cấu hình sau khi cài đặt

Thứ tự khuyến nghị cho một hệ thống mới:

1. **Đổi mật khẩu quản trị** (bắt buộc, hệ thống tự yêu cầu).
2. **Tham số hệ thống → Thông tin thư viện**: nhập tên thư viện, địa chỉ, điện thoại, email, logo.
   Tên này hiển thị trên đầu trang quản trị, trên OPAC và trên mọi biểu mẫu in.
3. **Tham số hệ thống → Cấu hình biên mục**: đặt *Nguồn biên mục (MARC 040$a)* theo tên thư viện.
4. **Tham số hệ thống → Quy tắc sinh mã**: đặt tiền tố và độ dài cho mã vạch ĐKCB, số đăng ký cá
   biệt, số thẻ bạn đọc theo quy ước sẵn có của thư viện.
5. **Tham số hệ thống → Chính sách mật khẩu**: siết theo quy định của đơn vị nếu cần.
6. **Tham số hệ thống → Cấu hình email**: khai báo SMTP để hệ thống gửi được thông báo nhắc hạn trả,
   cảnh báo sao lưu lỗi và danh sách tài liệu bạn đọc gửi từ trang tra cứu. Ô nào điền trên màn hình
   thì ô ấy có hiệu lực ngay, không cần khởi động lại; ô để trống rơi về biến môi trường `LC_Smtp__*`
   trong `.env` (cách khai lúc cài đặt). Chưa khai máy chủ thư thì mọi chức năng gửi thư báo rõ
   "Thư viện chưa cấu hình máy chủ gửi thư" thay vì im lặng.
7. **Nhóm người dùng**: rà lại quyền của 5 nhóm mẫu, tạo thêm nhóm nếu cơ cấu tổ chức khác.
8. **Người dùng**: tạo tài khoản cho cán bộ, hoặc nhập hàng loạt từ Excel.
9. **Sao lưu**: kiểm tra sao lưu thủ công chạy được, sau đó bật lịch sao lưu tự động.

Toàn bộ giá trị ở bước 2–6 nằm trong bảng `sys.system_parameters` và sửa được từ giao diện. **Không
có thông tin nào của thư viện được viết cứng trong mã nguồn**, nên cùng một bản cài đặt triển khai
lại được cho đơn vị khác chỉ bằng cách đổi tham số.

---

## 4. Danh mục biến môi trường

Mọi biến của backend mang tiền tố `LC_`. Dấu `__` (hai gạch dưới) tương ứng với một cấp lồng nhau
trong cấu hình, ví dụ `LC_Jwt__Secret` chính là `Jwt:Secret`.

### 4.1. Cơ sở dữ liệu

| Biến | Mặc định | Ý nghĩa |
|---|---|---|
| `LC_DB_HOST` | `localhost` | Máy chủ PostgreSQL |
| `LC_DB_PORT` | `5432` | Cổng |
| `LC_DB_NAME` | `libraryconnect` | Tên cơ sở dữ liệu |
| `LC_DB_USER` | `libraryconnect` | Tài khoản |
| `LC_DB_PASSWORD` | — | Mật khẩu |
| `LC_ConnectionStrings__Default` | — | Chuỗi kết nối đầy đủ; nếu đặt thì các biến trên bị bỏ qua |
| `LC_Database__AutoMigrate` | `true` | Tự chạy migration khi khởi động |

### 4.2. Bảo mật

| Biến | Mặc định | Ý nghĩa |
|---|---|---|
| `LC_Jwt__Secret` | — | Khóa ký JWT, tối thiểu 32 ký tự |
| `LC_Jwt__AccessTokenMinutes` | `60` | Thời gian sống của access token |
| `LC_Jwt__RefreshTokenDays` | `30` | Thời gian sống của refresh token |
| `LC_CORS_ORIGINS` | localhost | Danh sách origin được phép, ngăn cách bằng dấu phẩy |
| `LC_RateLimit__LoginPerMinute` | `20` | Số lần đăng nhập tối đa mỗi phút theo IP |
| `LC_RateLimit__PublicPerMinute` | `300` | Số yêu cầu công khai tối đa mỗi phút theo IP |

### 4.3. Cache, lưu trữ tệp, tác vụ nền

| Biến | Mặc định | Ý nghĩa |
|---|---|---|
| `LC_Redis__ConnectionString` | `localhost:6379` | Kết nối Redis |
| `LC_Redis__Enabled` | `true` | Tắt để chạy không cần Redis (dùng cache nội bộ) |
| `LC_Minio__Endpoint` | `localhost:9000` | Địa chỉ MinIO |
| `LC_Minio__AccessKey` / `LC_Minio__SecretKey` | — | Thông tin đăng nhập MinIO |
| `LC_Minio__UseSsl` | `false` | Bật khi MinIO chạy HTTPS |
| `LC_Hangfire__ServerEnabled` | `true` | Tắt trên các instance API không chạy tác vụ nền |

> Nếu chưa khai báo `LC_Minio__AccessKey` / `LC_Minio__SecretKey`, hệ thống vẫn khởi động và mọi chức
> năng không liên quan đến tệp vẫn dùng được; chỉ các thao tác tải lên/tải xuống tài liệu số báo lỗi
> kèm hướng dẫn cấu hình.

### 4.4. Sao lưu

| Biến | Mặc định | Ý nghĩa |
|---|---|---|
| `LC_Backup__Directory` | `/var/lib/libraryconnect/backups` | Thư mục chứa bản sao lưu |
| `LC_Backup__KeepCount` | `30` | Số bản sao lưu giữ lại |
| `LC_Backup__ScheduleCron` | `0 2 * * *` | Lịch sao lưu tự động |
| `LC_Backup__AutoEnabled` | `true` | Bật/tắt sao lưu tự động |

---

## 5. Cấu hình HTTPS

Bản `docker-compose.yml` mặc định phục vụ HTTP để tiện cài đặt và kiểm thử. Khi đưa vào vận hành
thật, đặt LibraryConnect sau một reverse proxy có chứng thư số.

Ví dụ với Nginx trên máy chủ:

```nginx
server {
    listen 443 ssl http2;
    server_name thuvien.example.edu.vn;

    ssl_certificate     /etc/letsencrypt/live/thuvien.example.edu.vn/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/thuvien.example.edu.vn/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;

    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    client_max_body_size 512m;

    location / {
        proxy_pass http://127.0.0.1:80;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}

server {
    listen 80;
    server_name thuvien.example.edu.vn;
    return 301 https://$host$request_uri;
}
```

Sau khi bật HTTPS, cập nhật `LC_CORS_ORIGINS` thành địa chỉ `https://` tương ứng.

Backend đã tự nhận diện `X-Forwarded-For` và `X-Forwarded-Proto`, nên nhật ký hệ thống ghi đúng địa
chỉ IP thật của người dùng thay vì địa chỉ của reverse proxy.

---

## 5.1. Triển khai cho máy chủ chạy thật

Môi trường phát triển mở cổng của PostgreSQL, Redis, MinIO và API ra ngoài để tiện thao tác. Máy chủ
chạy thật không được như vậy. Kèm theo mã nguồn có một lớp cấu hình riêng, dùng chồng lên tệp gốc:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

Lớp này khác môi trường phát triển ở bốn điểm:

1. **Chỉ Nginx mở cổng ra ngoài** (80 và 443). PostgreSQL, Redis, MinIO và cả API chỉ nghe trong mạng
   nội bộ của Docker.
2. **Bật HTTPS** với cấu hình Nginx riêng (`deploy/nginx/nginx.prod.conf`): chuyển hướng HTTP sang
   HTTPS, HSTS, các đầu đề bảo mật, chặn tần suất ở tầng proxy, và giới hạn `/swagger` cùng
   `/hangfire` cho dải mạng nội bộ.
3. **Giới hạn tài nguyên** từng container và gom nhật ký theo dung lượng.
4. **Tắt Swagger và tắt bộ dữ liệu minh họa** theo mặc định.

Trước khi chạy, cần chuẩn bị:

```bash
# Chứng thư HTTPS — xem deploy/nginx/certs/README.md
cp /etc/letsencrypt/live/<tên-miền>/fullchain.pem deploy/nginx/certs/
cp /etc/letsencrypt/live/<tên-miền>/privkey.pem  deploy/nginx/certs/

# Các biến bắt buộc trong .env
POSTGRES_PASSWORD=<mật khẩu mạnh>
REDIS_PASSWORD=<mật khẩu mạnh>
LC_Jwt__Secret=<chuỗi ngẫu nhiên ≥ 32 ký tự>
LC_CORS_ORIGINS=https://thuvien.tentruong.edu.vn
LC_SEED_DEMO=false
```

Thiếu bất kỳ biến nào trong bốn biến đầu, Docker sẽ dừng lại và báo đúng tên biến còn thiếu thay vì
khởi động một hệ thống hở.

Sau khi lên, sửa dải IP trong hai khối `location /swagger` và `location /hangfire` của
`deploy/nginx/nginx.prod.conf` cho khớp mạng nội bộ của nhà trường.

---

## 5.1b. Máy chủ đã có proxy khác giữ cổng 80/443

Máy chủ dùng chung nhiều ứng dụng thường đã có một proxy (Caddy, Traefik, Nginx của máy) chiếm cổng
80/443 và tự lo chứng thư. Khi ấy không dùng `nginx.prod.conf` (có TLS) mà chồng thêm **lớp thứ ba**:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.behind-proxy.yml up -d
```

Lớp này tắt cổng ngoài của `lc-nginx` và thay cấu hình bằng `deploy/nginx/nginx.behind-proxy.conf`:
chỉ HTTP trong mạng Docker, lấy IP thật từ `X-Forwarded-For` (để giới hạn tốc độ và chặn
`/swagger`, `/hangfire` vẫn tính trên người dùng chứ không phải trên proxy), và truyền tiếp
`X-Forwarded-Proto` của proxy phía trước. Không cần chứng thư trong `deploy/nginx/certs`.

Phía proxy, với Caddy: chép `deploy/caddy/libraryconnect.caddy.example` vào thư mục site của Caddy,
thay tên miền, cho Caddy tham gia mạng `libraryconnect_libraryconnect` rồi reload:

```bash
docker network connect libraryconnect_libraryconnect proxy-caddy   # và khai trong compose của proxy
docker exec proxy-caddy caddy reload --config /etc/caddy/Caddyfile
```

Đã triển khai theo cách này ngày 03/09/2026 tại `https://thuvien.bluestar.com.vn`: chứng thư
Let's Encrypt cấp trong 10 giây sau reload, `/swagger`, `/hangfire`, `/health` trả 404 từ ngoài,
SRU và OAI-PMH mở bình thường.

---

## 5.2. Chuẩn bị cho kho dữ liệu lớn

Với thư viện có trên 100.000 biểu ghi, kiểm lại ba tham số sau — đây là những chỗ đã đo được là điểm
nghẽn khi chạy thử trên kho 500.000 biểu ghi:

| Tham số | Giá trị khuyến nghị | Vì sao |
|---|---|---|
| `shm_size` của container PostgreSQL | 1–2 GB | Docker chỉ cấp 64 MB; các tiến trình chạy song song một câu truy vấn cần vùng này, hết chỗ là câu lệnh hỏng giữa chừng |
| `max_connections` của PostgreSQL | ≥ 200 | Kho kết nối của API cộng với kho của máy chủ tác vụ nền vượt mức mặc định 100 khi đông người dùng |
| `LC_DB_MAX_POOL_SIZE` | 60 (mặc định) | Chặn dưới `max_connections`; lượt gọi vượt hạn sẽ xếp hàng chờ chứ không bị từ chối |

Cả ba đã được đặt sẵn trong `docker-compose.yml` và `docker-compose.prod.yml`; mục này để đối chiếu
khi triển khai bằng cách khác.

Trên kho lớn, migration của lần nâng cấp đầu tiên có thể chạy vài phút (đo được 140 giây trên 500.000
biểu ghi). Đó là hành vi bình thường; hệ thống chờ xong migration mới nhận yêu cầu.

---

## 6. Nâng cấp phiên bản

```bash
cd LibraryConnect
docker compose exec api sh -c 'echo kiểm tra dịch vụ còn sống'   # tùy chọn
git pull
docker compose build
docker compose up -d
```

Migration mới được áp dụng tự động khi dịch vụ API khởi động lại. **Luôn tạo một bản sao lưu trước
khi nâng cấp** (Quản trị hệ thống → Sao lưu cơ sở dữ liệu → *Sao lưu ngay*).

---

## 7. Xử lý sự cố khi cài đặt

| Hiện tượng | Nguyên nhân thường gặp | Cách xử lý |
|---|---|---|
| `docker compose up` báo *port is already allocated* | Cổng đã bị dịch vụ khác chiếm | Đổi biến cổng tương ứng trong `.env` rồi chạy lại |
| API dừng ngay, log ghi *LC_JWT_SECRET chưa được cấu hình* | Chưa đặt khóa ký JWT | Đặt `LC_Jwt__Secret` ≥ 32 ký tự |
| Log ghi `28P01: password authentication failed` | Máy chủ đang có sẵn một PostgreSQL khác chiếm cổng 5432 | Đổi `POSTGRES_PORT` sang cổng khác |
| `/health/ready` báo `Unhealthy` | PostgreSQL hoặc Redis chưa sẵn sàng | `docker compose ps`, xem log của container tương ứng |
| Tải tài liệu số báo *Chưa cấu hình kho lưu trữ tệp MinIO* | Thiếu `LC_Minio__AccessKey` / `SecretKey` | Bổ sung vào `.env`, khởi động lại `api` |
| Sao lưu báo *Không tìm thấy công cụ pg_dump* | Chạy API ngoài container mà máy chủ chưa cài PostgreSQL client | Cài `postgresql-client`, hoặc đặt `LC_Backup__PgDumpPath` trỏ tới đường dẫn đầy đủ |
| Chữ tiếng Việt trong log bị lỗi phông trên Windows | Console chưa ở chế độ UTF-8 | Xem tệp log JSON trong `logs/` thay cho cửa sổ console |
| Log ghi `could not resize shared memory segment` | Bộ nhớ chia sẻ của container PostgreSQL quá nhỏ | Đặt `shm_size: 1gb` cho dịch vụ `postgres` (đã có sẵn trong tệp compose kèm theo) |
| Log ghi `sorry, too many clients already` | Số kết nối vượt `max_connections` | Nâng `max_connections` của PostgreSQL, hoặc hạ `LC_DB_MAX_POOL_SIZE` |
| Kho có sẵn 200 đầu sách lạ sau khi cài | Bộ dữ liệu minh họa được nạp theo mặc định | Đặt `LC_SEED_DEMO=false` rồi cài lại; bộ minh họa chỉ nạp khi kho còn trống |

## Máy chủ Z39.50 (kết nối liên thư viện chiều vào)

`docker compose up -d` công bố cổng **210** (biến `Z3950_PORT` trong `.env`, trong container là 2100 vì
tiến trình API không chạy root). Thư viện bạn khai vào phần mềm của họ: `tcp:<máy chủ>:210`, cơ sở dữ
liệu theo tham số `ILL.Z3950_DATABASE_NAME` (mặc định `LIBRARYCONNECT`). Bật/tắt bằng tham số
`ILL.Z3950_SERVER_ENABLED` (bản cài mới bật sẵn; bản cài trước 04/09/2026 giữ giá trị đang có).
Kiểm nhanh từ máy ngoài:

```bash
yaz-client tcp:<máy chủ>:210/LIBRARYCONNECT
Z> find "cơ sở dữ liệu"
Z> show 1
```

Sau proxy dùng chung (Caddy) cổng 210 vẫn do Docker công bố thẳng, không đi qua proxy.
