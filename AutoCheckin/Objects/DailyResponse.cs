using System.Text.Json.Serialization;

namespace AutoCheckin.Objects
{
    public class DailyResponse
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
        bool IsCaptchaBlock => Data?.GTResult?.IsRisk ?? false;
        [JsonIgnore]
        bool IsError => Message != "OK";
        [JsonIgnore]
        public bool IsSuccess => (ReturnCode == -5003) || (!IsError && !IsCaptchaBlock);
    }
}
