using System.Text.Json.Serialization;

namespace InstantAIGate.API.Dtos
{
    public class OpenAiChatRequest
    {
        #region Core Parameters

        /// <summary>
        /// The model identifier to use for generating the completion.
        /// Determines the capabilities, context window, and pricing of the request.
        /// </summary>
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        /// <summary>
        /// The list of messages comprising the conversation so far.
        /// Each message contains a role (system, user, assistant, tool) and content.
        /// This is the primary input that drives the model's response generation.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<OpenAiMessage>? Messages { get; set; }

        #endregion

        #region Sampling Parameters

        /// <summary>
        /// Controls the randomness of the model's output.
        /// Higher values (e.g., 1.0) make output more random and creative,
        /// while lower values (e.g., 0.2) make it more deterministic and focused.
        /// Affects the probability distribution used during token selection.
        /// </summary>
        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; }

        /// <summary>
        /// Nucleus sampling parameter that limits token selection to a cumulative probability mass.
        /// For example, 0.1 means only tokens comprising the top 10% probability mass are considered.
        /// Lower values make output more focused, higher values allow more diversity.
        /// Works in conjunction with temperature to control output variability.
        /// </summary>
        [JsonPropertyName("top_p")]
        public float? TopP { get; set; }

        /// <summary>
        /// The maximum number of tokens to generate in the response.
        /// Limits the length of the model's output to prevent excessive generation.
        /// The actual token count depends on the tokenizer used by the specific model.
        /// Note: For newer models, max_completion_tokens is preferred.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// The maximum number of completion tokens to generate (excluding prompt tokens).
        /// This is the modern replacement for max_tokens, especially for reasoning models (o-series).
        /// Provides more precise control over output length for advanced models.
        /// Takes precedence over max_tokens if both are specified.
        /// </summary>
        [JsonPropertyName("max_completion_tokens")]
        public int? MaxCompletionTokens { get; set; }

        /// <summary>
        /// Number of completion choices to generate for each request.
        /// Allows generating multiple alternative responses in a single API call.
        /// Useful for A/B testing different outputs or selecting the best response.
        /// Increases API usage proportionally to the number of choices requested.
        /// </summary>
        [JsonPropertyName("n")]
        public int? N { get; set; }

        /// <summary>
        /// Sequences where the model will stop generating further tokens.
        /// The response will not contain the stop sequence(s).
        /// Useful for controlling output format or terminating at specific markers.
        /// Can specify up to 4 stop sequences.
        /// </summary>
        [JsonPropertyName("stop")]
        public List<string>? Stop { get; set; }

        /// <summary>
        /// Penalizes new tokens based on whether they appear in the text so far.
        /// Positive values encourage the model to talk about new topics.
        /// Range: -2.0 to 2.0. Higher values increase topic diversity.
        /// Helps prevent the model from repeating the same themes or phrases.
        /// </summary>
        [JsonPropertyName("presence_penalty")]
        public float? PresencePenalty { get; set; }

        /// <summary>
        /// Penalizes new tokens based on their existing frequency in the text so far.
        /// Positive values decrease the likelihood of repeating the same lines verbatim.
        /// Range: -2.0 to 2.0. Higher values reduce repetition of common phrases.
        /// More effective than presence_penalty for reducing exact repetitions.
        /// </summary>
        [JsonPropertyName("frequency_penalty")]
        public float? FrequencyPenalty { get; set; }

        /// <summary>
        /// Modifies the likelihood of specified tokens appearing in the completion.
        /// Maps token IDs (as strings) to bias values from -100 to 100.
        /// Positive values increase likelihood, negative values decrease it.
        /// Useful for forcing or preventing specific words, phrases, or formatting.
        /// </summary>
        [JsonPropertyName("logit_bias")]
        public Dictionary<string, int>? LogitBias { get; set; }

        /// <summary>
        /// If specified, the model will attempt to generate deterministic output.
        /// Using the same seed and parameters should produce the same result.
        /// Useful for debugging, testing, and reproducible experiments.
        /// Note: Determinism is not guaranteed in all cases due to system optimizations.
        /// </summary>
        [JsonPropertyName("seed")]
        public int? Seed { get; set; }

        #endregion

        #region Log Probabilities

        /// <summary>
        /// Whether to return log probabilities of the output tokens.
        /// If true, returns the log probability of each token in the response.
        /// Useful for analyzing model confidence and understanding token selection.
        /// Adds computational overhead and increases response size.
        /// </summary>
        [JsonPropertyName("logprobs")]
        public bool? Logprobs { get; set; }

        /// <summary>
        /// Number of most likely tokens to return at each position, along with their log probabilities.
        /// Only applicable when logprobs is true. Range: 0 to 20.
        /// Provides insight into alternative token choices the model considered.
        /// Useful for understanding model uncertainty and exploring different generation paths.
        /// </summary>
        [JsonPropertyName("top_logprobs")]
        public int? TopLogprobs { get; set; }

        #endregion

        #region Streaming

        /// <summary>
        /// If true, the model will stream output tokens as they are generated.
        /// Uses Server-Sent Events (SSE) format for incremental delivery.
        /// Reduces time-to-first-token and improves perceived responsiveness.
        /// Essential for real-time chat applications and progressive rendering.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool? Stream { get; set; } = false;

        /// <summary>
        /// Options for configuring streaming behavior.
        /// Can include settings like include_usage to add token usage statistics to the stream.
        /// Allows fine-grained control over what data is included in streaming responses.
        /// Only applicable when stream is set to true.
        /// </summary>
        [JsonPropertyName("stream_options")]
        public object? StreamOptions { get; set; }

        #endregion

        #region Tools and Function Calling

        /// <summary>
        /// List of tools (functions) available for the model to call.
        /// Each tool defines a function name, description, and parameter schema.
        /// Enables the model to interact with external systems and APIs.
        /// The model can choose to call one or more tools based on the conversation context.
        /// </summary>
        [JsonPropertyName("tools")]
        public List<object>? Tools { get; set; }

        /// <summary>
        /// Controls which tool the model should call.
        /// Can be "none" (no tool calls), "auto" (model decides), "required" (must call a tool),
        /// or a specific tool name to force that particular tool.
        /// Provides explicit control over tool invocation behavior.
        /// </summary>
        [JsonPropertyName("tool_choice")]
        public object? ToolChoice { get; set; }

        /// <summary>
        /// Whether to enable parallel function calling during tool use.
        /// If true, the model can call multiple tools in a single response.
        /// If false, the model will call at most one tool per response.
        /// Useful for optimizing workflows that require multiple independent tool calls.
        /// </summary>
        [JsonPropertyName("parallel_tool_calls")]
        public bool? ParallelToolCalls { get; set; }

        #endregion

        #region Response Format

        /// <summary>
        /// Specifies the format the model must output.
        /// Can be {"type": "text"} for default behavior or {"type": "json_object"} for JSON mode.
        /// JSON mode ensures the output is valid JSON, useful for structured data extraction.
        /// Requires the model to be fine-tuned or prompted appropriately for reliable JSON output.
        /// </summary>
        [JsonPropertyName("response_format")]
        public object? ResponseFormat { get; set; }

        #endregion

        #region Multimodal and Advanced Features

        /// <summary>
        /// Specifies the modalities the model should output.
        /// Can include "text" and/or "audio" for multimodal responses.
        /// Enables generation of both text and audio content in a single request.
        /// Requires a model that supports the specified modalities.
        /// </summary>
        [JsonPropertyName("modalities")]
        public List<string>? Modalities { get; set; }

        /// <summary>
        /// Configuration for audio output when audio modality is enabled.
        /// Can specify voice selection, audio format, and other audio-specific parameters.
        /// Only applicable when "audio" is included in the modalities list.
        /// Controls the characteristics of generated audio responses.
        /// </summary>
        [JsonPropertyName("audio")]
        public object? Audio { get; set; }

        /// <summary>
        /// Controls the reasoning effort for reasoning models (o-series).
        /// Can be "low", "medium", or "high" to adjust the depth of analysis.
        /// Higher values increase response quality but also increase latency and token usage.
        /// Allows balancing between speed and thoroughness for complex tasks.
        /// </summary>
        [JsonPropertyName("reasoning_effort")]
        public string? ReasoningEffort { get; set; }

        /// <summary>
        /// Configuration for web search integration.
        /// Enables the model to search the web for up-to-date information.
        /// Can specify search parameters and constraints.
        /// Useful for queries requiring current information beyond the model's training data.
        /// </summary>
        [JsonPropertyName("web_search_options")]
        public object? WebSearchOptions { get; set; }

        #endregion

        #region User and Tracking

        /// <summary>
        /// A unique identifier representing the end-user making the request.
        /// Helps OpenAI monitor and detect abuse, and enables usage tracking per user.
        /// Should be a hash or anonymized identifier to protect user privacy.
        /// Recommended for all production applications to enable better support and monitoring.
        /// </summary>
        [JsonPropertyName("user")]
        public string? User { get; set; }

        /// <summary>
        /// A unique identifier for safety and abuse monitoring purposes.
        /// Used to track and prevent misuse of the API.
        /// Helps maintain service quality and compliance with usage policies.
        /// Optional but recommended for enterprise applications.
        /// </summary>
        [JsonPropertyName("safety_identifier")]
        public string? SafetyIdentifier { get; set; }

        /// <summary>
        /// A unique key used for prompt caching to improve performance and reduce costs.
        /// Identical prompts with the same cache key can be served from cache.
        /// Significantly reduces latency for repeated or similar requests.
        /// Useful for applications with high request volumes and repetitive patterns.
        /// </summary>
        [JsonPropertyName("prompt_cache_key")]
        public string? PromptCacheKey { get; set; }

        #endregion

        #region Storage and Metadata

        /// <summary>
        /// Whether to store the request and response for model distillation or evaluation.
        /// If true, the data may be used to improve future model versions.
        /// Useful for organizations participating in model improvement programs.
        /// Should be set based on data privacy and retention policies.
        /// </summary>
        [JsonPropertyName("store")]
        public bool? Store { get; set; }

        /// <summary>
        /// Custom key-value pairs for storing metadata about the request.
        /// Useful for tracking, analytics, and debugging purposes.
        /// Can include information like request source, user session, or experiment ID.
        /// Does not affect model behavior but helps with request management.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        #endregion

        #region Service Tier

        /// <summary>
        /// Specifies the service tier for processing the request.
        /// Can be "auto" (default), "default", or "flex" for different processing priorities.
        /// Flex tier may offer lower costs with variable latency.
        /// Allows optimization of cost-performance tradeoffs based on application requirements.
        /// </summary>
        [JsonPropertyName("service_tier")]
        public string? ServiceTier { get; set; }

        #endregion
    }
}