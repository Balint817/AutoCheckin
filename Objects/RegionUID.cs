using System.Text.Json.Serialization;

namespace AutoCheckin.Objects
{
    public class RegionUID
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public RegionUID(string regionKey)
        {
            ArgumentNullException.ThrowIfNull(regionKey, nameof(regionKey));
            RegionKey = regionKey;
            EnsureNotNull();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonPropertyName("redeemCodes")]
        public bool CodeRedeemEnabled { get; set; }

        [JsonPropertyName("uid")]
        public string UID { get; set; }

        [JsonPropertyName("regionKey")]
        public string RegionKey { get; set; }

        public static implicit operator bool(RegionUID self)
        {
            return self.CodeRedeemEnabled;
        }
        public void EnsureNotNull()
        {
            UID ??= string.Empty;
            UID = UID.Trim();

            RegionKey ??= string.Empty;
            RegionKey = RegionKey.Trim();
        }

        [JsonIgnore]
        public bool IsUIDValid => !string.IsNullOrWhiteSpace(UID) && UID.All(c => '0' <= c && c <= '9');

        [JsonIgnore]
        public bool IsValid => IsUIDValid && !string.IsNullOrWhiteSpace(RegionKey);
    }
}
