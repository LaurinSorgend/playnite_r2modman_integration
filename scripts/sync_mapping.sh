#!/usr/bin/env bash
# Usage: sync_mapping.sh <mapping-file> <extractor-script>
#
# After an extractor ran, fails a pull request whose mapping drifted from
# upstream, or commits and pushes the refreshed mapping on any other event.
# Rebases onto the latest main first so sequential jobs don't race each other.
set -euo pipefail

mapping="${1:?mapping file required}"
extractor="${2:?extractor script required}"
name="$(basename "$mapping")"

if git diff --quiet -- "$mapping"; then
  echo "$name is already in sync with upstream."
  exit 0
fi

if [ "${FORGEJO_EVENT_NAME:?}" = "pull_request" ]; then
  git diff -- "$mapping"
  echo "::error::$name is out of sync with upstream."
  echo "Run 'python $extractor' locally and commit the result."
  exit 1
fi

if [ -z "${FORGEJO_TOKEN:-}" ]; then
  echo "::error::No automatic token available; cannot push the refreshed mapping."
  exit 1
fi

host="${FORGEJO_SERVER_URL:?}"
remote="https://forgejo-actions:${FORGEJO_TOKEN}@${host#https://}/${FORGEJO_REPOSITORY:?}.git"

git config user.name "forgejo-actions"
git config user.email "forgejo-actions@noreply.codeberg.org"
git commit -q -m "Update $name" -- "$mapping"
git pull -q --rebase "$remote" main
git push "$remote" HEAD:main
