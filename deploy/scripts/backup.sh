#!/usr/bin/env bash
#
# Sao lưu LibraryConnect từ máy chủ chạy Docker.
#
# Bản sao lưu do màn hình Quản trị hệ thống → Sao lưu tạo ra nằm trong volume của Docker; script này
# dùng khi cần chủ động sao lưu ngoài giờ, hoặc khi muốn đem bản sao ra khỏi máy chủ (ổ cứng rời,
# ổ mạng, dịch vụ lưu trữ ngoài).
#
# Cách dùng:
#   ./deploy/scripts/backup.sh                       # lưu vào ./backups
#   ./deploy/scripts/backup.sh /mnt/nas/thuvien      # lưu vào thư mục chỉ định
#
# Kết quả: một tệp .dump của cơ sở dữ liệu và một tệp .tar.gz của kho tệp tài liệu số, cùng một tệp
# .sha256 để kiểm chứng khi phục hồi.

set -euo pipefail

# Git Bash trên Windows tự đổi đường dẫn kiểu Unix trong tham số dòng lệnh thành đường dẫn Windows,
# làm hỏng đường dẫn nằm bên trong container. Tắt cơ chế đó; trên Linux hai biến này vô hại.
export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL='*'

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DEST="${1:-$ROOT/backups}"
STAMP="$(date +%Y%m%d-%H%M%S)"

cd "$ROOT"

# Đọc .env để lấy tên cơ sở dữ liệu và tài khoản; không có thì dùng giá trị mặc định của sản phẩm.
DB_NAME="$(grep -E '^LC_DB_NAME=' .env 2>/dev/null | cut -d= -f2- || true)"
DB_USER="$(grep -E '^LC_DB_USER=' .env 2>/dev/null | cut -d= -f2- || true)"
DB_NAME="${DB_NAME:-libraryconnect}"
DB_USER="${DB_USER:-libraryconnect}"

COMPOSE="docker compose"
if ! $COMPOSE version >/dev/null 2>&1; then
  COMPOSE="docker-compose"
fi

if ! $COMPOSE ps --status running --services 2>/dev/null | grep -qx postgres; then
  echo "Lỗi: dịch vụ postgres chưa chạy. Khởi động bằng '$COMPOSE up -d' rồi chạy lại." >&2
  exit 1
fi

mkdir -p "$DEST"

DB_FILE="$DEST/libraryconnect-db-$STAMP.dump"
FILES_ARCHIVE="$DEST/libraryconnect-files-$STAMP.tar.gz"

echo "==> Sao lưu cơ sở dữ liệu $DB_NAME"
# --format=custom là định dạng pg_restore đọc được, đúng định dạng mà chức năng phục hồi trong phần
# mềm dùng, nên bản sao lưu từ script và từ giao diện thay thế cho nhau được.
$COMPOSE exec -T postgres pg_dump \
  --format=custom --compress=6 --no-owner --no-privileges \
  --username="$DB_USER" --dbname="$DB_NAME" > "$DB_FILE"

echo "==> Sao lưu kho tệp tài liệu số"
# Kho tệp nằm trong volume của MinIO, mà ảnh MinIO không có sẵn tar. Gắn chính volume đó vào một
# container alpine dùng một lần rồi đóng gói từ đấy — cách này không phụ thuộc vào việc dịch vụ
# MinIO có đang phục vụ hay không, và cũng không cần cài thêm gì lên máy chủ.
MINIO_VOLUME="$(docker inspect -f '{{range .Mounts}}{{if eq .Destination "/data"}}{{.Name}}{{end}}{{end}}' lc-minio 2>/dev/null || true)"

if [ -n "$MINIO_VOLUME" ]; then
  # Đẩy gói qua luồng chuẩn thay vì gắn thêm thư mục của máy chủ vào container: bớt một chỗ phụ
  # thuộc vào cách từng hệ điều hành dịch đường dẫn, và cũng bớt một quyền ghi không cần thiết.
  docker run --rm -v "$MINIO_VOLUME":/data:ro alpine:3 tar -czf - -C /data . > "$FILES_ARCHIVE"
else
  echo "Cảnh báo: không tìm thấy volume kho tệp của MinIO. Bản sao lưu chỉ gồm cơ sở dữ liệu." >&2
fi

echo "==> Tính mã kiểm tra"
( cd "$DEST" && sha256sum "$(basename "$DB_FILE")" > "$(basename "$DB_FILE").sha256" )

if [ -f "$FILES_ARCHIVE" ]; then
  ( cd "$DEST" && sha256sum "$(basename "$FILES_ARCHIVE")" > "$(basename "$FILES_ARCHIVE").sha256" )
fi

echo
echo "Đã sao lưu xong vào $DEST:"
ls -lh "$DEST" | grep "$STAMP" || true
echo
echo "Phục hồi bằng: ./deploy/scripts/restore.sh \"$DB_FILE\""
