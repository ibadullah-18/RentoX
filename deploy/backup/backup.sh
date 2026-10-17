#!/bin/bash
# One backup: the database (a dump) and the uploaded images, both into the encrypted restic repository in R2.
# - the dump is a new snapshot each time; images are added incrementally (only new files are sent)
# - old snapshots are removed: 30 daily, 8 weekly, 12 monthly ones are kept
# Run it by hand to try it:  docker compose exec backup /scripts-default/backup.sh
set -euo pipefail

export PGPASSWORD="${POSTGRES_PASSWORD}"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

echo "[backup] $(date -Is) start"

# The repository is made on the first run.
if ! restic cat config >/dev/null 2>&1; then
  echo "[backup] first run: creating the repository"
  restic init
fi

echo "[backup] database dump"
pg_dump -h postgres -U rentox -d rentox -Fc -f "$work/rentox.dump"
# A dump that cannot be read back is no backup: check it before sending.
pg_restore --list "$work/rentox.dump" >/dev/null

restic backup "$work/rentox.dump" --tag db --host rentox --quiet
echo "[backup] images"
restic backup /data/storage --tag files --host rentox --quiet

echo "[backup] removing old snapshots"
restic forget --tag db --host rentox --keep-daily 30 --keep-weekly 8 --keep-monthly 12 --prune --quiet
# Images: only the newest snapshots are needed (each one holds everything), a few for safety.
restic forget --tag files --host rentox --keep-daily 7 --keep-weekly 4 --prune --quiet

restic check --read-data-subset=2% --quiet

if [ -n "${HEALTHCHECK_URL:-}" ]; then
  curl -fsS -m 10 --retry 3 "$HEALTHCHECK_URL" >/dev/null || echo "[backup] could not reach the monitor address"
fi
echo "[backup] $(date -Is) done"
