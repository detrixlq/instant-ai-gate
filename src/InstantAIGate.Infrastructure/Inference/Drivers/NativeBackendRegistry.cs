
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;

namespace InstantAIGate.Infrastructure.Inference.Drivers
{


    /// <summary>
    /// Registry that discovers and manages available native backends.
    /// Automatically ensures native libraries are copied to the output directory before scanning.
    /// </summary>
    public class NativeBackendRegistry : INativeBackendRegistry
    {
        private readonly ILogger<NativeBackendRegistry> _logger;
        private readonly NativeLibraryOptions _options;
        private readonly object _lock = new();
        private List<NativeBackendInfo> _backends = new();
        private readonly string _currentRid;

        public NativeBackendRegistry(
            ILogger<NativeBackendRegistry> logger,
            IOptions<NativeLibraryOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _currentRid = GetRuntimeIdentifier();
            Refresh();
        }

        public IReadOnlyList<NativeBackendInfo> GetAllBackends()
        {
            lock (_lock) return _backends.AsReadOnly();
        }

        public IReadOnlyList<NativeBackendInfo> GetAvailableBackends()
        {
            lock (_lock) return _backends.Where(b => b.IsAvailable).ToList().AsReadOnly();
        }

        public NativeBackendInfo? GetBackend(string name)
        {
            lock (_lock)
            {
                return _backends.FirstOrDefault(b =>
                    b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public NativeBackendInfo ResolveBackend(string preferredBackend)
        {
            lock (_lock)
            {
                var available = _backends.Where(b => b.IsAvailable).ToList();

                if (available.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"No native backends found for '{_currentRid}'. " +
                        "Ensure the '.runtimes' folder exists in the solution root or application directory.");
                }

                if (preferredBackend.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    var gpu = available.FirstOrDefault(b => b.IsGpu);
                    if (gpu != null)
                    {
                        _logger.LogInformation("Auto-selected GPU backend: {Backend}", gpu.Name);
                        return gpu;
                    }

                    var cpu = available.FirstOrDefault(b => !b.IsGpu);
                    if (cpu != null)
                    {
                        _logger.LogInformation("Auto-selected CPU backend (no GPU available)");
                        return cpu;
                    }
                }

                var requested = available.FirstOrDefault(b =>
                    b.Name.Equals(preferredBackend, StringComparison.OrdinalIgnoreCase));

                if (requested == null)
                {
                    var availableNames = string.Join(", ", available.Select(b => b.Name));
                    throw new InvalidOperationException(
                        $"Requested backend '{preferredBackend}' is not available. " +
                        $"Available backends for {_currentRid}: {availableNames}");
                }

                _logger.LogInformation("Selected backend: {Backend}", requested.Name);
                return requested;
            }
        }

        public void Refresh()
        {
            lock (_lock)
            {
                _backends.Clear();

                // 1. Ensure files are copied from source to destination
                EnsureRuntimesCopied();

                var basePath = AppContext.BaseDirectory;
                var nativePath = Path.Combine(basePath, "runtimes", _currentRid);

                _logger.LogInformation("Scanning for native backends at: {Path}", nativePath);

                if (!Directory.Exists(nativePath))
                {
                    _logger.LogWarning("Native backends directory not found after copy attempt: {Path}", nativePath);
                    return;
                }

                foreach (var backendDir in Directory.GetDirectories(nativePath))
                {
                    var backendName = Path.GetFileName(backendDir);
                    var libraries = Directory.GetFiles(backendDir)
                        .Where(f => IsNativeLibrary(f))
                        .Select(Path.GetFileName)
                        .ToList();

                    var hasLlama = libraries.Any(l =>
                        string.Equals(l, "llama.dll", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(l, "libllama.so", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(l, "libllama.dylib", StringComparison.OrdinalIgnoreCase));

                    _backends.Add(new NativeBackendInfo
                    {
                        Rid = _currentRid,
                        Name = backendName,
                        Path = backendDir,
                        IsAvailable = hasLlama && libraries.Count > 0,
                        LibraryFiles = libraries!
                    });

                    _logger.LogDebug(
                        "Discovered backend: {Name}, Available: {Available}, Libraries: {Libraries}",
                        backendName, hasLlama, string.Join(", ", libraries));
                }

                _logger.LogInformation(
                    "Backend registry refreshed for {Rid}. Found {Total} backends, {Available} available",
                    _currentRid, _backends.Count, _backends.Count(b => b.IsAvailable));
            }
        }

        /// <summary>
        /// Copies the .runtimes folder from the source to the application output directory.
        /// Automatically searches parent directories to find the source ".runtimes" folder.
        /// </summary>
        private void EnsureRuntimesCopied()
        {
            var destRuntimesPath = Path.Combine(AppContext.BaseDirectory, "runtimes");
            var destRidPath = Path.Combine(destRuntimesPath, _currentRid);

            // Check if there are already files in destination
            if (Directory.Exists(destRidPath) && Directory.GetFiles(destRidPath, "*.so", SearchOption.AllDirectories).Length > 0)
            {
                if (_options.EnableDebugLogging)
                    _logger.LogDebug("Native runtimes already present at {Path}. Skipping copy.", destRidPath);
                return; // ← The files are already there, no need to copy
            }

            // Strategy: Search upwards from the base directory to find the ".runtimes" folder.
            string? sourceRuntimesPath = null;
            var currentDir = AppContext.BaseDirectory;

            while (currentDir != null)
            {
                var potentialPath = Path.Combine(currentDir, ".runtimes");
                if (Directory.Exists(potentialPath))
                {
                    sourceRuntimesPath = potentialPath;
                    break;
                }
                currentDir = Directory.GetParent(currentDir)?.FullName;
            }

            if (string.IsNullOrEmpty(sourceRuntimesPath))
            {
                if (_options.EnableDebugLogging)
                    _logger.LogDebug("Source '.runtimes' folder not found in any parent directory. Skipping copy.");
                return;
            }

            var sourceRidPath = Path.Combine(sourceRuntimesPath, _currentRid);

            if (!Directory.Exists(sourceRidPath))
            {
                if (_options.EnableDebugLogging)
                    _logger.LogDebug("Source folder for {Rid} not found at {Path}. Skipping copy.", _currentRid, sourceRidPath);
                return;
            }

            _logger.LogInformation("Copying native runtimes from {Source} to {Dest}", sourceRidPath, destRidPath);
            CopyDirectory(sourceRidPath, destRidPath);
            _logger.LogInformation("Native runtimes copied successfully.");
        }

        /// <summary>
        /// Recursively copies a directory, overwriting only if the source file is newer.
        /// </summary>
        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                if (!File.Exists(destFile) || File.GetLastWriteTime(file) > File.GetLastWriteTime(destFile))
                {
                    File.Copy(file, destFile, overwrite: true);
                }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
            }
        }

        private static string GetRuntimeIdentifier()
        {
            var arch = RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new PlatformNotSupportedException($"Unsupported architecture: {RuntimeInformation.OSArchitecture}")
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return $"win-{arch}";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return $"linux-{arch}";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return $"osx-{arch}";

            throw new PlatformNotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");
        }

        private static bool IsNativeLibrary(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext is ".dll" or ".so" or ".dylib";
        }
    }

}
