#!/usr/bin/env bash
# Daily backup of the WildSpirit database, product photos and Data Protection keys.
# Usage: sudo /srv/wildspirit/src/deploy/oracle/backup.sh   (run by wildspirit-backup.timer)
set -euo pipefail

cd "$(dirname "$0")"
ENV_FILE="${ENV_FILE:-production.env}"
BACKUP_DIR="${BACKUP_DIR:-/srv/wildspirit-backups}"
KEEP_DAYS="${KEEP_DAYS:-14}"
DATA_DIR="$(grep -E '^WILDSPIRIT_DATA_DIR=' "$ENV_FILE" | cut -d= -f2- | tr -d '\r"'"'"'')"
: "${DATA_DIR:?WILDSPIRIT_DATA_DIR missing in $ENV_FILE}"

umask 077
mkdir -p "$BACKUP_DIR"
chmod 700 "$BACKUP_DIR"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"

# Retention first, so a failing backup day does not stop old files (and leftovers) from expiring.
find "$BACKUP_DIR" -maxdepth 1 -type f \( -name 'db-*.dump' -o -name 'files-*.tgz' -o -name '*.partial' \) -mtime +"$KEEP_DAYS" -delete

docker compose --env-file "$ENV_FILE" exec -T postgres pg_dump -U wildspirit -d wildspirit -Fc > "$BACKUP_DIR/db-$stamp.dump.partial"
mv "$BACKUP_DIR/db-$stamp.dump.partial" "$BACKUP_DIR/db-$stamp.dump"

tar -C "$DATA_DIR/app" -czf "$BACKUP_DIR/files-$stamp.tgz.partial" files keys
mv "$BACKUP_DIR/files-$stamp.tgz.partial" "$BACKUP_DIR/files-$stamp.tgz"

echo "WildSpirit backup $stamp done: $(du -sh "$BACKUP_DIR/db-$stamp.dump" | cut -f1) database, $(du -sh "$BACKUP_DIR/files-$stamp.tgz" | cut -f1) files"
