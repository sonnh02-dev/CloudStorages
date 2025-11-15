using Amazon.Runtime.Internal;

namespace CloudStorages.Server.Utils
{
    public static class PathHelper
    {

        public static string NormalizePrefix(string? inputPrefix)
        {
            var normalized = inputPrefix?.Trim().TrimStart('/').TrimEnd('/') ?? string.Empty;

            var allowedPrefixes = new[] { "uploads", "avatars", "documents" };

            if (!allowedPrefixes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                return "uploads";

            return normalized;

        }


        public static string BuildKey(string? prefix)
        {
            var normalizedPrefix = NormalizePrefix(prefix);
            return $"{normalizedPrefix}/{Guid.NewGuid()}";
        }
    }
}
