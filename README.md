# Playnite r2modman integration

Playnite plugin that adds launch entries for [r2modman](https://r2modman.com/) profiles when you click Play on a supported Steam game. Profiles are read live from disk, no config to keep in sync.

Status: WIP. Covers BepInEx games on Steam.

## Build

Needs VS Build Tools 2022 with the .NET Framework 4.6.2 targeting pack.

```powershell
$msbuild = "path_to_ms_build"
& $msbuild "src\PlayniteR2modman\PlayniteR2modman.sln" -t:Restore -p:RestorePackagesConfig=true
& $msbuild "src\PlayniteR2modman\PlayniteR2modman.csproj" -p:Configuration=Debug
```

Output: `src\PlayniteR2modman\bin\Debug\`.

## Install

Download the `.pext` from [Releases](https://codeberg.org/LaurinS/playnite_r2modman_integration/releases) and double-click to install.

Dev build: Playnite → **Settings → For developers → External extensions** → add the `bin\Debug` folder → restart.


## Adding games

The bundled list at `src/PlayniteR2modman/Resources/r2modman_games.json` is generated from r2modman's upstream `ecosystem.json`. To regenerate:

```bash
python scripts/update_game_mappings.py
```

CI re-runs this daily and fails if the bundled file drifts. For a one-off, just add a settings override row.