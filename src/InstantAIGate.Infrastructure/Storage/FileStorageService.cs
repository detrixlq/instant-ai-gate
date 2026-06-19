using Microsoft.Extensions.Logging;

namespace InstantAIGate.Infrastructure.Storage
{
    public class FileStorageService : IFileStorageService
    {
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(ILogger<FileStorageService> logger)
        {
            _logger = logger;
        }

        public void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public bool FileExists(string path) => File.Exists(path);

        public long GetFileSize(string path)
        {
            if (!File.Exists(path)) return 0;
            return new FileInfo(path).Length;
        }

        public string GetTempPath(string destinationPath) => destinationPath + ".tmp";

        public void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file {Path}", path);
            }
        }

        public void MoveFileAtomic(string sourcePath, string destinationPath)
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            File.Move(sourcePath, destinationPath);
        }
    }
}
