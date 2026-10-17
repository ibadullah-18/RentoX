#!/bin/bash
# Runs backup.sh once a day at BACKUP_HOUR (Baku time) and keeps going whatever happens to a single run.
set -u

if [ -z "${RESTIC_REPOSITORY:-}" ] || [ -z "${RESTIC_PASSWORD:-}" ]; then
  echo "[backup] RESTIC_REPOSITORY / RESTIC_PASSWORD are empty: backups are OFF. Fill them in .env and restart this service."
  exec sleep infinity
fi

hour="${BACKUP_HOUR:-3}"
case "$hour" in
  ''|*[!0-9]*) echo "[backup] BACKUP_HOUR must be a number 0-23, not '$hour'"; exec sleep infinity ;;
esac
if [ "$hour" -gt 23 ]; then
  echo "[backup] BACKUP_HOUR must be 0-23, not '$hour'"
  exec sleep infinity
fi

echo "[backup] ready: a backup every day at ${hour}:00 (Baku time)"

# Seconds from now until the next BACKUP_HOUR:00 on the local (Baku) clock. Plain arithmetic: no GNU date needed.
seconds_until_backup() {
  local h m s now target wait
  h=$((10#$(date +%H))); m=$((10#$(date +%M))); s=$((10#$(date +%S)))
  now=$((h * 3600 + m * 60 + s))
  target=$((hour * 3600))
  wait=$((target - now))
  if [ "$wait" -le 60 ]; then
    wait=$((wait + 86400))
  fi
  echo "$wait"
}

while true; do
  sleep "$(seconds_until_backup)"
  /scripts-default/backup.sh || echo "[backup] FAILED at $(date -Is)"
  # Never twice in a row, whatever the clock does.
  sleep 120
done
