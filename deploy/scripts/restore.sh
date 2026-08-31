#!/usr/bin/env bash
#
# Phục hồi LibraryConnect từ một bản sao lưu do backup.sh tạo ra.
#
# Cách dùng:
#   ./deploy/scripts/restore.sh backups/libraryconnect-db-20260901-020000.dump
#   ./deploy/scripts/restore.sh <tệp .dump> <tệp .tar.gz kho tệp>
#
# Thao tác này GHI ĐÈ toàn bộ dữ liệu hiện có. Script dừng dịch vụ API trước khi phục hồi để không
# có kết nối nào ghi vào giữa chừng, rồi bật lại sau khi xong.

set -euo pipefail

# Git Bash trên Windows tự đổi đường dẫn kiểu Unix trong tham số dòng lệnh thành đường dẫn Windows,
# làm hỏng đường dẫn nằm bên trong container. Tắt cơ chế đó; trên Linux hai biến này vô hại.
export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL='*'

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

DB_FILE="${1:-}"
FILES_ARCHIVE="${2:-}"

if [ -z "$DB_FILE" ] || [ ! -f "$DB_FILE" ]; then
  echo "Cách dùng: $0 <tệp .dump> [tệp .tar.gz kho tệp]" >&2
  exit 1
fi

# Đoán tệp kho tệp đi kèm theo dấu thời gian trong tên, nếu người dùng không chỉ định.
if [ -z "$FILES_ARCHIVE" ]; then
  CANDIDATE="${DB_FILE/libraryconnect-db-/libraryconnect-files-}"
  CANDIDATE="${CANDIDATE%.dump}.tar.gz"
  [ -f "$CANDIDATE" ] && FILES_ARCHIVE="$CANDIDATE"
fi

DB_NAME="$(grep -E '^LC_DB_NAME=' .env 2>/dev/null | cut -d= -f2- || true)"
DB_USER="$(grep -E '^LC_DB_USER=' .env 2>/dev/null | cut -d= -f2- || true)"
DB_NAME="${DB_NAME:-libraryconnect}"
DB_USER="${DB_USER:-libraryconnect}"

COMPOSE="docker compose"
if ! $COMPOSE version >/dev/null 2>&1; then
  COMPOSE="docker-compose"
fi

# Kiểm tra mã băm trước khi động vào dữ liệu: phục hồi từ một tệp hỏng còn tệ hơn không phục hồi.
if [ -f "$DB_FILE.sha256" ]; then
  echo "==> Kiểm tra mã băm của tệp sao lưu"
  ( cd "$(dirname "$DB_FILE")" && sha256sum -c "$(basename "$DB_FILE").sha256" )
fi

echo
echo "CẢNH BÁO: toàn bộ dữ liệu hiện có trong cơ sở dữ liệu '$DB_NAME' sẽ bị thay thế."
echo "Tệp sao lưu:      $DB_FILE"
echo "Kho tệp đi kèm:   ${FILES_ARCHIVE:-(không có)}"
echo
printf "Gõ chính xác PHUC-HOI để tiếp tục: "
read -r CONFIRM

if [ "$CONFIRM" != "PHUC-HOI" ]; then
  echo "Đã hủy, không thay đổi gì."
  exit 1
fi

echo "==> Dừng dịch vụ API"
$COMPOSE stop api >/dev/null

echo "==> Ngắt các kết nối còn lại tới $DB_NAME"
$COMPOSE exec -T postgres psql --username="$DB_USER" --dbname=postgres -v ON_ERROR_STOP=1 -c \
  "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$DB_NAME' AND pid <> pg_backend_pid();" \
  >/dev/null

echo "==> Phục hồi cơ sở dữ liệu"
# --clean --if-exists xóa đối tượng cũ trước khi dựng lại; --no-owner để bản sao lưu từ máy khác vẫn
# nạp được khi tên chủ sở hữu khác nhau.
$COMPOSE exec -T postgres pg_restore \
  --clean --if-exists --no-owner --no-privileges --exit-on-error \
  --username="$DB_USER" --dbname="$DB_NAME" < "$DB_FILE"

if [ -n "$FILES_ARCHIVE" ] && [ -f "$FILES_ARCHIVE" ]; then
  echo "==> Phục hồi kho tệp tài liệu số"

  MINIO_VOLUME="$(docker inspect -f '{{range .Mounts}}{{if eq .Destination "/data"}}{{.Name}}{{end}}{{end}}' lc-minio 2>/dev/null || true)"

  if [ -n "$MINIO_VOLUME" ]; then
    # Dừng MinIO trong lúc thay nội dung volume: ghi đè ngay dưới chân tiến trình đang chạy dễ để
    # lại trạng thái nửa vời trong bộ nhớ của nó.
    $COMPOSE stop minio >/dev/null
    docker run --rm -i -v "$MINIO_VOLUME":/data alpine:3       sh -c 'rm -rf /data/* /data/..?* 2>/dev/null; tar -xzf - -C /data' < "$FILES_ARCHIVE"
    $COMPOSE start minio >/dev/null
  else
    echo "Cảnh báo: không tìm thấy volume kho tệp của MinIO, bỏ qua phần tệp tài liệu số." >&2
  fi
fi

echo "==> Khởi động lại dịch vụ API"
$COMPOSE start api >/dev/null

echo "==> Chờ hệ thống sẵn sàng"
for attempt in $(seq 1 30); do
  if curl -fsS http://localhost/health/ready >/dev/null 2>&1; then
    echo
    echo "Phục hồi hoàn tất. Hãy đăng nhập và đối chiếu vài số liệu trước khi mở lại dịch vụ cho bạn đọc."
    exit 0
  fi
  sleep 5
done

echo "Cảnh báo: hệ thống chưa trả lời sau 150 giây. Xem nhật ký bằng '$COMPOSE logs api'." >&2
exit 1
