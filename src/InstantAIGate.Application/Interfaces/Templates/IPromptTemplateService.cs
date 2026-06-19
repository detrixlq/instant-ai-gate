using InstantAIGate.Application.Dtos.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace InstantAIGate.Application.Interfaces.Templates
{
    public interface IPromptTemplateService
    {
        /// <summary>
        /// Converts a list of messages into a prompt string according to the model's format.
        /// </summary>
        string BuildPrompt(IEnumerable<ChatMessage> messages);
    }
}
