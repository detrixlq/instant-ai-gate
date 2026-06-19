namespace InstantAIGate.Application.Dtos.Streaming
{
    public class DownloadProgress(
        string fileName,
        long totalBytes,
        long bytesDownloaded,
        double percent,
        bool isCompleted)
    {
        public string FileName { get; } = fileName;
        public long TotalBytes { get; } = totalBytes;
        public long BytesDownloaded { get; } = bytesDownloaded;
        public double Percent { get; } = percent;
        public bool IsCompleted { get; } = isCompleted;

        public override string ToString()
        {
            return $"{FileName}: {BytesDownloaded}/{TotalBytes} bytes ({Percent:F2}%), completed={IsCompleted}";
        }
    }

}
