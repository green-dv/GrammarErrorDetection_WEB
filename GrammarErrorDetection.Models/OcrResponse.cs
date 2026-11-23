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

    [JsonPropertyName("lines")]
    public List<OcrLine> Lines { get; set; } = [];

    [JsonPropertyName("average_confidence")]
    public double AverageConfidence { get; set; }
}

public sealed class OcrLine
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    // El servidor marca las líneas leídas con poca seguridad para que el estudiante las revise.
    [JsonPropertyName("needs_review")]
    public bool NeedsReview { get; set; }

    [JsonPropertyName("engine")]
    public string Engine { get; set; } = "";
}
