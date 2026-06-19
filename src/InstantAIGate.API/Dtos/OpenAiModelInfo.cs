namespace InstantAIGate.API.Dtos
{
    /// <summary>
    /// DTO representing the OpenAI-compatible model info returned by the /v1/models endpoint.
    /// </summary>
    public record OpenAiModelInfo(
        string id, // The logical RepoId (for example, "Qwen/Qwen3-VL-2B-Instruct-GGUF")
        string @object = "model",
        long created = 1710000000,
        string owned_by = "instant-ai-gate"
    );
}
