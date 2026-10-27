using GrammarErrorDetection.Models.Materials;

namespace GrammarErrorDetection.WEB.Views.Materials;

public static class MaterialPresentation
{
    public static string? ResolveLink(string? link, string apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(link)) return null;
        var value = link.Trim();
        if (value.StartsWith("//", StringComparison.Ordinal) || value.Contains('\\') || value.Any(char.IsControl)) return null;
        if (!Uri.TryCreate(new Uri(apiBaseUrl), value, out var uri)
            || uri.Scheme is not ("https" or "http") || !string.IsNullOrEmpty(uri.UserInfo)) return null;
        return uri.AbsoluteUri;
    }

    public static string Format(SupportMaterialResponse material, FileTypeResponse? type = null)
    {
        if (type is not null)
        {
            var format = type.Extension.Trim().TrimStart('.').ToUpperInvariant();
            if (format.Length > 0 && format.Length <= 8) return format;
            if (type.Name.Length > 0 && type.Name.Length <= 8) return type.Name.ToUpperInvariant();
        }
        var path = material.FileLink.Split('?', '#')[0];
        var extension = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
        return extension is "PDF" or "PNG" or "JPG" or "JPEG" or "WEBP" or "GIF" or "AVIF" or "SVG"
            or "DOC" or "DOCX" or "PPT" or "PPTX" or "XLS" or "XLSX" or "TXT" or "MP3" or "MP4"
            ? extension : "URL";
    }

    public static string Kind(SupportMaterialResponse material, FileTypeResponse? type = null)
    {
        if (type?.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true) return "Imagen";
        if (string.Equals(type?.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase)) return "PDF";
        return Format(material, type) switch
        {
            "PDF" => "PDF",
            "PNG" or "JPG" or "JPEG" or "WEBP" or "GIF" or "AVIF" or "SVG" => "Imagen",
            "URL" => "Enlace",
            _ => "Documento"
        };
    }

    public static string Size(long? bytes) => bytes switch
    {
        null => "No indicado",
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024d:0.#} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024d * 1024):0.#} MB",
        _ => $"{bytes / (1024d * 1024 * 1024):0.#} GB"
    };
}
