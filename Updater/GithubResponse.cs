using System.Text.Json.Serialization;

namespace Updater
{
    class GithubResponse
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [JsonPropertyName("name")]
        public string TagName { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        // ignore the rest of the response as we don't need it
    }
}
