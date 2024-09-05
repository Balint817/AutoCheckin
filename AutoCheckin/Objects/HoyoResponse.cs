using System.Text.Json.Serialization;

namespace AutoCheckin.Objects
{
    public class HoyoResponse
    {
        public class DailyResponse_Data
        {
            public class Data_GTResult
            {
                [JsonPropertyName("is_risk")]
                public bool IsRisk;
            }
            [JsonPropertyName("gt_result")]
            public Data_GTResult? GTResult { get; set; }
        }
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("retcode")]
        public int? ReturnCode { get; set; }

        [JsonPropertyName("data")]
        public DailyResponse_Data? Data { get; set; }

        [JsonIgnore]
        public bool IsCaptchaBlock => Data?.GTResult?.IsRisk ?? false;
        [JsonIgnore]
        public bool IsError => Message != "OK";
        [JsonIgnore]
        public bool IsSuccess => (ReturnCode == -5003) || (!IsError && !IsCaptchaBlock && !IsBusy && !IsMalformed && !NotLoggedIn && !MissingRegion);
        [JsonIgnore]
        public bool IsBusy => (ReturnCode == -1009) || (Message?.Contains("System busy", StringComparison.Ordinal) ?? false);
        [JsonIgnore]
        public bool IsMalformed => (ReturnCode == -502) || (Message?.Contains("went wrong", StringComparison.Ordinal) ?? false);
        [JsonIgnore]
        public bool NotLoggedIn => (ReturnCode == -1071) || (Message?.Contains("log in ", StringComparison.Ordinal) ?? false);
        [JsonIgnore]
        public bool MissingRegion => (ReturnCode == -1075) || (Message?.Contains("Create a character first", StringComparison.Ordinal) ?? false);
    }
}
