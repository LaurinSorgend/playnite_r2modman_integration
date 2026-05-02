using System;

namespace PlayniteR2modman
{
    public static class DoorstopLaunchBuilder
    {
        public static string BuildSteamRunUrl(string steamAppId, string preloaderDllAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(steamAppId))
            {
                throw new ArgumentException("steamAppId is required", nameof(steamAppId));
            }
            if (string.IsNullOrWhiteSpace(preloaderDllAbsolutePath))
            {
                throw new ArgumentException("preloaderDllAbsolutePath is required", nameof(preloaderDllAbsolutePath));
            }

            var args = $"\"--doorstop-enable\" \"true\" \"--doorstop-target\" \"{preloaderDllAbsolutePath}\"";
            var encoded = Uri.EscapeDataString(args);
            return $"steam://run/{steamAppId}//{encoded}/";
        }
    }
}
