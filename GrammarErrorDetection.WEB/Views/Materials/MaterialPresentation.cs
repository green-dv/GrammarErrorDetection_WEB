using GrammarErrorDetection.Models.Materials;

namespace GrammarErrorDetection.WEB.Views.Materials;

/// <summary>Cómo se previsualiza un material: imagen, iframe, solo descarga o enlace externo.</summary>
public enum MaterialPreviewKind { Image, Frame, Download, External }

/// <summary>Datos comunes para el visor de materiales (listado y recomendaciones).</summary>
public sealed record MaterialView(
    string Name,
    string? Category,
    string? CategoryDescription,
    string Format,
    string TypeLabel,
    long? FileSize,
    DateTime UploadDate,
    string? UploadedBy,
    string? Link,
    string? Extension);

public static class MaterialPresentation
{
    // Deben coincidir con los formatos que el servidor de archivos (nginx) entrega inline.
    private static readonly HashSet<string> ImageExtensions = ["png", "jpg", "jpeg", "gif", "webp", "avif", "svg"];
    private static readonly HashSet<string> FrameExtensions = ["pdf", "txt"];

    /// <summary>Extensión en minúsculas y sin punto, a partir de una extensión, nombre de archivo o enlace.</summary>
    public static string Extension(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var text = value.Trim();
        if (Uri.TryCreate(text, UriKind.Absolute, out var uri)) text = uri.AbsolutePath;
        text = text.Split('?', '#')[0];
        var extension = text.Contains('.') ? Path.GetExtension(text) : text;
        return extension.TrimStart('.').ToLowerInvariant();
    }

    /// <summary>True si el enlace apunta al servidor de archivos propio (/archivos/…).</summary>
    public static bool IsFileServerLink(string? link)
    {
        if (string.IsNullOrWhiteSpace(link)) return false;
        var path = Uri.TryCreate(link, UriKind.Absolute, out var uri) ? uri.AbsolutePath : link;
        return path.StartsWith("/archivos/", StringComparison.OrdinalIgnoreCase);
    }

    public static MaterialPreviewKind PreviewKind(string? link, string? extension = null)
    {
        if (!IsFileServerLink(link)) return MaterialPreviewKind.External;
        return ExtensionKind(string.IsNullOrWhiteSpace(extension) ? Extension(link) : Extension(extension));
    }

    /// <summary>Tipo de vista previa de un archivo local (antes de subirlo).</summary>
    public static MaterialPreviewKind LocalPreviewKind(string fileName) => ExtensionKind(Extension(fileName));

    private static MaterialPreviewKind ExtensionKind(string extension) =>
        ImageExtensions.Contains(extension) ? MaterialPreviewKind.Image
        : FrameExtensions.Contains(extension) ? MaterialPreviewKind.Frame
        : MaterialPreviewKind.Download;

    /// <summary>Enlace que fuerza la descarga en el servidor de archivos; los enlaces externos se devuelven igual.</summary>
    public static string? DownloadLink(string? resolvedLink)
    {
        if (resolvedLink is null || !IsFileServerLink(resolvedLink)) return resolvedLink;
        return resolvedLink + (resolvedLink.Contains('?') ? "&" : "?") + "download=1";
    }

    public static string FormatFromExtension(string? extension, string? mimeType = null)
    {
        var format = Extension(extension).ToUpperInvariant();
        if (format.Length is > 0 and <= 5) return format;
        return mimeType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true ? "IMG" : "URL";
    }

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
