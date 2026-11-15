using GrammarErrorDetection.Models.Materials;

namespace GrammarErrorDetection.WEB.Views.Materials;

public static class MaterialFileTypes
{
    public static FileTypeResponse? ForLink(IEnumerable<FileTypeResponse> types) => types.FirstOrDefault(type =>
        type.Active && (type.MimeType.Equals("text/uri-list", StringComparison.OrdinalIgnoreCase)
        || new[] { "url", "enlace", "enlaces", "link", "links", "enlace externo" }.Contains(type.Name.Trim().ToLowerInvariant())
        || Extensions(type).Contains("url")));

    public static FileTypeResponse? ForFile(IEnumerable<FileTypeResponse> types, string name, string mimeType)
    {
        var active = types.Where(type => type.Active).ToList();
        var extension = Path.GetExtension(name).TrimStart('.').ToLowerInvariant();
        var exact = active.FirstOrDefault(type => extension.Length > 0 && Extensions(type).Contains(extension));
        if (exact is not null) return exact;
        if (!string.IsNullOrWhiteSpace(mimeType) && mimeType != "application/octet-stream")
        {
            exact = active.FirstOrDefault(type => type.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase));
            if (exact is not null) return exact;
        }
        var family = extension switch
        {
            "png" or "jpg" or "jpeg" or "gif" or "webp" or "avif" or "svg" or "bmp" => "image",
            "mp3" or "wav" or "ogg" or "m4a" => "audio",
            "mp4" or "webm" or "mov" => "video",
            _ => mimeType.Split('/')[0].ToLowerInvariant()
        };
        return family is "image" or "audio" or "video"
            ? active.FirstOrDefault(type => type.MimeType.Equals(family + "/*", StringComparison.OrdinalIgnoreCase)
                || (family == "image" && new[] { "imagen", "imagenes", "imágenes" }.Contains(type.Name.Trim().ToLowerInvariant()))
                || (family == "audio" && type.Name.Equals("Audio", StringComparison.OrdinalIgnoreCase))
                || (family == "video" && new[] { "video", "videos", "vídeo", "vídeos" }.Contains(type.Name.Trim().ToLowerInvariant())))
            : null;
    }

    private static IEnumerable<string> Extensions(FileTypeResponse type) => type.Extension
        .Split([',', ';', '|', ' ', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(value => value.TrimStart('*', '.').ToLowerInvariant());
}
