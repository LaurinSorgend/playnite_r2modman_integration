using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace PlayniteR2modman
{
    public class GaleGame
    {
        public string SteamAppId { get; set; }
        public string GaleSlug { get; set; }
        public string DisplayName { get; set; }
    }

    /// <summary>Steam AppId to Gale slug lookup, read from the bundled Resources\gale_games.json.</summary>
    public class GaleGameCatalog
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly string bundledJsonPath;
        private Dictionary<string, GaleGame> games = new Dictionary<string, GaleGame>(StringComparer.Ordinal);

        public GaleGameCatalog(string pluginInstallDir)
        {
            bundledJsonPath = Path.Combine(pluginInstallDir, "Resources", "gale_games.json");
        }

        public bool TryGet(string steamAppId, out GaleGame game) => games.TryGetValue(steamAppId, out game);

        public void Reload(IDictionary<string, bool> disabledByAppId)
        {
            var loaded = new Dictionary<string, GaleGame>(StringComparer.Ordinal);
            foreach (var game in LoadBundled())
            {
                if (string.IsNullOrWhiteSpace(game.SteamAppId) || string.IsNullOrWhiteSpace(game.GaleSlug))
                {
                    continue;
                }
                if (disabledByAppId != null && disabledByAppId.ContainsKey(game.SteamAppId))
                {
                    continue;
                }
                loaded[game.SteamAppId] = game;
            }
            games = loaded;
            logger.Info($"Gale mappings loaded: {games.Count} entries (bundled file: {bundledJsonPath})");
        }

        private List<GaleGame> LoadBundled()
        {
            if (!File.Exists(bundledJsonPath))
            {
                logger.Warn($"Bundled Gale mapping file not found at {bundledJsonPath}");
                return new List<GaleGame>();
            }
            try
            {
                return Serialization.FromJson<List<GaleGame>>(File.ReadAllText(bundledJsonPath)) ?? new List<GaleGame>();
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to parse bundled Gale mapping file {bundledJsonPath}");
                return new List<GaleGame>();
            }
        }
    }
}
