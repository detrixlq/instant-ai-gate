namespace InstantAIGate.Infrastructure.Storage
{
    public sealed class HttpDownloadResponse : IDisposable
    {
        public HttpClient Client { get; }
        public HttpResponseMessage Response { get; }
        public Stream Stream { get; }

        public HttpDownloadResponse(HttpClient client, HttpResponseMessage response, Stream stream)
        {
            Client = client;
            Response = response;
            Stream = stream;
        }

        public void Dispose()
        {
            try { Stream?.Dispose(); } catch { }
            try { Response?.Dispose(); } catch { }
            try { Client?.Dispose(); } catch { }
        }
    }
}
