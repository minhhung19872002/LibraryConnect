# Chứng thư HTTPS

Thư mục này để trống trong mã nguồn. Trước khi chạy `docker-compose.prod.yml`, đặt vào đây hai tệp:

| Tệp | Nội dung |
|---|---|
| `fullchain.pem` | Chứng thư của tên miền, nối tiếp chứng thư trung gian |
| `privkey.pem` | Khóa riêng, quyền đọc chỉ dành cho quản trị viên (`chmod 600`) |

Lấy chứng thư miễn phí bằng Let's Encrypt:

```bash
sudo certbot certonly --webroot -w /var/www/certbot -d thuvien.tentruong.edu.vn
sudo cp /etc/letsencrypt/live/thuvien.tentruong.edu.vn/fullchain.pem deploy/nginx/certs/
sudo cp /etc/letsencrypt/live/thuvien.tentruong.edu.vn/privkey.pem  deploy/nginx/certs/
docker compose -f docker-compose.yml -f docker-compose.prod.yml restart nginx
```

Chứng thư Let's Encrypt hết hạn sau 90 ngày. Đặt lịch gia hạn và chép lại hai tệp trên, hoặc dùng
chứng thư do nhà trường cấp nếu hệ thống chỉ phục vụ trong mạng nội bộ.

Không đưa khóa riêng vào Git.
