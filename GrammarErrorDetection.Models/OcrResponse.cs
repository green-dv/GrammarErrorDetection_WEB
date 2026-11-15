using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models;

public sealed class OcrResponse
{
    [JsonPropertyName("paragraph")]
    public string Paragraph { get; set; } = "";

    [JsonPropertyName("line_count")]
    public int LineCount { get; set; }

    [JsonPropertyName("model_name")]
    public string ModelName { get; set; } = "";

    [JsonPropertyName("image_width")]
    public int ImageWidth { get; set; }

    [JsonPropertyName("image_height")]
    public int ImageHeight { get; set; }
}
