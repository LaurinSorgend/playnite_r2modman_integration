# Playnite r2modman integration

Playnite plugin that adds launch entries for [r2modman](https://r2modman.com/) and [Gale](https://github.com/Kesomannen/gale) profiles when you click Play on a supported Steam game. Profiles are read live from disk, no config to keep in sync.

Status: WIP. r2modman entries cover BepInEx games on Steam. Gale entries run `gale.exe --game <slug> --profile <name> --launch`, so they work for any loader Gale supports.

## Build

Needs VS Build Tools 2022 with the .NET Framework 4.6.2 targeting pack.

```powershell
$msbuild = "path_to_ms_build"
& $msbuild "src\PlayniteR2modman\PlayniteR2modman.sln" -t:Restore -p:RestorePackagesConfig=true
& $msbuild "src\PlayniteR2modman\PlayniteR2modman.csproj" -p:Configuration=Debug
```

Output: `src\PlayniteR2modman\bin\Debug\`.

## Install

Download the `.pext` from [Releases](https://github.com/LaurinSorgend/playnite_r2modman_integration/releases) and double-click to install.

Dev build: Playnite → **Settings → For developers → External extensions** → add the `bin\Debug` folder → restart.


## Adding games

The bundled lists in `src/PlayniteR2modman/Resources/` are generated from upstream: `r2modman_games.json` from r2modman's `ecosystem.json`, `gale_games.json` from Gale's `games.json`. To regenerate:

```bash
python scripts/update_game_mappings.py
python scripts/update_gale_game_mappings.py
```

CI re-runs both daily and commits any changes to `main`. For a one-off r2modman game, add a settings override row.

## Gale

Gale keeps profiles in `%AppData%\com.kesomannen.gale\<slug>\profiles\`. The plugin looks for `gale.exe` in the per-user installer folder, `Program Files\Gale` and scoop. If yours is elsewhere, or you moved Gale's data folder, set the paths in the plugin settings.