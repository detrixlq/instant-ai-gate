using InstantAIGate.Application.Dtos.Streaming;
using InstantAIGate.Domain.Entities;

namespace InstantAIGate.Application.Interfaces.Storage
{
    public interface IModelStorageService
    {
        string GetStoragePath(string repoId);

        IAsyncEnumerable<DownloadProgress> DownloadModelFileAsync(
            string url,
            string repoId,
            string filePath,
            long expectedSizeBytes,
            CancellationToken ct = default);
    }
}
