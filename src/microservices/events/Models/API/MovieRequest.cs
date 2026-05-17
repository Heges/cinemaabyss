using System.Text.Json.Serialization;

namespace events.Models.API
{
    public sealed class MovieRequest
    {
        [JsonPropertyName("movie_id")]
        public int MovieId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        [JsonPropertyName("rating")]
        public double? Rating { get; set; }

        [JsonPropertyName("genres")]
        public string[]? Genres { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
