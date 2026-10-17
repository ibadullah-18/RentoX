#!/bin/bash
# Brings a backup back. Usage (inside the backup service):
#   docker compose exec backup /scripts-default/restore.sh db      # the database  (latest snapshot)
#   docker compose exec backup /scripts-default/restore.sh files   # the uploaded images
#   ... add a snapshot id as the second word to take an older one:  restore.sh db 1a2b3c4d
#   docker compose exec backup restic snapshots                    # lists what there is
#
# On a NEW server: set up .env (the SAME keys as before), start only postgres and the backup service, restore the
# database, then the images, then start everything. README.md has the full steps.
set -euo pipefail

what="${1:-}"
snap="${2:-latest}"
export PGPASSWORD="${POSTGRES_PASSWORD}"

case "$what" in
  db)
    work="$(mktemp -d)"
    trap 'rm -rf "$work"' EXIT
    restic restore "$snap" --tag db --host rentox --target "$work"
    dump="$(find "$work" -name rentox.dump | head -n 1)"
    [ -n "$dump" ] || { echo "no database dump in that snapshot"; exit 1; }
    echo "[restore] loading $dump (the current content of the database is replaced)"
    pg_restore -h postgres -U rentox -d rentox --clean --if-exists --no-owner "$dump"
    echo "[restore] database done"
    ;;
  files)
    echo "[restore] images back into /data/storage (existing files with the same name are overwritten)"
    restic restore "$snap" --tag files --host rentox --target /
    # The API and the worker run as the user "app" (id 1654) of the .NET image: the restored files must belong to it.
    chown -R 1654:1654 /data/storage
    echo "[restore] images done"
    ;;
  *)
    echo "usage: restore.sh db|files [snapshot-id]"
    exit 2
    ;;
esac
