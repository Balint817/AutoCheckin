using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace AutoCheckin.Objects
{
    public abstract class AbstractToken
    {
        [JsonIgnore]
        public abstract string Token { get; set; }
        [JsonIgnore]
        public abstract string Mid { get; set; }
        [JsonIgnore]
        public abstract string ID { get; set; }
        public void EnsureNotNull()
        {
            Token ??= "";
            Mid ??= "";
            ID ??= "";
            Token = Token.Trim();
            Mid = Mid.Trim();
            ID = ID.Trim();
        }
        [JsonIgnore]
        public bool IsValid => !string.IsNullOrEmpty(Token)
                                && !string.IsNullOrEmpty(Mid)
                                && !string.IsNullOrEmpty(ID);
        [JsonIgnore]
        private Dictionary<string, dynamic>? _propCache = null;
        public string MakeCookie()
        {
            if (_propCache == null)
            {
                _propCache = new();
                var props = GetType().GetProperties();
                foreach (var prop in props)
                {
                    var getMethod = prop.GetMethod;
                    if (getMethod is null)
                    {
                        continue;
                    }
                    var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                    if (attr is null)
                    {
                        continue;
                    }
                    _propCache[attr.Name] = getMethod.ToDynamicDelegate(this);
                }
            }
            var sb = new StringBuilder();
            foreach (var kv in _propCache)
            {
                sb.Append($"{kv.Key}={kv.Value()}; ");
            }
            return sb.ToString();
        }

        public void Reset()
        {
            Token = "";
            Mid = "";
            ID = "";
        }
    }
}
