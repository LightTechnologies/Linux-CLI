using System;
using System.Text.Json.Serialization;

namespace LightVPN.CLI.Auth.Models
{
    public class SessionFile
    {
        [JsonPropertyName("sessionId")]
        public Guid SessionId { get; set; }
    }
}