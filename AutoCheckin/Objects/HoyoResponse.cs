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
        // TODO: Figure out where I got this return code from 💀💀💀
        public bool IsSuccess => (ReturnCode == -5003) || (!IsError && !IsCaptchaBlock && !IsBusy && !IsMalformed && !NotLoggedIn && !MissingRegion);


        [JsonIgnore]
        public bool IsValidCode => CheckResponse(0, "Redeemed successfully");
        [JsonIgnore]
        public bool IsInvalidCode => CheckResponse(-2003, "Invalid ");
        [JsonIgnore]
        public bool IsUsedCode => CheckResponse(-2017, "already in use");
        [JsonIgnore]
        public bool IsExpiredCode => CheckResponse(-2001, "expired");
        [JsonIgnore]
        public bool CooldownError => CheckResponse(-2016, "Redemption in cooldown");


        [JsonIgnore]
        public bool IsBusy => CheckResponse(-1009, "System busy");
        [JsonIgnore]
        public bool IsMalformed => CheckResponse(-502, "went wrong");
        [JsonIgnore]
        public bool NotLoggedIn => CheckResponse(-1071, "log in ");
        [JsonIgnore]
        public bool MissingRegion => CheckResponse(-1075, "Create a character first");
        bool CheckResponse(int code, string substring) => (ReturnCode == code) || (Message?.Contains(substring, StringComparison.Ordinal) ?? false);
        bool CheckResponse(string substring) => (Message?.Contains(substring, StringComparison.Ordinal) ?? false);
        bool CheckResponse(string substring, params int[] codes) => (Message?.Contains(substring, StringComparison.Ordinal) ?? false)
            || (ReturnCode.HasValue
                ? codes.Contains(ReturnCode.Value)
                : (codes == null || codes.Length == 0));
    }
}
