using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml;

namespace GrammarErrorDetection.WEB.Views.Gamification.Badges;

public static partial class BadgeRenderer
{
    public const int RibbonTextLimit = 16;
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    public static List<string> Validate(BadgeDesign design)
    {
        var errors = new List<string>();
        if (!BadgeCatalog.Centers.Any(a => a.Id == design.CenterId)) errors.Add("Selecciona un centro de badge.");
        if (design.IconId is not null && !BadgeCatalog.Icons.Any(a => a.Id == design.IconId)) errors.Add("Selecciona un elemento central válido.");
        if (design.TopId is not null && !BadgeCatalog.Tops.Any(a => a.Id == design.TopId)) errors.Add("Selecciona un elemento superior válido.");
        if (!HexColor().IsMatch(design.BaseColor ?? "")) errors.Add("Selecciona un color válido.");
        if (design.ShowRibbon && (string.IsNullOrWhiteSpace(design.RibbonText) || design.RibbonText.Length > RibbonTextLimit))
            errors.Add($"Escribe un texto para la cinta de 1 a {RibbonTextLimit} caracteres.");
        if (design.ShowRibbon)
        {
            try { XmlConvert.VerifyXmlChars(design.RibbonText); }
            catch (XmlException) { errors.Add("El texto de la cinta contiene caracteres no válidos."); }
        }
        return errors;
    }
    public static string Render(BadgeDesign design, IReadOnlyDictionary<string, string> images)
        => RenderCore(design, asset => images.TryGetValue(asset.Path, out var data) && data.StartsWith("data:image/png;base64,", StringComparison.Ordinal)
            ? data : throw new InvalidOperationException("No se cargó una de las piezas del badge."));

    // Referencias locales pequeñas: al escribir no se vuelve a codificar ningún PNG.
    public static string RenderPreview(BadgeDesign design) => RenderCore(design, asset => asset.Path);
    public static string PreviewLayers(BadgeDesign design)
    {
        var root = XElement.Parse(RenderPreview(design with { RibbonText = "" }));
        root.Descendants(Svg + "text").Remove();
        return string.Concat(root.Nodes().Select(node => node.ToString(SaveOptions.DisableFormatting)));
    }
    public static string CleanRibbonText(string value) => string.Concat(value.EnumerateRunes().Where(rune => rune.Value is 9 or 10 or 13 ||
        rune.Value is >= 0x20 and <= 0xD7FF or >= 0xE000 and <= 0xFFFD or >= 0x10000 and <= 0x10FFFF).Take(RibbonTextLimit)).Trim();
    public static string RibbonFontSize(string text) => Math.Min(25m, 280m / Math.Max(1, text.Length)).ToString("0.##", CultureInfo.InvariantCulture);
    public static BadgeImageBounds StarBounds(BadgeAsset asset)
    {
        if (asset.StarWidth <= 0 || asset.SourceWidth <= 0 || asset.SourceHeight <= 0) throw new ArgumentException("Geometría de estrellas no válida.");
        const decimal starSize = 56;
        var width = asset.SourceWidth * starSize / asset.StarWidth;
        var height = asset.SourceHeight * starSize / asset.StarWidth;
        return new((512 - width) / 2, 112, width, height);
    }
    private static string RenderCore(BadgeDesign design, Func<BadgeAsset, string> source)
    {
        var center = BadgeCatalog.Centers.Single(a => a.Id == design.CenterId);
        if (!HexColor().IsMatch(design.BaseColor ?? "")) throw new ArgumentException("Color no válido.");
        var channels = new[] { 1, 3, 5 }.Select(start =>
            (int.Parse(design.BaseColor!.Substring(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255m).ToString("0.######", CultureInfo.InvariantCulture)).ToArray();
        var matrix = $"{channels[0]} 0 0 0 0 0 {channels[1]} 0 0 0 0 0 {channels[2]} 0 0 0 0 0 1 0";
        var root = new XElement(Svg + "svg", new XAttribute("viewBox", "0 0 512 512"), new XAttribute("width", 512), new XAttribute("height", 512),
            new XElement(Svg + "defs", new XElement(Svg + "filter", new XAttribute("id", "badge-base-tint"), new XAttribute("color-interpolation-filters", "sRGB"),
                new XElement(Svg + "feColorMatrix", new XAttribute("type", "matrix"), new XAttribute("values", matrix)))));
        // Multiplicador compartido para las dos piezas blancas: base y estrellas.
        root.Add(new XElement(Svg + "g", new XAttribute("data-layer", "center"), new XAttribute("filter", "url(#badge-base-tint)"), Place(center, source, 36, 36, 440, 440)));
        if (design.IconId is { } iconId)
            root.Add(new XElement(Svg + "g", new XAttribute("data-layer", "icon"), Place(BadgeCatalog.Icons.Single(a => a.Id == iconId), source, 160, 156, 192, 192)));
        if (design.TopId is { } topId)
        {
            var stars = BadgeCatalog.Tops.Single(a => a.Id == topId);
            var bounds = StarBounds(stars);
            root.Add(new XElement(Svg + "g", new XAttribute("data-layer", "top"), new XAttribute("filter", "url(#badge-base-tint)"),
                Place(stars, source, bounds.X, bounds.Y, bounds.Width, bounds.Height)));
        }
        if (design.ShowRibbon)
        {
            var text = CleanRibbonText(design.RibbonText);
            var fontSize = RibbonFontSize(text);
            root.Element(Svg + "defs")!.Add(new XElement(Svg + "mask", new XAttribute("id", "badge-ribbon-mask"), new XAttribute("maskUnits", "userSpaceOnUse"),
                new XAttribute("x", 0), new XAttribute("y", 0), new XAttribute("width", 512), new XAttribute("height", 512),
                new XAttribute("style", "mask-type:alpha"), Place(center, source, 36, 36, 440, 440)));
            root.Add(new XElement(Svg + "g", new XAttribute("data-layer", "ribbon"), new XAttribute("mask", "url(#badge-ribbon-mask)"),
                new XElement(Svg + "g", new XAttribute("transform", "rotate(-15 256 342)"),
                new XElement(Svg + "rect", new XAttribute("x", 0), new XAttribute("y", 300), new XAttribute("width", 512), new XAttribute("height", 86),
                    new XAttribute("fill", "#F8F6EF"), new XAttribute("stroke", "#D5D4CF"), new XAttribute("stroke-width", 1)),
                new XElement(Svg + "text", new XAttribute("x", 256), new XAttribute("y", 351), new XAttribute("text-anchor", "middle"),
                    new XAttribute("font-family", "Arial, sans-serif"), new XAttribute("font-size", fontSize), new XAttribute("font-weight", 700),
                    new XAttribute("fill", "#24323B"), text.Trim()))));
        }
        return root.ToString(SaveOptions.DisableFormatting);
    }
    public static string DataUrl(BadgeDesign design, IReadOnlyDictionary<string, string> images) => "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(Render(design, images)));
    private static XElement Place(BadgeAsset asset, Func<BadgeAsset, string> source, decimal x, decimal y, decimal width, decimal height)
    {
        return new XElement(Svg + "image", new XAttribute("x", x), new XAttribute("y", y), new XAttribute("width", width),
            new XAttribute("height", height), new XAttribute("preserveAspectRatio", "xMidYMid meet"), new XAttribute("href", source(asset)));
    }
    [GeneratedRegex("^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColor();
}
