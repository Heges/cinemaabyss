using System.Text.Json.Serialization;

namespace events.Models.API
{
    public sealed class UserRequest
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTimeOffset? Timestamp { get; set; }
    }
}
