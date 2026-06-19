namespace InstantAIGate.Infrastructure.Storage
{
    public interface IFileStorageService
    {
        void EnsureDirectoryExists(string path);
        bool FileExists(string path);
        long GetFileSize(string path);
        string GetTempPath(string destinationPath);
        void DeleteIfExists(string path);
        void MoveFileAtomic(string sourcePath, string destinationPath);
    }
}
