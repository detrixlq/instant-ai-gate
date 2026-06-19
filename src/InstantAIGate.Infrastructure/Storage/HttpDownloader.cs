using InstantAIGate.Application.Dtos.Streaming;
using Microsoft.Extensions.Logging;
using System.Net;

namespace InstantAIGate.Infrastructure.Storage
{
    public class HttpDownloader : IHttpDownloader
    {
        private readonly ILogger<HttpDownloader> _logger;

        public HttpDownloader(ILogger<HttpDownloader> logger)
        {
            _logger = logger;
        }

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) InstantAIGate-Client");
            client.Timeout = TimeSpan.FromHours(3);
            return client;
        }

        public async IAsyncEnumerable<DownloadProgress> DownloadToStreamAsync(
            string url,
            string fileName,
            long expectedSizeBytes,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            using var client = CreateClient();
            _logger.LogInformation("Starting download {File} from {Url}", fileName, url);

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

            if ((int)response.StatusCode == 302 || (int)response.StatusCode == 301)
            {
                var redirectUrl = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(redirectUrl))
                {
                    _logger.LogWarning("Redirect to {RedirectUrl}", redirectUrl);
                    response.Dispose();
                    using var redirected = await client.GetAsync(redirectUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                    await foreach (var p in ProcessResponse(fileName, redirected, expectedSizeBytes, ct))
                        yield return p;
                    yield break;
                }
            }

            await foreach (var p in ProcessResponse(fileName, response, expectedSizeBytes, ct))
                yield return p;
        }

        private async IAsyncEnumerable<DownloadProgress> ProcessResponse(
            string fileName,
            HttpResponseMessage response,
            long expectedSizeBytes,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType == "text/html")
                throw new Exception($"Received HTML instead of binary. URL returned an error page.");

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;

            if (expectedSizeBytes > 0 && totalBytes > 0 && totalBytes < expectedSizeBytes * 0.9)
            {
                throw new Exception($"File too small. Expected ~{expectedSizeBytes} bytes, got {totalBytes}.");
            }

            using var downloadStream = await response.Content.ReadAsStreamAsync(ct);

            var buffer = new byte[256 * 1024];
            long totalRead = 0;
            int read;

            while ((read = await downloadStream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                totalRead += read;

                var percent = totalBytes > 0 ? (double)totalRead / totalBytes * 100.0 : 0.0;
                yield return new DownloadProgress(fileName, totalBytes, totalRead, percent, false);
            }

            yield return new DownloadProgress(fileName, totalBytes, totalRead, 100, true);
        }

        public async Task<HttpDownloadResponse> GetResponseStreamAsync(string url, CancellationToken ct = default)
        {
            var client = CreateClient();
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

            if ((int)response.StatusCode == 302 || (int)response.StatusCode == 301)
            {
                var redirectUrl = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(redirectUrl))
                {
                    _logger.LogWarning("Redirect to {RedirectUrl}", redirectUrl);
                    response.Dispose();
                    response = await client.GetAsync(redirectUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                }
            }

            var stream = await response.Content.ReadAsStreamAsync(ct);
            return new HttpDownloadResponse(client, response, stream);
        }
    }
}

