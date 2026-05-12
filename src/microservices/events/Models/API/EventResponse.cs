using events.Models;
using System.Text.Json.Serialization;

namespace events.Models.API
{
    public sealed class EventResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "success";

        [JsonPropertyName("queued")]
        public bool Queued { get; set; }

        [JsonPropertyName("partition")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Partition { get; set; }

        [JsonPropertyName("offset")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? Offset { get; set; }

        [JsonPropertyName("event")]
        public Event Event { get; set; } = new();
    }
}
