using InstantAIGate.Domain.Enums;
using System.Text.Json.Serialization;

namespace InstantAIGate.Admin.Dtos;

public class ModelViewItem
{
    public string RepoId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ModelType ModelType { get; set; } = ModelType.Llm;


    // UI State
    public bool IsDownloaded { get; set; }
    public bool IsDownloading { get; set; }
    public double DownloadProgress { get; set; }

}