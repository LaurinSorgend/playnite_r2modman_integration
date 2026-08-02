using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayniteR2modman
{
    public class GameMapping
    {
        public string SteamAppId { get; set; }
        public string R2modmanInternalName { get; set; }
        public string DisplayName { get; set; }
        public bool IsBundled { get; set; }
    }

    public class UserMappingOverride
    {
        public string R2modmanInternalName { get; set; }
        public string DisplayName { get; set; }
    }

    public class GameMappingStore
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly string bundledJsonPath;
        private Dictionary<string, GameMapping> mappings = new Dictionary<string, GameMapping>(StringComparer.Ordinal);

        public GameMappingStore(string pluginInstallDir)
        {
            bundledJsonPath = Path.Combine(pluginInstallDir, "Resources", "r2modman_games.json");
        }

        public IReadOnlyCollection<GameMapping> All => mappings.Values.ToList();

        public bool TryGet(string steamAppId, out GameMapping mapping) => mappings.TryGetValue(steamAppId, out mapping);

        public void Reload(IDictionary<string, UserMappingOverride> userOverrides, IDictionary<string, bool> disabledByAppId)
        {
            var loaded = new Dictionary<string, GameMapping>(StringComparer.Ordinal);

            foreach (var entry in LoadBundled())
            {
                entry.IsBundled = true;
                loaded[entry.SteamAppId] = entry;
            }

            if (userOverrides != null)
            {
                foreach (var kv in userOverrides)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value?.R2modmanInternalName))
                    {
                        continue;
                    }

                    loaded[kv.Key] = new GameMapping
                    {
                        SteamAppId = kv.Key,
                        R2modmanInternalName = kv.Value.R2modmanInternalName,
                        DisplayName = ResolveDisplayName(kv.Key, kv.Value, loaded),
                        IsBundled = false
                    };
                }
            }

            if (disabledByAppId != null)
            {
                foreach (var kv in disabledByAppId.Where(p => p.Value))
                {
                    loaded.Remove(kv.Key);
                }
            }

            mappings = loaded;
            logger.Info($"r2modman mappings loaded: {mappings.Count} entries (bundled file: {bundledJsonPath})");
        }

        private static string ResolveDisplayName(string steamAppId, UserMappingOverride userOverride, Dictionary<string, GameMapping> loaded)
        {
            if (!string.IsNullOrWhiteSpace(userOverride.DisplayName))
            {
                return userOverride.DisplayName.Trim();
            }
            return loaded.TryGetValue(steamAppId, out var existing) && !string.IsNullOrWhiteSpace(existing.DisplayName)
                ? existing.DisplayName
                : userOverride.R2modmanInternalName;
        }

        private List<GameMapping> LoadBundled()
        {
            if (!File.Exists(bundledJsonPath))
            {
                logger.Warn($"Bundled mapping file not found at {bundledJsonPath}");
                return new List<GameMapping>();
            }

            try
            {
                var json = File.ReadAllText(bundledJsonPath);
                return Serialization.FromJson<List<GameMapping>>(json) ?? new List<GameMapping>();
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to parse bundled mapping file {bundledJsonPath}");
                return new List<GameMapping>();
            }
        }
    }
}
