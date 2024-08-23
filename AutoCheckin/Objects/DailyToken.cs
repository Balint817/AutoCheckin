using System.Text.Json.Serialization;

namespace AutoCheckin.Objects
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class DailyToken : AbstractToken
    {
        [JsonPropertyName("ltoken_v2")]
        public override string Token { get; set; }
        [JsonPropertyName("ltmid_v2")]
        public override string Mid { get; set; }
        [JsonPropertyName("ltuid_v2")]
        public override string ID { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
