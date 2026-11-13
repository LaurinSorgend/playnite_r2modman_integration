using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayniteR2modman
{
    /// <summary>Lists Gale profiles, which live at &lt;data folder&gt;\&lt;slug&gt;\profiles\&lt;profile name&gt;.</summary>
    public class GaleScanner
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public static string DefaultDataFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "com.kesomannen.gale");

        public static bool TryResolveDataFolder(string dataFolder, out string resolved, out string error) =>
            PathResolver.TryResolve(dataFolder, DefaultDataFolder, out resolved, out error);

        public IReadOnlyList<string> ListProfiles(string dataFolder, string galeSlug)
        {
            if (!TryResolveDataFolder(dataFolder, out var resolved, out var error))
            {
                logger.Warn($"Gale data folder override rejected: {error}");
                return Array.Empty<string>();
            }
            if (!R2modmanScanner.IsValidFolderName(galeSlug))
            {
                logger.Warn($"Unusable Gale slug '{galeSlug}'.");
                return Array.Empty<string>();
            }

            var profilesRoot = Path.Combine(resolved, galeSlug, "profiles");
            if (!Directory.Exists(profilesRoot))
            {
                logger.Debug($"No Gale profiles folder for {galeSlug} (expected {profilesRoot}).");
                return Array.Empty<string>();
            }

            try
            {
                var names = Directory.EnumerateDirectories(profilesRoot).Select(Path.GetFileName).ToList();
                logger.Info($"Discovered {names.Count} Gale profiles for {galeSlug}: {string.Join(", ", names)}");
                return names;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to enumerate {profilesRoot}");
                return Array.Empty<string>();
            }
        }
    }
}
