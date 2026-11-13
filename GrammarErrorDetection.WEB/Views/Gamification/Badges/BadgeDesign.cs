namespace GrammarErrorDetection.WEB.Views.Gamification.Badges;

public sealed record BadgeDesign
{
    public string CenterId { get; set; } = "circle";
    public string? IconId { get; set; }
    public string? TopId { get; set; }
    public bool ShowRibbon { get; set; }
    public string RibbonText { get; set; } = "";
    public string BaseColor { get; set; } = "#D6A744";
}

public sealed record GeneratedBadge(byte[] Bytes, BadgeDesign Design)
{
    public string FileName => "logro-personalizado.png";
    public string ContentType => "image/png";
}

// Rutas locales de las piezas PNG. Se incrustan como datos al componer/exportar.
public sealed record BadgeAsset(string Id, string Name, string Path, int SourceWidth = 0, int SourceHeight = 0, int StarWidth = 0);

public readonly record struct BadgeImageBounds(decimal X, decimal Y, decimal Width, decimal Height);
