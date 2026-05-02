using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayniteR2modman
{
    public class DiscoveredProfile
    {
        public string ProfileName { get; set; }
        public string PreloaderPath { get; set; }
        public string SteamRunUrl { get; set; }
    }

    public class R2modmanScanner
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private const string PreloaderRelativePath = @"BepInEx\core\BepInEx.Preloader.dll";

        public static string DefaultDataFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "r2modmanPlus-local");

        public IReadOnlyList<DiscoveredProfile> ScanGame(string dataFolder, string r2modmanInternalName, string steamAppId)
        {
            var results = new List<DiscoveredProfile>();
            var resolvedDataFolder = string.IsNullOrWhiteSpace(dataFolder) ? DefaultDataFolder : dataFolder;
            var gameRoot = Path.Combine(resolvedDataFolder, r2modmanInternalName);
            var profilesRoot = Path.Combine(gameRoot, "profiles");

            if (!Directory.Exists(gameRoot))
            {
                logger.Debug($"r2modman game folder absent for {r2modmanInternalName} (expected {gameRoot}); no actions injected.");
                return results;
            }
            if (!Directory.Exists(profilesRoot))
            {
                logger.Warn($"r2modman game folder present but profiles\\ missing for {r2modmanInternalName}: {profilesRoot}");
                return results;
            }

            foreach (var profileDir in Directory.EnumerateDirectories(profilesRoot))
            {
                var preloader = Path.Combine(profileDir, PreloaderRelativePath);
                if (!File.Exists(preloader))
                {
                    logger.Debug($"Skipping profile (preloader missing): {profileDir}");
                    continue;
                }

                var profileName = Path.GetFileName(profileDir);
                results.Add(new DiscoveredProfile
                {
                    ProfileName = profileName,
                    PreloaderPath = preloader,
                    SteamRunUrl = DoorstopLaunchBuilder.BuildSteamRunUrl(steamAppId, preloader)
                });
            }

            if (results.Count == 0)
            {
                logger.Info($"No usable profiles found for {r2modmanInternalName} under {profilesRoot}");
            }
            else
            {
                logger.Info($"Discovered {results.Count} modded profiles for {r2modmanInternalName}: {string.Join(", ", results.Select(r => r.ProfileName))}");
            }
            return results;
        }

        public IReadOnlyList<string> ListGameFolders(string dataFolder)
        {
            var resolvedDataFolder = string.IsNullOrWhiteSpace(dataFolder) ? DefaultDataFolder : dataFolder;
            if (!Directory.Exists(resolvedDataFolder))
            {
                return Array.Empty<string>();
            }
            return Directory.EnumerateDirectories(resolvedDataFolder).Select(Path.GetFileName).ToList();
        }
    }
}
