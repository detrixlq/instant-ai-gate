using InstantAIGate.Application.Dtos.Streaming;
namespace InstantAIGate.Infrastructure.Storage
{
    public interface IHttpDownloader
    {
        IAsyncEnumerable<DownloadProgress> DownloadToStreamAsync(
            string url,
            string fileName,
            long expectedSizeBytes,
            CancellationToken ct = default);

        Task<HttpDownloadResponse> GetResponseStreamAsync(string url, CancellationToken ct = default);
    }
}
