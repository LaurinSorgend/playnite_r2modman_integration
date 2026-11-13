using System;
using System.IO;

namespace PlayniteR2modman
{
    public static class PathResolver
    {
        /// <summary>
        /// Resolves a user-supplied absolute path, or returns <paramref name="fallback"/> when the value is empty.
        /// Reports why the value is unusable instead of throwing.
        /// </summary>
        public static bool TryResolve(string value, string fallback, out string resolved, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                resolved = fallback;
                return true;
            }

            resolved = null;
            var trimmed = value.Trim().Trim('"');
            try
            {
                if (!Path.IsPathRooted(trimmed))
                {
                    error = $"'{trimmed}' is not an absolute path.";
                    return false;
                }
                resolved = Path.GetFullPath(trimmed);
                return true;
            }
            catch (Exception ex)
            {
                error = $"'{trimmed}' is not a usable path: {ex.Message}";
                return false;
            }
        }
    }
}
