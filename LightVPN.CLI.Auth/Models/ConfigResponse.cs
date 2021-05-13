using System.Text.Json.Serialization;

namespace LightVPN.CLI.Auth.Models
{
    public class ConfigResponse : GenericResponse
    {
        [JsonPropertyName("bytes")]
        public string ConfigArchiveBase64 { get; set; }
    }
}