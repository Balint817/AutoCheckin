using System.Text.Json.Serialization;

namespace AutoCheckin.Objects
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class Settings
    {
        [JsonPropertyName("checkin")]
        public bool CheckinEnabled { get; set; }

        [JsonPropertyName("codeRegions")]
        public List<RegionUID> Regions { get; set; }
        public IEnumerable<RegionUID> GetEnabledRegions()
        {
            foreach (var region in Regions)
            {
                if (region.CodeRedeemEnabled)
                {
                    yield return region;
                }
            }
        }
        public void EnsureNotNull()
        {
            Regions ??= new();
            Regions = Regions.Where(x => x != null).ToList();

            foreach (var requiredRegion in new string[] { "eu", "usa", "asia", "cht" })
            {
                if (!Regions.Any(x => x.RegionKey == requiredRegion))
                {
                    Regions.Add(new RegionUID(requiredRegion));
                }
            }
        }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
