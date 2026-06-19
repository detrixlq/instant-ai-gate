

using InstantAIGate.Application.Dtos.Requests;
using InstantAIGate.Application.Interfaces.Templates;

namespace InstantAIGate.Infrastructure.Templates
{
    public class RawPromptTemplate : IPromptTemplateService
    {
        public string BuildPrompt(IEnumerable<ChatMessage> messages)
        {
            // Simply join messages with a newline
            return string.Join("\n", messages.Select(m => $"{m.Role}: {m.Content}"));
        }
    }
}
