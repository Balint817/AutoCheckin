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
        public bool IsBusy => CheckResponse(-1009, "System busy");
        [JsonIgnore]
        public bool IsMalformed => CheckResponse(-502, "went wrong");
        [JsonIgnore]
        public bool NotLoggedIn => CheckResponse(-1071, "log in ");
        [JsonIgnore]
        public bool MissingRegion => CheckResponse(-1075, "Create a character first");
        bool CheckResponse(int code, string substring) => (ReturnCode == code) || (Message?.Contains(substring, StringComparison.Ordinal) ?? false);
    }
}
