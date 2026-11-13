#!/usr/bin/env bash
# Usage: sync_mapping.sh <mapping-file> <extractor-script>
#
# After an extractor ran, fails a pull request whose mapping drifted from
# upstream, or commits and pushes the refreshed mapping on any other event.
# Rebases onto the latest main first so sequential jobs don't race each other.
# Pushing relies on the credentials actions/checkout leaves in the git config.
set -euo pipefail

mapping="${1:?mapping file required}"
extractor="${2:?extractor script required}"
name="$(basename "$mapping")"

if git diff --quiet -- "$mapping"; then
  echo "$name is already in sync with upstream."
  exit 0
fi

if [ "${GITHUB_EVENT_NAME:?}" = "pull_request" ]; then
  git diff -- "$mapping"
  echo "::error::$name is out of sync with upstream."
  echo "Run 'python $extractor' locally and commit the result."
  exit 1
fi

git config user.name "github-actions[bot]"
git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
git commit -q -m "Update $name" -- "$mapping"
git pull -q --rebase origin main
git push origin HEAD:main
