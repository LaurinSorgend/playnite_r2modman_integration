#!/usr/bin/env python3
"""Regenerate Resources/r2modman_games.json from r2modman's upstream ecosystem.json.

Filters to BepInEx games that have a Steam (or steam-direct) distribution.
Output is sorted by numeric Steam AppId so CI drift detection (`git diff
--exit-code`) is reliable.

Stdlib only - runs on `python:3.12-alpine` without extra deps.
"""
from __future__ import annotations

import argparse
import json
import sys
import urllib.request
from pathlib import Path

DEFAULT_SOURCE_URL = (
    "https://raw.githubusercontent.com/ebkr/r2modmanPlus/master/"
    "src/assets/data/ecosystem.json"
)
REPO_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_OUTPUT = REPO_ROOT / "src" / "PlayniteR2modman" / "Resources" / "r2modman_games.json"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-url", default=DEFAULT_SOURCE_URL)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    return parser.parse_args()


def fetch_ecosystem(url: str) -> dict:
    with urllib.request.urlopen(url, timeout=30) as resp:
        return json.loads(resp.read().decode("utf-8"))


def extract_bepinex_steam_games(ecosystem: dict) -> list[dict]:
    rows: list[dict] = []
    for game in (ecosystem.get("games") or {}).values():
        for entry in game.get("r2modman") or []:
            if entry.get("packageLoader") != "bepinex":
                continue
            steam_id = next(
                (
                    str(d.get("identifier"))
                    for d in entry.get("distributions") or []
                    if d.get("platform") in ("steam", "steam-direct") and d.get("identifier")
                ),
                None,
            )
            if not steam_id:
                continue
            rows.append({
                "steamAppId": steam_id,
                "r2modmanInternalName": str(entry.get("internalFolderName", "")),
                "displayName": str((game.get("meta") or {}).get("displayName", "")),
            })
    return rows


def deduplicate_and_sort(rows: list[dict]) -> list[dict]:
    """Keeps the first row per Steam AppId, dropping non-numeric ids so a bad
    upstream entry cannot abort the whole run. Both cases are reported."""
    by_app_id: dict[str, dict] = {}
    for row in rows:
        app_id = row["steamAppId"]
        if not (app_id.isascii() and app_id.isdigit()):
            print(
                f"Skipping non-numeric Steam AppId {app_id!r} "
                f"({row['r2modmanInternalName']!r})",
                file=sys.stderr,
            )
            continue
        kept = by_app_id.setdefault(app_id, row)
        if kept is not row:
            print(
                f"Duplicate Steam AppId {app_id}: keeping "
                f"{kept['r2modmanInternalName']!r}, dropping "
                f"{row['r2modmanInternalName']!r}",
                file=sys.stderr,
            )
    return sorted(by_app_id.values(), key=lambda r: int(r["steamAppId"]))


def main() -> int:
    args = parse_args()
    print(f"Source : {args.source_url}", file=sys.stderr)
    print(f"Output : {args.output}", file=sys.stderr)

    ecosystem = fetch_ecosystem(args.source_url)
    rows = deduplicate_and_sort(extract_bepinex_steam_games(ecosystem))

    if not rows:
        print("Extracted zero entries - refusing to overwrite the bundle.", file=sys.stderr)
        return 1

    args.output.parent.mkdir(parents=True, exist_ok=True)
    text = json.dumps(rows, indent=2, ensure_ascii=False) + "\n"
    args.output.write_text(text, encoding="utf-8", newline="\n")
    print(f"Wrote {len(rows)} BepInEx Steam game entries.", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
