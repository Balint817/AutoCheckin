using System.Text.Json.Serialization;
using System.Text.Json;

namespace Constants
{
    public static class Values
    {
        public const string SettingsPath = "settings.json";
        public const string TriedCodesPath = "triedCodes.json";
        public const string LastVersionKey = "lastSkippedVersion";
        public const string UpdateFolder = "download";
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.Strict,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true,
        };
    }
}
