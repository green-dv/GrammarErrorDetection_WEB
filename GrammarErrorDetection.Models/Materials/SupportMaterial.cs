using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Materials;

public sealed class ErrorClassificationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class FileTypeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Extension { get; set; } = "";
    public string MimeType { get; set; } = "";
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class SupportMaterialSearchRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public SupportMaterialFilter? Filter { get; set; }
}

public sealed class SupportMaterialFilter
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public Guid? ErrorClassificationId { get; set; }
    public DateOnly? UploadDate { get; set; }
    public string? FileLink { get; set; }
    public Guid? FileTypeId { get; set; }
    public long? FileSize { get; set; }
    public Guid? UploadedBy { get; set; }
    public bool? Active { get; set; }
    public DateOnly? CreatedAt { get; set; }
}

public sealed class SupportMaterialResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid ErrorClassificationId { get; set; }
    public string ErrorClassificationName { get; set; } = "";
    public DateTime UploadDate { get; set; }
    public string FileLink { get; set; } = "";
    public Guid FileTypeId { get; set; }
    public long? FileSize { get; set; }
    public Guid UploadedBy { get; set; }
    public string UploadedByName { get; set; } = "";
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class SupportMaterialCreateRequest
{
    public string Name { get; set; } = "";
    public Guid ErrorClassificationId { get; set; }
    public DateTime UploadDate { get; set; }
    public string FileLink { get; set; } = "";
    public Guid FileTypeId { get; set; }
    public Guid UploadedBy { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? FileSize { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class SupportMaterialUpdateRequest
{
    public Guid Id { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ErrorClassificationId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? UploadDate { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileLink { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? FileTypeId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? FileSize { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? UploadedBy { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Active { get; set; }

    [JsonIgnore]
    public bool HasChanges => Name is not null || ErrorClassificationId is not null || UploadDate is not null
        || FileLink is not null || FileTypeId is not null || FileSize is not null || UploadedBy is not null || Active is not null;
}
