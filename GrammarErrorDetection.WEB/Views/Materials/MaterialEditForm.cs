using GrammarErrorDetection.Models.Materials;

namespace GrammarErrorDetection.WEB.Views.Materials;

public sealed class MaterialEditForm
{
    public string Name { get; set; } = "";
    public string FileLink { get; set; } = "";
    public string ErrorClassificationId { get; set; } = "";
    public string FileTypeId { get; set; } = "";
    public DateTime UploadDate { get; set; } = DateTime.Now;
    public long? FileSize { get; set; }

    public static MaterialEditForm From(SupportMaterialResponse material) => new()
    {
        Name = material.Name, FileLink = material.FileLink,
        ErrorClassificationId = material.ErrorClassificationId.ToString("D"),
        FileTypeId = material.FileTypeId.ToString("D"), UploadDate = material.UploadDate,
        FileSize = material.FileSize
    };

    public List<string> Validate(string apiBaseUrl)
    {
        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(Name) || Name.Trim().Length > 255) errors.Add("El nombre debe tener entre 1 y 255 caracteres.");
        if (MaterialPresentation.ResolveLink(FileLink, apiBaseUrl) is null) errors.Add("Ingresa un enlace HTTP/HTTPS o una ruta del archivo alojado.");
        if (!Guid.TryParse(ErrorClassificationId, out var category) || category == Guid.Empty) errors.Add("Selecciona o ingresa una clasificación válida.");
        if (!Guid.TryParse(FileTypeId, out var type) || type == Guid.Empty) errors.Add("Selecciona o ingresa un tipo de archivo válido.");
        if (UploadDate == default) errors.Add("Indica la fecha de subida.");
        if (FileSize < 0) errors.Add("El tamaño del archivo no puede ser negativo.");
        return errors;
    }

    public SupportMaterialCreateRequest CreateRequest(Guid userId) => new()
    {
        Name = Name.Trim(), FileLink = FileLink.Trim(), ErrorClassificationId = Guid.Parse(ErrorClassificationId),
        FileTypeId = Guid.Parse(FileTypeId), UploadDate = UploadDate, FileSize = FileSize, UploadedBy = userId, Active = true
    };

    public SupportMaterialUpdateRequest ChangesFrom(SupportMaterialResponse material) => new()
    {
        Id = material.Id,
        Name = Name.Trim() == material.Name ? null : Name.Trim(),
        FileLink = FileLink.Trim() == material.FileLink ? null : FileLink.Trim(),
        ErrorClassificationId = Guid.TryParse(ErrorClassificationId, out var category) && category != material.ErrorClassificationId ? category : null,
        FileTypeId = Guid.TryParse(FileTypeId, out var type) && type != material.FileTypeId ? type : null,
        UploadDate = UploadDate == material.UploadDate ? null : UploadDate,
        FileSize = FileSize == material.FileSize ? null : FileSize
    };
}
