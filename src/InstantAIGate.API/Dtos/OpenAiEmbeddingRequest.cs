namespace InstantAIGate.API.Dtos
{
    public record OpenAiEmbeddingRequest
    {
        public string Model { get; init; } = string.Empty; // RepoId will be provided here

        // OpenAI allows either a single string or an array of strings.
        // For universality and parsing simplicity we use object and will parse in the controller.
        public object Input { get; init; } = null!;
    }
}
