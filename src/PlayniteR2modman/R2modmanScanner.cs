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

        private class LoaderVariant
        {
            public LoaderVariant(string relativePath, DoorstopGeneration doorstop)
            {
                RelativePath = relativePath;
                Doorstop = doorstop;
            }

            public string RelativePath { get; }
            public DoorstopGeneration Doorstop { get; }
        }

        /// <summary>Probed in order, so a BepInEx 6 layout wins over a leftover BepInEx 5 preloader.</summary>
        private static readonly LoaderVariant[] LoaderVariants =
        {
            new LoaderVariant(@"BepInEx\core\BepInEx.Unity.IL2CPP.dll", DoorstopGeneration.Four),
            new LoaderVariant(@"BepInEx\core\BepInEx.Unity.Mono.Preloader.dll", DoorstopGeneration.Four),
            new LoaderVariant(@"BepInEx\core\BepInEx.Preloader.dll", DoorstopGeneration.Three)
        };

        public static string DefaultDataFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "r2modmanPlus-local");

        /// <summary>Resolves a user-supplied data folder, reporting why it is unusable instead of throwing.</summary>
        public static bool TryResolveDataFolder(string dataFolder, out string resolved, out string error) =>
            PathResolver.TryResolve(dataFolder, DefaultDataFolder, out resolved, out error);

        public static bool IsValidFolderName(string name) =>
            !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

        public IReadOnlyList<DiscoveredProfile> ScanGame(string dataFolder, string r2modmanInternalName, string steamAppId)
        {
            var results = new List<DiscoveredProfile>();
            if (!TryResolveDataFolder(dataFolder, out var resolvedDataFolder, out var folderError))
            {
                logger.Warn($"r2modman data folder override rejected: {folderError}");
                return results;
            }
            if (!IsValidFolderName(r2modmanInternalName))
            {
                logger.Warn($"Mapping for Steam AppId {steamAppId} has an unusable r2modman folder name '{r2modmanInternalName}'.");
                return results;
            }

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

            foreach (var profileDir in EnumerateDirectories(profilesRoot))
            {
                var profile = BuildProfile(profileDir, steamAppId);
                if (profile != null)
                {
                    results.Add(profile);
                }
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
            if (!TryResolveDataFolder(dataFolder, out var resolvedDataFolder, out _) || !Directory.Exists(resolvedDataFolder))
            {
                return Array.Empty<string>();
            }
            return EnumerateDirectories(resolvedDataFolder).Select(Path.GetFileName).ToList();
        }

        private static DiscoveredProfile BuildProfile(string profileDir, string steamAppId)
        {
            foreach (var variant in LoaderVariants)
            {
                var preloader = Path.Combine(profileDir, variant.RelativePath);
                if (!File.Exists(preloader))
                {
                    continue;
                }

                return new DiscoveredProfile
                {
                    ProfileName = Path.GetFileName(profileDir),
                    PreloaderPath = preloader,
                    SteamRunUrl = DoorstopLaunchBuilder.BuildSteamRunUrl(steamAppId, preloader, variant.Doorstop)
                };
            }

            logger.Info($"Skipping profile (no supported BepInEx preloader under BepInEx\\core): {profileDir}");
            return null;
        }

        private static IReadOnlyList<string> EnumerateDirectories(string root)
        {
            try
            {
                return Directory.EnumerateDirectories(root).ToList();
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to enumerate {root}");
                return Array.Empty<string>();
            }
        }
    }
}
