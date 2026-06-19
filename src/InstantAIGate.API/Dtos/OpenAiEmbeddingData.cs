using System.Text.Json.Serialization;

namespace InstantAIGate.API.Dtos
{
    /// <summary>
    /// Represents a single embedding vector object within the OpenAI-compliant response payload.
    /// Uses explicit JSON annotations to maintain strict camelCase wire compatibility.
    /// </summary>
    public record OpenAiEmbeddingData(
        [property: JsonPropertyName("embedding")] IReadOnlyList<float> Embedding,
        [property: JsonPropertyName("index")] int Index = 0,
        [property: JsonPropertyName("object")] string Object = "embedding"
    );
}