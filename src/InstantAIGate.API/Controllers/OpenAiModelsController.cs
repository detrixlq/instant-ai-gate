using InstantAIGate.API.Dtos;
using InstantAIGate.Application.Interfaces.Inference;
using Microsoft.AspNetCore.Mvc;

namespace InstantAiGate.Api.Controllers
{
    [ApiController]
    [Route("v1/models")] // Strict OpenAI API specification compliance
    public class OpenAiModelsController(IModelManager modelManager) : ControllerBase
    {
        private readonly IModelManager _modelManager = modelManager;

        /// <summary>
        /// OpenAI-compatible endpoint that lists all models currently active in memory and ready for immediate inference.
        /// </summary>
        /// <returns>A standardized OpenAI model list response envelope containing active RepoIds.</returns>
        [HttpGet] // URL: v1/models
        public IActionResult GetOpenAiModels()
        {
            try
            {
                // 1. Retrieve the strict collection of memory-allocated active RepoIds from the manager
                var activeRepoIds = _modelManager.GetActiveModels();

                // 2. Safe check against a null return allocation from the infrastructure layer
                if (activeRepoIds == null)
                {
                    return Ok(new OpenAiModelListResponse { data = new List<OpenAiModelInfo>() });
                }

                // 3. Map memory-active models directly into the OpenAI presentation format
                var availableModels = activeRepoIds
                    .Where(repoId => !string.IsNullOrWhiteSpace(repoId))
                    .Select(repoId => new OpenAiModelInfo(id: repoId))
                    .ToList();

                // 4. Wrap inside the traditional OpenAI list wrapper object
                var response = new OpenAiModelListResponse
                {
                    data = availableModels
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "ModelListRetrievalError", message = ex.Message });
            }
        }
    }
}