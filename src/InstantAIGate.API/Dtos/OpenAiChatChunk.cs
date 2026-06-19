using System.Text.Json.Serialization;

namespace InstantAIGate.API.Dtos
{
    // <summary>
    /// Chunk for streaming (OpenAI format)
    /// </summary>
    public class OpenAiChatChunk
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("object")]
        public string Object { get; set; } = "chat.completion.chunk";

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("choices")]
        public List<OpenAiChunkChoice> Choices { get; set; } = new();
    }

}
