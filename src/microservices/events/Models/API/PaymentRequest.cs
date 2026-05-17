using System.Text.Json.Serialization;

namespace events.Models.API
{
    public sealed class PaymentRequest
    {
        [JsonPropertyName("payment_id")]
        public int PaymentId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTimeOffset? Timestamp { get; set; }

        [JsonPropertyName("method_type")]
        public string? MethodType { get; set; }
    }
}
