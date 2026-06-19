using InstantAIGate.API.Dtos;
using InstantAIGate.Application.Interfaces.Inference;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace InstantAiGate.Api.Controllers
{
    [ApiController]
    [Route("v1/embeddings")] // Strict OpenAI API specification compliance
    public class OpenAiEmbeddingsController(IEmbeddingAdapter embeddingAdapter) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateEmbedding([FromBody] OpenAiEmbeddingRequest request, CancellationToken ct)
        {
            if (request == null || request.Input == null || string.IsNullOrWhiteSpace(request.Model))
            {
                return BadRequest(new { error = "Invalid request. Both 'input' and 'model' parameters are required." });
            }

            try
            {
                // 1. Normalize the incoming flexible input (can be a single string or an array of strings)
                var inputs = NormalizeInput(request.Input);

                if (inputs.Count == 0)
                {
                    return BadRequest(new { error = "Input text payload cannot be empty." });
                }

                // 2. Delegate the block array of strings to the structural adapter.
                // The new adapter internally applies Mean Pooling and returns a clean, flat IReadOnlyList<float[]>.
                IReadOnlyList<float[]> sentenceVectors = await embeddingAdapter.GetEmbeddingAsync(request.Model, inputs, ct);

                // 3. Directly map the computed sentence vectors into the OpenAI-compliant layout
                var embeddingDataList = new List<OpenAiEmbeddingData>();

                for (int i = 0; i < sentenceVectors.Count; i++)
                {
                    // Since the adapter handles pooling internally, sentenceVectors[i] is already the direct float[] array.
                    var finalVector = sentenceVectors[i];

                    embeddingDataList.Add(new OpenAiEmbeddingData(
                        Embedding: finalVector ?? Array.Empty<float>(), // Fallback to an empty vector if null safely
                        Index: i
                    ));
                }

                // 4. Build the standardized OpenAI-compatible response envelope
                var response = new OpenAiEmbeddingResponse
                {
                    model = request.Model,
                    data = embeddingDataList,
                    usage = new OpenAiUsage
                    {
                        PromptTokens = 0, // Telemetry metadata to be enhanced later
                        TotalTokens = 0,
                    }
                };

                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = "ModelNotRegistered", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = "ModelExecutionError", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "EmbeddingGenerationError", message = ex.Message });
            }
        }

        /// <summary>
        /// Normalizes the incoming flexible JSON element into a uniform string array layout.
        /// Under the OpenAI specification, the 'input' property can arrive as a string ("text") or an array of strings (["text1", "text2"]).
        /// </summary>
        private List<string> NormalizeInput(object input)
        {
            var list = new List<string>();

            if (input is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in jsonElement.EnumerateArray())
                    {
                        var str = item.GetString();
                        if (str != null) list.Add(str);
                    }
                }
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    var str = jsonElement.GetString();
                    if (str != null) list.Add(str);
                }
            }
            else if (input is string singleString)
            {
                list.Add(singleString);
            }
            else if (input is string[] arrayStrings)
            {
                list.AddRange(arrayStrings);
            }

            return list;
        }
    }
}