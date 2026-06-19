using System.Text.Json.Serialization;

namespace InstantAIGate.API.Dtos
{
    public class OpenAiChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public OpenAiMessage Message { get; set; } = new();

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; } // "stop", "length", "content_filter"
    }
}
