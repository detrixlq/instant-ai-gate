using InstantAIGate.Application.Interfaces.Catalog;
using InstantAIGate.Application.Interfaces.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text.Json;

namespace InstantAIGate.API.Controllers
{
    [ApiController]
    [Route("api/admin/fetch")]
    [Authorize(Policy = "AdminApiKeyPolicy")]
    public class AdminFetchController : ControllerBase
    {
        private readonly IModelRegistry _modelRegistry;
        private readonly IModelStorageService _storageService;
        private readonly ILogger<AdminFetchController> _logger;

        // Shared cross-thread synchronization maps for detached background jobs
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _fetchCancellations = new();
        private static readonly ConcurrentDictionary<string, double> _liveProgressRegistry = new();
        private static readonly ConcurrentDictionary<string, string> _currentRunningFiles = new();

        public AdminFetchController(IModelRegistry modelRegistry, 
            IModelStorageService storageService,
            ILogger<AdminFetchController> logger)
        {
            _modelRegistry = modelRegistry;
            _storageService = storageService;
            _logger = logger;
        }

        /// <summary>
        /// Spawns an isolated non-blocking asynchronous worker task to download model binary artifacts.
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartFetch([FromQuery] string repoId)
        {
            if (string.IsNullOrWhiteSpace(repoId)) return BadRequest("Parameter 'repoId' is required.");

            var model = await _modelRegistry.GetModelAsync(repoId);
            if (model == null) return NotFound($"Model '{repoId}' not found inside catalog.");

            if (_fetchCancellations.ContainsKey(repoId))
            {
                return Conflict("Fetch operation already running for this target reference block.");
            }

            var cts = new CancellationTokenSource();
            _fetchCancellations[repoId] = cts;
            _liveProgressRegistry[repoId] = 0.0;

            // Detach pipeline thread to maintain execution lifespan across frontend tab navigation actions
            _ = Task.Run(async () =>
            {
                try
                {
                    foreach (var file in model.Files)
                    {
                        if (cts.Token.IsCancellationRequested) break;
                        _currentRunningFiles[repoId] = file.FileName;

                        await foreach (var progress in _storageService.DownloadModelFileAsync(
                            file.Url, model.RepoId, file.FileName, file.SizeBytes, cts.Token))
                        {
                            double percentage = (double)progress.BytesDownloaded / progress.TotalBytes * 100.0;
                            _liveProgressRegistry[repoId] = percentage;
                        }
                    }
                }
                catch (Exception)
                {
                    // Catch underlying file network disruptions safely here
                }
                finally
                {
                    _fetchCancellations.TryRemove(repoId, out _);
                    _liveProgressRegistry.TryRemove(repoId, out _);
                    _currentRunningFiles.TryRemove(repoId, out _);
                }
            });

            return Accepted(new { status = "Model pipeline fetch operation launched in background." });
        }

        /// <summary>
        /// Signal termination handle allowing instant cancellation of running network download streams.
        /// </summary>
        [HttpPost("cancel")]
        public IActionResult CancelFetch([FromQuery] string repoId)
        {
            if (_fetchCancellations.TryRemove(repoId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();

                _liveProgressRegistry.TryRemove(repoId, out _);
                _currentRunningFiles.TryRemove(repoId, out _);

                return Ok(new { message = "Background target acquisition pipeline dropped." });
            }
            return NotFound("No active structural fetch process detected for this model.");
        }

        /// <summary>
        /// Long-lived continuous network feed reporting storage layout progress streams.
        /// </summary>
        [HttpGet("progress-stream")]
        public async Task StreamProgress(CancellationToken clientCt)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            try
            {
                while (!clientCt.IsCancellationRequested)
                {
                    var snapshot = _liveProgressRegistry.Select(kvp => new
                    {
                        repoId = kvp.Key,
                        progress = kvp.Value,
                        currentFile = _currentRunningFiles.TryGetValue(kvp.Key, out var f) ? f : string.Empty
                    }).ToList();

                    var payload = JsonSerializer.Serialize(snapshot);

                    // The following line will throw an exception when the connection is broken
                    await Response.WriteAsync($"data: {payload}\n\n", clientCt);
                    await Response.Body.FlushAsync(clientCt);

                    await Task.Delay(1000, clientCt);
                }
            }
            catch (OperationCanceledException)
            {
                // This exception is expected.
                // It occurs whenever the user navigates away from the page or reloads it. No action required.
            }
            catch (Exception ex)
            {
                // Real errors (for example, network failures) can be logged here.
                _logger.LogError(ex, "SSE stream error occurred.");
            }
        }
    }
}