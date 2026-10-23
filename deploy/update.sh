#!/usr/bin/env bash
# Updates the site on the server with one command: takes the new code from GitHub, saves a backup first, rebuilds,
# starts the new version, checks that it really works and, if it does not, goes back to the version that worked.
#
#   bash update.sh              normal update
#   bash update.sh --force      rebuild even when there is no new code (after changing .env, for example)
#   bash update.sh --no-backup  skip the backup that is taken before the update (not recommended)
#
# Run it on the server. (Section 0 of README.md shows how to make the short command "rentox-update".)
set -euo pipefail

cd "$(dirname "$(readlink -f "$0")")"

FORCE=0
BACKUP=1
for arg in "$@"; do
  case "$arg" in
    --force) FORCE=1 ;;
    --no-backup) BACKUP=0 ;;
    *) echo "Unknown option: $arg (use --force or --no-backup)"; exit 2 ;;
  esac
done

say() { printf '\n[update] %s\n' "$*"; }
fail() { printf '\n[update] ERROR: %s\n' "$*" >&2; exit 1; }

[ -f .env ] || fail "deploy/.env was not found. This is not a set-up server."
command -v docker >/dev/null || fail "Docker is not installed."
git rev-parse --git-dir >/dev/null 2>&1 || fail "This folder is not a git copy of the project."

# What runs now (the backup service only if it is on).
SERVICES="caddy web api worker"
if docker compose ps --services --filter status=running | grep -qx backup; then
  SERVICES="$SERVICES backup"
fi

OLD=$(git rev-parse HEAD)
say "Current version: $(git log -1 --format='%h %s' "$OLD")"

say "Taking the new code from GitHub"
git fetch --quiet origin
git merge --ff-only '@{u}' >/dev/null 2>&1 || fail "The server copy cannot be moved forward cleanly (local changes?). Nothing was changed."
NEW=$(git rev-parse HEAD)

if [ "$OLD" = "$NEW" ] && [ "$FORCE" -eq 0 ]; then
  say "Already the newest version. Nothing to do (use --force to rebuild anyway)."
  exit 0
fi
say "New version: $(git log -1 --format='%h %s' "$NEW")"

if [ "$BACKUP" -eq 1 ] && echo "$SERVICES" | grep -qw backup; then
  say "Saving a backup before the update"
  docker compose exec -T backup /scripts-default/backup.sh || fail "The backup failed, so the update was stopped. The site still runs the old version."
fi

go_back() {
  say "Going back to the version that worked ($(git log -1 --format=%h "$OLD"))"
  git reset --hard "$OLD" >/dev/null
  docker compose build
  docker compose up -d $SERVICES
}

say "Building (this can take several minutes; the site keeps running the old version meanwhile)"
if ! docker compose build; then
  git reset --hard "$OLD" >/dev/null
  fail "The build failed. The site still runs the old version and the code was put back."
fi

say "Starting the new version"
docker compose up -d $SERVICES

say "Checking that it works (up to 2 minutes)"
healthy=0
for _ in $(seq 1 24); do
  api_id=$(docker compose ps -q api || true)
  state=""
  [ -n "$api_id" ] && state=$(docker inspect --format '{{.State.Health.Status}}' "$api_id" 2>/dev/null || true)
  if [ "$state" = "healthy" ] && docker compose exec -T web node -e \
      "fetch('http://127.0.0.1:3000/').then(r=>process.exit(r.status<500?0:1)).catch(()=>process.exit(1))" >/dev/null 2>&1; then
    healthy=1
    break
  fi
  sleep 5
done

if [ "$healthy" -ne 1 ]; then
  docker compose logs --tail 40 api || true
  go_back
  fail "The new version did not become healthy, so the old code was started again. (The database was saved before the update; if a database change already ran, check the log above.)"
fi

say "Cleaning old build leftovers"
docker image prune -f >/dev/null

say "Done: $(git log -1 --format='%h %s')"
docker compose ps
