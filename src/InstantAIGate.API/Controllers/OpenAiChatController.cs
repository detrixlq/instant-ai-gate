using InstantAIGate.API.Dtos;
using InstantAIGate.Application.Dtos.Requests;
using InstantAIGate.Application.Interfaces.Inference;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace InstantAIGate.API.Controllers
{
    [ApiController]
    [Route("v1/chat")]
    public class OpenAiChatController(IChatAdapter chatAdapter) : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        [HttpPost("completions")]
        public async Task<IActionResult> CreateChatCompletion([FromBody] OpenAiChatRequest? request)
        {
            // Validation
            if (request == null || string.IsNullOrWhiteSpace(request.Model))
            {
                return BadRequest(new
                {
                    error = new
                    {
                        message = "The 'model' parameter is required and cannot be null.",
                        type = "invalid_request_error",
                        code = "missing_required_parameter"
                    }
                });
            }

            if (request.Messages == null || request.Messages.Count == 0)
            {
                return BadRequest(new
                {
                    error = new
                    {
                        message = "The 'messages' array is required and must contain at least one message object.",
                        type = "invalid_request_error",
                        code = "missing_required_parameter"
                    }
                });
            }

            if (request.Temperature.HasValue && (request.Temperature < 0 || request.Temperature > 2))
            {
                return BadRequest(new
                {
                    error = new
                    {
                        message = "Temperature must be between 0 and 2.",
                        type = "invalid_request_error",
                        code = "invalid_value"
                    }
                });
            }

            if (request.TopP.HasValue && (request.TopP < 0 || request.TopP > 1))
            {
                return BadRequest(new
                {
                    error = new
                    {
                        message = "Top_p must be between 0 and 1.",
                        type = "invalid_request_error",
                        code = "invalid_value"
                    }
                });
            }

            var targetModel = request.Model;
            var chatRequest = MapToLlamaChatRequest(request);
            var chatId = $"chatcmpl-{Guid.NewGuid():N}"[..20];
            var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // STREAMING MODE
            if (request.Stream == true)
            {
                Response.ContentType = "text/event-stream";
                Response.Headers.Append("Cache-Control", "no-cache");
                Response.Headers.Append("Connection", "keep-alive");
                Response.Headers.Append("X-Accel-Buffering", "no");

                try
                {
                    await foreach (var token in chatAdapter.StreamAsync(chatRequest, HttpContext.RequestAborted))
                    {
                        if (string.IsNullOrWhiteSpace(token)) continue;
                        await WriteSseAsync(BuildChunk(token, targetModel, chatId, created));
                    }

                    await WriteSseAsync(BuildChunk(string.Empty, targetModel, chatId, created, finishReason: "stop"));
                    await Response.WriteAsync("data: [DONE]\n\n", HttpContext.RequestAborted);
                    await Response.Body.FlushAsync(HttpContext.RequestAborted);
                }
                catch (OperationCanceledException)
                {
                    // Client aborted
                }
                catch (InvalidOperationException ex)
                {
                    await WriteSseAsync(BuildChunk(string.Empty, targetModel, chatId, created, finishReason: ex.Message));
                }
                catch (Exception)
                {
                    await WriteSseAsync(BuildChunk(string.Empty, targetModel, chatId, created, finishReason: "internal_server_error"));
                }

                return new EmptyResult();
            }

            // NON-STREAMING MODE
            try
            {
                var content = await chatAdapter.GenerateAsync(chatRequest, HttpContext.RequestAborted);
                return Ok(BuildFullResponse(content, targetModel, chatId, created));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = new { message = ex.Message, type = "invalid_request_error" } });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, new { error = new { message = "Client closed request", type = "client_closed_request" } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { message = ex.Message, type = "server_error" } });
            }
        }

        #region Mapping Methods

        /// <summary>
        /// Maps a single OpenAI message to the internal ChatMessage format,
        /// handling both plain text and multimodal content (text, images, audio).
        /// </summary>
        private ChatMessage MapMessage(OpenAiMessage m)
        {
            var (textContent, contentParts) = MapContent(m.Content);

            return new ChatMessage
            {
                Role = m.Role?.ToLowerInvariant() ?? "user",
                Content = textContent,
                ContentParts = contentParts,
                Name = m.Name,
                ToolCallId = m.ToolCallId,
                ToolCalls = m.ToolCalls?.Select(tc => new ToolCall
                {
                    Id = tc.Id,
                    Type = tc.Type,
                    Function = tc.Function != null ? new FunctionCall
                    {
                        Name = tc.Function.Name,
                        Arguments = tc.Function.Arguments
                    } : null
                }).ToList()
            };
        }

        /// <summary>
        /// Processes the Content field which can be either a string or an array of content parts.
        /// Returns the extracted text content and the list of multimodal content parts.
        /// </summary>
        private (string textContent, List<ContentPart>? contentParts) MapContent(object? content)
        {
            if (content == null)
                return (string.Empty, null);

            // Case 1: Simple string content
            if (content is string textContent)
                return (textContent, null);

            // Case 2: JsonElement from deserialization (most common case)
            if (content is System.Text.Json.JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => (jsonElement.GetString() ?? string.Empty, null),
                    System.Text.Json.JsonValueKind.Array => MapContentArray(jsonElement),
                    _ => (content.ToString() ?? string.Empty, null)
                };
            }

            // Case 3: Already deserialized list
            if (content is List<OpenAiContentPart> parts)
            {
                return MapContentParts(parts);
            }

            return (content.ToString() ?? string.Empty, null);
        }

        /// <summary>
        /// Maps a JSON array of content parts to text and multimodal parts.
        /// </summary>
        private (string textContent, List<ContentPart>? contentParts) MapContentArray(System.Text.Json.JsonElement jsonArray)
        {
            var textParts = new List<string>();
            var contentParts = new List<ContentPart>();

            foreach (var item in jsonArray.EnumerateArray())
            {
                var type = item.TryGetProperty("type", out var typeProp)
                    ? typeProp.GetString() ?? "text"
                    : "text";

                switch (type)
                {
                    case "text":
                        if (item.TryGetProperty("text", out var textProp))
                        {
                            var text = textProp.GetString() ?? string.Empty;
                            textParts.Add(text);
                            contentParts.Add(new ContentPart { Type = "text", Text = text });
                        }
                        break;

                    case "image_url":
                        if (item.TryGetProperty("image_url", out var imgProp) && imgProp.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            var url = imgProp.TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? string.Empty : string.Empty;
                            var detail = imgProp.TryGetProperty("detail", out var detailProp) ? detailProp.GetString() : null;
                            contentParts.Add(new ContentPart
                            {
                                Type = "image_url",
                                ImageUrl = new ImageContent { Url = url, Detail = detail }
                            });
                        }
                        break;

                    case "input_audio":
                        if (item.TryGetProperty("input_audio", out var audioProp) && audioProp.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            var data = audioProp.TryGetProperty("data", out var dataProp) ? dataProp.GetString() ?? string.Empty : string.Empty;
                            var format = audioProp.TryGetProperty("format", out var formatProp) ? formatProp.GetString() ?? "wav" : "wav";
                            contentParts.Add(new ContentPart
                            {
                                Type = "input_audio",
                                InputAudio = new AudioContent { Data = data, Format = format }
                            });
                        }
                        break;
                }
            }

            var combinedText = string.Join("\n", textParts);
            return (combinedText, contentParts.Count > 0 ? contentParts : null);
        }

        /// <summary>
        /// Maps a list of OpenAiContentPart to internal format.
        /// </summary>
        private (string textContent, List<ContentPart>? contentParts) MapContentParts(List<OpenAiContentPart> parts)
        {
            var textParts = new List<string>();
            var contentParts = new List<ContentPart>();

            foreach (var part in parts)
            {
                switch (part.Type)
                {
                    case "text":
                        if (!string.IsNullOrEmpty(part.Text))
                        {
                            textParts.Add(part.Text);
                            contentParts.Add(new ContentPart { Type = "text", Text = part.Text });
                        }
                        break;

                    case "image_url":
                        if (part.ImageUrl != null)
                        {
                            contentParts.Add(new ContentPart
                            {
                                Type = "image_url",
                                ImageUrl = new ImageContent
                                {
                                    Url = part.ImageUrl.Url,
                                    Detail = part.ImageUrl.Detail
                                }
                            });
                        }
                        break;

                    case "input_audio":
                        if (part.InputAudio != null)
                        {
                            contentParts.Add(new ContentPart
                            {
                                Type = "input_audio",
                                InputAudio = new AudioContent
                                {
                                    Data = part.InputAudio.Data,
                                    Format = part.InputAudio.Format
                                }
                            });
                        }
                        break;
                }
            }

            var combinedText = string.Join("\n", textParts);
            return (combinedText, contentParts.Count > 0 ? contentParts : null);
        }

        private LlamaChatRequest MapToLlamaChatRequest(OpenAiChatRequest request)
        {
            var effectiveMaxTokens = request.MaxCompletionTokens ?? request.MaxTokens ?? 4096;

            return new LlamaChatRequest
            {
                Model = request.Model,
                Messages = (request.Messages ?? Enumerable.Empty<OpenAiMessage>())
                    .OfType<OpenAiMessage>()
                    .Select(m => MapMessage(m))
                    .Where(m => !string.IsNullOrEmpty(m.Content) || m.ContentParts?.Count > 0 || m.ToolCalls?.Count > 0)
                    .ToList(),

                Temperature = request.Temperature ?? 0.7f,
                TopP = request.TopP ?? 0.9f,
                TopK = 40,
                N = request.N ?? 1,
                MaxTokens = effectiveMaxTokens,
                Stop = request.Stop,
                Seed = request.Seed,

                PresencePenalty = request.PresencePenalty ?? 0.0f,
                FrequencyPenalty = request.FrequencyPenalty ?? 0.0f,
                RepeatPenalty = 1.1f,

                Logprobs = request.Logprobs ?? false,
                TopLogprobs = request.TopLogprobs ?? 0,
                LogitBias = request.LogitBias,

                Stream = request.Stream ?? false,
                StreamOptions = MapStreamOptions(request.StreamOptions),

                Tools = MapTools(request.Tools),
                ToolChoice = MapToolChoice(request.ToolChoice),
                ParallelToolCalls = request.ParallelToolCalls ?? true,

                ResponseFormat = MapResponseFormat(request.ResponseFormat),

                User = request.User,
                SafetyIdentifier = request.SafetyIdentifier,
                PromptCacheKey = request.PromptCacheKey,

                Store = request.Store ?? false,
                Metadata = request.Metadata,
                ServiceTier = request.ServiceTier
            };
        }

        private StreamOptions? MapStreamOptions(object? streamOptions)
        {
            if (streamOptions == null) return null;

            try
            {
                var jsonElement = JsonSerializer.SerializeToElement(streamOptions);
                return new StreamOptions
                {
                    IncludeUsage = jsonElement.TryGetProperty("include_usage", out var prop)
                        && prop.ValueKind == JsonValueKind.True
                };
            }
            catch
            {
                return null;
            }
        }

        private List<ToolDefinition>? MapTools(List<object>? tools)
        {
            if (tools == null || tools.Count == 0) return null;

            try
            {
                var json = JsonSerializer.Serialize(tools);
                return JsonSerializer.Deserialize<List<ToolDefinition>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private ToolChoiceOption? MapToolChoice(object? toolChoice)
        {
            if (toolChoice == null) return null;

            try
            {
                var jsonElement = JsonSerializer.SerializeToElement(toolChoice);

                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return new ToolChoiceOption { Type = jsonElement.GetString() ?? "auto" };
                }

                if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    var type = jsonElement.TryGetProperty("type", out var typeProp)
                        ? typeProp.GetString() ?? "auto"
                        : "auto";

                    ToolFunction? function = null;
                    if (jsonElement.TryGetProperty("function", out var funcProp) && funcProp.ValueKind == JsonValueKind.Object)
                    {
                        var name = funcProp.TryGetProperty("name", out var nameProp)
                            ? nameProp.GetString() ?? string.Empty
                            : string.Empty;
                        function = new ToolFunction { Name = name };
                    }

                    return new ToolChoiceOption { Type = type, Function = function };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private ResponseFormat? MapResponseFormat(object? responseFormat)
        {
            if (responseFormat == null) return null;

            try
            {
                var jsonElement = JsonSerializer.SerializeToElement(responseFormat);
                var type = jsonElement.TryGetProperty("type", out var typeProp)
                    ? typeProp.GetString() ?? "text"
                    : "text";

                var grammar = jsonElement.TryGetProperty("grammar", out var grammarProp)
                    ? grammarProp.GetString()
                    : null;

                return new ResponseFormat { Type = type, Grammar = grammar };
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region SSE Helpers

        private async Task WriteSseAsync(object data)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);
        }

        private static object BuildChunk(
            string content,
            string model,
            string chatId,
            long created,
            string? finishReason = null)
        {
            return new
            {
                id = chatId,
                @object = "chat.completion.chunk",
                created,
                model,
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        delta = string.IsNullOrEmpty(content)
                            ? (object)new { }
                            : new { content },
                        finish_reason = finishReason
                    }
                }
            };
        }

        private static object BuildFullResponse(
            string content,
            string model,
            string chatId,
            long created,
            string? finishReason = "stop")
        {
            return new
            {
                id = chatId,
                @object = "chat.completion",
                created,
                model,
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        message = new { role = "assistant", content },
                        finish_reason = finishReason
                    }
                },
                usage = new
                {
                    prompt_tokens = 0,
                    completion_tokens = 0,
                    total_tokens = 0
                }
            };
        }

        #endregion
    }
}