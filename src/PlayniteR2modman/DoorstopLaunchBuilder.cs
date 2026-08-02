using System;

namespace PlayniteR2modman
{
    /// <summary>Doorstop generations differ in flag spelling; BepInEx 5 ships 3, BepInEx 6 ships 4.</summary>
    public enum DoorstopGeneration
    {
        Three,
        Four
    }

    public static class DoorstopLaunchBuilder
    {
        public static string BuildSteamRunUrl(string steamAppId, string preloaderDllAbsolutePath, DoorstopGeneration generation)
        {
            if (string.IsNullOrWhiteSpace(steamAppId))
            {
                throw new ArgumentException("steamAppId is required", nameof(steamAppId));
            }
            if (string.IsNullOrWhiteSpace(preloaderDllAbsolutePath))
            {
                throw new ArgumentException("preloaderDllAbsolutePath is required", nameof(preloaderDllAbsolutePath));
            }

            var enableFlag = generation == DoorstopGeneration.Four ? "--doorstop-enabled" : "--doorstop-enable";
            var targetFlag = generation == DoorstopGeneration.Four ? "--doorstop-target-assembly" : "--doorstop-target";
            var args = $"\"{enableFlag}\" \"true\" \"{targetFlag}\" \"{preloaderDllAbsolutePath}\"";
            var encoded = Uri.EscapeDataString(args);
            return $"steam://run/{steamAppId}//{encoded}/";
        }
    }
}
