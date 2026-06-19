using System.Text.Json.Serialization;

namespace InstantAIGate.API.Dtos
{
    public class OpenAiChunkChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("delta")]
        public OpenAiMessage Delta { get; set; } = new();

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }
}
