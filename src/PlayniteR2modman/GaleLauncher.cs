using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PlayniteR2modman
{
    /// <summary>
    /// Builds play actions that run <c>gale.exe --game &lt;slug&gt; --profile &lt;name&gt; --launch</c>,
    /// the same command Gale writes into its own desktop shortcuts.
    /// </summary>
    public static class GaleLauncher
    {
        /// <summary>Install locations of the NSIS (per-user), MSI (per-machine) and scoop packages.</summary>
        public static IEnumerable<string> DefaultExecutableCandidates()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var scoopRoot = Environment.GetEnvironmentVariable("SCOOP");
            if (string.IsNullOrWhiteSpace(scoopRoot))
            {
                scoopRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop");
            }

            yield return Path.Combine(localAppData, "Gale", "gale.exe");
            yield return Path.Combine(programFiles, "Gale", "gale.exe");
            yield return Path.Combine(scoopRoot, "apps", "gale", "current", "PFiles", "Gale", "gale.exe");
        }

        public static bool TryResolveExecutable(string executableOverride, out string executable, out string error)
        {
            if (!string.IsNullOrWhiteSpace(executableOverride))
            {
                return PathResolver.TryResolve(executableOverride, null, out executable, out error);
            }
            error = null;
            executable = DefaultExecutableCandidates().FirstOrDefault(File.Exists);
            if (executable == null)
            {
                error = "gale.exe not found in the default install locations; set the Gale executable path in settings.";
                return false;
            }
            return true;
        }

        public static AutomaticPlayController BuildPlayController(Game game, string executable, string galeSlug, string profileName)
        {
            var controller = new AutomaticPlayController(game)
            {
                Type = AutomaticPlayActionType.File,
                Name = $"Modded ({profileName}) via Gale",
                Path = executable,
                Arguments = BuildArguments(executable, galeSlug, profileName),
                WorkingDir = Path.GetDirectoryName(executable),
                TrackingMode = TrackingMode.Default
            };

            if (!string.IsNullOrWhiteSpace(game.InstallDirectory))
            {
                controller.TrackingMode = TrackingMode.Directory;
                controller.TrackingPath = game.InstallDirectory;
            }
            return controller;
        }

        /// <summary>
        /// Omits <c>--no-gui</c> while Gale is open: the running instance receives the forwarded
        /// arguments and would otherwise exit, closing the user's window.
        /// </summary>
        private static string BuildArguments(string executable, string galeSlug, string profileName)
        {
            var args = $"--game \"{galeSlug}\" --profile \"{profileName}\" --launch";
            return IsRunning(executable) ? args : args + " --no-gui";
        }

        private static bool IsRunning(string executable)
        {
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(executable));
            try
            {
                return processes.Length > 0;
            }
            finally
            {
                foreach (var process in processes)
                {
                    process.Dispose();
                }
            }
        }
    }
}
