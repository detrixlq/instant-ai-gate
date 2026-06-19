namespace InstantAIGate.API.Dtos
{
    public record OpenAiEmbeddingResponse
    {
        public string @object { get; init; } = "list";
        public List<OpenAiEmbeddingData> data { get; init; } = new();
        public string model { get; init; } = string.Empty;
        public OpenAiUsage usage { get; init; } = new(); // Token statistics
    }
}
