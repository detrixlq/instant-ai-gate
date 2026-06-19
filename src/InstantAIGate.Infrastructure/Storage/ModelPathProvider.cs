using InstantAIGate.Application.Interfaces.Catalog;
using InstantAIGate.Application.Interfaces.Storage;
using InstantAIGate.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InstantAIGate.Infrastructure.Storage
{
    /// <summary>
    /// Provides filesystem abstraction layers for mapping logical AI repository identifiers 
    /// into physical directory grids and explicit file targets.
    /// </summary>
    public class ModelPathProvider : IModelPathProvider
    {
        private readonly string _rootPath;
        private readonly IModelRegistry _registry;
        private readonly ILogger<ModelPathProvider> _logger;

        public ModelPathProvider(
            IOptions<StorageOptions> options,
            IModelRegistry registry,
            ILogger<ModelPathProvider> logger)
        {
            _registry = registry;
            _logger = logger;
            _rootPath = GetInitializedRootPath(options.Value.RootPath);
        }

        private static string GetInitializedRootPath(string rootPath)
        {
            var path = Path.IsPathRooted(rootPath)
                ? rootPath
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rootPath));

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        public string GetModelDirectory(string repoId)
        {
            var folderName = repoId.Replace("/", "_").Replace("\\", "_");
            return Path.Combine(_rootPath, folderName);
        }

        public string GetModelFilePath(string repoId, string fileName)
        {
            return Path.Combine(GetModelDirectory(repoId), fileName).Replace("\\", "/");
        }

        /// <summary>
        /// Resolves the absolute physical path to the primary execution entry point of a model repository.
        /// Seamlessly supports both single-file GGUFs and multi-part sharded splits.
        /// </summary>
        public Task<string> GetFullModelPathAsync(string repoId)
        {
            if (string.IsNullOrWhiteSpace(repoId))
            {
                throw new ArgumentException("Repository identifier cannot be null or empty.", nameof(repoId));
            }

            string sanitizedFolderName = repoId.Replace('/', '_').Replace('\\', '_');
            string targetFolder = Path.Combine(_rootPath, sanitizedFolderName);

            if (!Directory.Exists(targetFolder))
            {
                throw new DirectoryNotFoundException($"The local storage directory for model '{repoId}' was not found at path: {targetFolder}");
            }

            var ggufFiles = Directory.GetFiles(targetFolder, "*.gguf", SearchOption.TopDirectoryOnly);

            if (ggufFiles.Length == 0)
            {
                throw new FileNotFoundException($"No valid model binaries (*.gguf) were found inside the directory for repository: '{repoId}'.");
            }

            // Alphabetical ordering ensures that -00001-of-XXXXX.gguf is always selected as primary descriptor entry
            string primaryModelPath = ggufFiles.OrderBy(f => f).First();

            _logger.LogInformation("Resolved primary execution binary path for model '{RepoId}': {Path} (Total pieces in folder: {Count})",
                repoId, primaryModelPath, ggufFiles.Length);

            return Task.FromResult(primaryModelPath);
        }

        /// <summary>
        /// Reverses a physical filesystem path back into its matching domain catalog record.
        /// Successfully identifies the root model even if the input targets an intermediate split shard file.
        /// </summary>
        public async Task<ModelManifest?> GetModelFromPathAsync(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return null;

            try
            {
                // 1. Normalize the incoming path format and extract just the raw file name (e.g., "Qwen3VL-Q4_K_M-00002-of-00002.gguf")
                var targetFileName = Path.GetFileName(fullPath);
                var normalizedInputPath = Path.GetFullPath(fullPath);

                // 2. Load the entire schema registry to evaluate bounds
                var models = await _registry.GetAllModelsAsync();

                foreach (var model in models)
                {
                    if (model.Files == null) continue;

                    // 3. Scan the collection of nested file names inside this model mapping profile
                    var containsFile = model.Files.Any(f =>
                        string.Equals(f.FileName, targetFileName, StringComparison.OrdinalIgnoreCase));

                    if (containsFile)
                    {
                        // 4. Double-check destination correctness by verifying the directory belongs to this RepoId
                        var expectedDirectory = Path.GetFullPath(GetModelDirectory(model.RepoId));
                        var inputDirectory = Path.GetFullPath(Path.GetDirectoryName(normalizedInputPath) ?? string.Empty);

                        if (string.Equals(expectedDirectory, inputDirectory, StringComparison.OrdinalIgnoreCase))
                        {
                            return model;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve a registered domain model out of the raw path sequence: {Path}", fullPath);
            }

            _logger.LogWarning("Reverse path resolution failed. No registered model metadata claims ownership for path: {Path}", fullPath);
            return null;
        }
    }
}