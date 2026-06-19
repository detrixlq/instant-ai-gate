using InstantAIGate.Infrastructure.Inference.Drivers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InstantAIGate.Infrastructure.Services
{

    /// <summary>
    /// Hosted service that automatically loads native libraries at application startup.
    /// Resolves the best backend based on configuration and availability.
    /// </summary>
    public class NativeInitializationService : IHostedService
    {
        private readonly INativeBackendRegistry _backendRegistry;
        private readonly INativeLibraryLoader _libraryLoader;
        private readonly NativeLibraryOptions _options;
        private readonly ILogger<NativeInitializationService> _logger;

        public NativeInitializationService(
            INativeBackendRegistry backendRegistry,
            INativeLibraryLoader libraryLoader,
            IOptions<NativeLibraryOptions> options,
            ILogger<NativeInitializationService> logger)
        {
            _backendRegistry = backendRegistry;
            _libraryLoader = libraryLoader;
            _options = options.Value;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Initializing native library loader...");

                if (_options.EnableDebugLogging)
                {
                    _logger.LogDebug("Preferred backend: {Preferred}", _options.PreferredBackend);
                }

                // Resolve the best backend
                var backend = _backendRegistry.ResolveBackend(_options.PreferredBackend);

                // Load the backend
                _libraryLoader.LoadBackend(backend);

                _logger.LogInformation(
                    "Native library initialization completed successfully. Backend: {Backend}",
                    backend.Name);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize native library loader");
                return Task.CompletedTask;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Native library initialization service stopping");
            return Task.CompletedTask;
        }
    }
}
