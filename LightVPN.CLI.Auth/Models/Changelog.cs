using System.Text.Json.Serialization;

namespace LightVPN.CLI.Auth.Models
{
    public class Changelog
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("changelog")]
        public string Content { get; set; }
    }
}