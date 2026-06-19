namespace InstantAIGate.API.Dtos
{
    public static class OpenAiResponseBuilder
    {
        private static readonly DateTimeOffset Epoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static string GenerateId() =>
            "chatcmpl-" + Guid.NewGuid().ToString("N")[..8];

        public static long GetCurrentTimestamp() =>
            DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static OpenAiChatResponse BuildFullResponse(
            string content,
            string model,
            string? finishReason = "stop")
        {
            return new OpenAiChatResponse
            {
                Id = GenerateId(),
                Object = "chat.completion",
                Created = GetCurrentTimestamp(),
                Model = model,
                Choices = new List<OpenAiChoice>
            {
                new()
                {
                    Index = 0,
                    Message = new OpenAiMessage { Role = "assistant", Content = content },
                    FinishReason = finishReason
                }
            },
                Usage = new OpenAiUsage
                {
                    PromptTokens = 0,
                    CompletionTokens = 0,
                    TotalTokens = 0
                }
            };
        }

        public static OpenAiChatChunk BuildChunk(
            string content,
            string model,
            string? finishReason = null)
        {
            return new OpenAiChatChunk
            {
                Id = GenerateId(),
                Object = "chat.completion.chunk",
                Created = GetCurrentTimestamp(),
                Model = model,
                Choices = new List<OpenAiChunkChoice>
            {
                new()
                {
                    Index = 0,
                    Delta = new OpenAiMessage { Role = "assistant", Content = content },
                    FinishReason = finishReason
                }
            }
            };
        }
    }
}
