#!/usr/bin/env python3
"""Regenerate Resources/gale_games.json from Gale's upstream games.json.

Keeps every game that has a Steam AppId, regardless of mod loader, because the
plugin launches through Gale's CLI and Gale handles the loader itself. Output
is sorted by numeric Steam AppId so CI drift detection (`git diff
--exit-code`) is reliable.

Stdlib only - runs on `python:3.12-alpine` without extra deps.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from update_game_mappings import REPO_ROOT, deduplicate_and_sort, fetch_json, write_rows

DEFAULT_SOURCE_URL = (
    "https://raw.githubusercontent.com/Kesomannen/gale/refs/heads/master/"
    "src-tauri/games.json"
)
DEFAULT_OUTPUT = REPO_ROOT / "src" / "PlayniteR2modman" / "Resources" / "gale_games.json"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-url", default=DEFAULT_SOURCE_URL)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    return parser.parse_args()


def split_words(text: str) -> list[str]:
    """Port of the word splitting in Rust's `heck` crate, which Gale uses to
    derive a missing slug. Non-alphanumerics separate words; inside a run, a
    boundary falls after a lowercase char followed by an uppercase one, and
    before the last capital of an acronym followed by lowercase (`HTTPServer`
    -> `HTTP`, `Server`). Digits keep the case mode of the char before them."""
    words: list[str] = []
    for chunk in "".join(c if c.isalnum() else " " for c in text).split():
        start, mode = 0, None
        for i, c in enumerate(chunk[:-1]):
            nxt = chunk[i + 1]
            next_mode = "lower" if c.islower() else "upper" if c.isupper() else mode
            if next_mode == "lower" and nxt.isupper():
                words.append(chunk[start:i + 1])
                start, mode = i + 1, None
                continue
            if mode == "upper" and c.isupper() and nxt.islower():
                words.append(chunk[start:i])
                start = i
            mode = next_mode
        words.append(chunk[start:])
    return words


def to_kebab_case(text: str) -> str:
    return "-".join(word.lower() for word in split_words(text))


def extract_steam_games(games: list[dict]) -> list[dict]:
    rows: list[dict] = []
    for game in games:
        steam = (game.get("platforms") or {}).get("steam") or {}
        if steam.get("id") is None:
            continue
        name = str(game.get("name", ""))
        rows.append({
            "steamAppId": str(steam["id"]),
            "galeSlug": str(game.get("slug") or to_kebab_case(name)),
            "displayName": name,
        })
    return rows


def main() -> int:
    args = parse_args()
    print(f"Source : {args.source_url}", file=sys.stderr)
    print(f"Output : {args.output}", file=sys.stderr)

    games = fetch_json(args.source_url)
    if not isinstance(games, list):
        print("Upstream games.json is not a JSON array.", file=sys.stderr)
        return 1
    rows = deduplicate_and_sort(extract_steam_games(games), name_key="galeSlug")
    return write_rows(rows, args.output, "Gale Steam game")


if __name__ == "__main__":
    raise SystemExit(main())
