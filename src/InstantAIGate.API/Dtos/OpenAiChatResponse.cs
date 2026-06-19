using System.Text.Json.Serialization;

namespace InstantAIGate.API.Dtos
{
    /// <summary>
    /// Full response in OpenAI format
    /// </summary>
    public class OpenAiChatResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("object")]
        public string Object { get; set; } = "chat.completion";

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("choices")]
        public List<OpenAiChoice> Choices { get; set; } = new();

        [JsonPropertyName("usage")]
        public OpenAiUsage? Usage { get; set; }
    }
}
