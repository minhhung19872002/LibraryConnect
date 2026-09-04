#!/usr/bin/env bash
# deploy/scripts/gh-deploy.sh — máy chủ kéo bản mới theo lệnh của GitHub Actions.
#
# Được gọi qua forced command trong ~/.ssh/authorized_keys, nên khóa deploy KHÔNG mở được shell:
#   command="/home/hung/apps/libraryconnect/deploy/scripts/gh-deploy.sh",no-port-forwarding,no-X11-forwarding,no-agent-forwarding,no-pty ssh-ed25519 ...
# Actions gọi:  echo "$GITHUB_TOKEN" | ssh -i key hung@<máy chủ> <mã commit>
#   - mã commit lấy từ SSH_ORIGINAL_COMMAND: dùng để ghim LC_IMAGE_TAG (quay lại bản trước = chạy lại với mã cũ)
#   - token GHCR đọc từ stdin, không nằm trên dòng lệnh nên không lộ trong ps/log
#
# Máy chủ chỉ kéo ảnh, không dựng: máy 4 nhân dùng chung năm hệ thống. Không dùng sudo — user hung
# thuộc nhóm docker. Cùng mẫu với ~/apps/starlab/gh-deploy.sh.
set -euo pipefail

DEPLOY_DIR=/home/hung/apps/libraryconnect
GHCR_USER=minhhung19872002
REPO=minhhung19872002/LibraryConnect
LOG=$DEPLOY_DIR/deploy.log
COMPOSE="docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.behind-proxy.yml -f docker-compose.ghcr.yml"

log() { echo "[$(date -u +%FT%TZ)] $*" | tee -a "$LOG"; }

TAG="${SSH_ORIGINAL_COMMAND:-latest}"
case "$TAG" in
    latest | [0-9a-f]*) ;;
    *) echo "usage: ssh ... <commit-sha|latest> (got: '$TAG')" >&2; exit 2 ;;
esac

log "deploy $TAG bắt đầu"

if ! read -r -t 10 GHCR_TOKEN || [ -z "${GHCR_TOKEN:-}" ]; then
    log "LỖI: không nhận được token GHCR qua stdin"
    exit 3
fi
echo "$GHCR_TOKEN" | docker login ghcr.io -u "$GHCR_USER" --password-stdin >/dev/null
log "đã đăng nhập ghcr.io"

cd "$DEPLOY_DIR"

# Mã nguồn trên máy chủ chỉ để lấy compose/nginx/script mới nhất — ảnh thì kéo từ GHCR.
git fetch -q origin main && git reset -q --hard origin/main
log "mã nguồn tại $(git rev-parse --short HEAD)"

# Ghim tag ảnh cho lượt này, ghi vào .env để `up` sau cũng dùng đúng bản.
sed -i '/^LC_IMAGE_TAG=/d' .env
echo "LC_IMAGE_TAG=$TAG" >> .env

log "kéo ảnh api/admin/opac:$TAG"
$COMPOSE pull -q api admin opac

# API tự chạy migration lúc khởi động (LC_Database__AutoMigrate=true).
log "khởi động lại"
$COMPOSE up -d --no-build --remove-orphans

# Cấu hình nginx được gắn vào container theo **tệp**, mà `git reset --hard` ở trên thay tệp bằng một
# inode mới — container vẫn giữ inode cũ, nên đổi cấu hình xong mà không dựng lại thì nginx chạy bản
# cũ trong khi tệp trên đĩa đã mới. Đã mất một lượt triển khai vì chuyện này (04/09/2026).
log "dựng lại nginx để nhận cấu hình mới"
$COMPOSE up -d --no-build --force-recreate nginx

# APK mới nhất từ release mobile-latest → thư mục downloads (OPAC phục vụ tại /downloads/LibraryConnect.apk).
mkdir -p downloads
if curl -fsSL --retry 3 -o downloads/LibraryConnect.apk.tmp \
     "https://github.com/$REPO/releases/download/mobile-latest/LibraryConnect.apk"; then
    mv downloads/LibraryConnect.apk.tmp downloads/LibraryConnect.apk
    log "APK cập nhật ($(du -h downloads/LibraryConnect.apk | cut -f1))"
else
    rm -f downloads/LibraryConnect.apk.tmp
    log "CẢNH BÁO: không tải được APK từ release mobile-latest, giữ bản cũ"
fi

# Đợi API healthy — .NET khởi động chậm trên máy dùng chung, tối đa 4 phút.
for i in $(seq 1 48); do
    s=$(docker inspect lc-api --format '{{.State.Health.Status}}' 2>/dev/null || echo none)
    if [ "$s" = healthy ]; then
        code=$(curl -s -o /dev/null -w '%{http_code}' https://thuvien.bluestar.com.vn/api/public/settings || echo 000)
        log "deploy $TAG THÀNH CÔNG (healthy sau $((i * 5))s, /api/public/settings → $code)"
        docker image prune -f >/dev/null 2>&1 || true
        exit 0
    fi
    sleep 5
done

log "LỖI: lc-api không healthy sau 240s"
docker logs lc-api --tail 40 2>&1 | tee -a "$LOG"
exit 5
