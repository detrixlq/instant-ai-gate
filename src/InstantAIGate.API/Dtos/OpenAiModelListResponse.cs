namespace InstantAIGate.API.Dtos
{
    public record OpenAiModelListResponse(
            string @object = "list",
            List<OpenAiModelInfo> data = null!
        );
}
