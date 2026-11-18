using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Categories;

/// <summary>Categoría de error gramatical (<c>api/ErrorClassification</c>).</summary>
public sealed class ErrorCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class ErrorCategorySearchRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public ErrorCategorySearchFilter? Filter { get; set; }
}

public sealed record ErrorCategorySearchFilter
{
    [StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
    public string? Name { get; set; }
    public string? Description { get; set; }
    /// <summary>null = todas; true = activas; false = deshabilitadas.</summary>
    public bool? Active { get; set; }

    public ErrorCategorySearchFilter? Normalize()
    {
        var normalized = this with
        {
            Name = string.IsNullOrWhiteSpace(Name) ? null : Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
        };
        return normalized.Name is null && normalized.Description is null && normalized.Active is null ? null : normalized;
    }
}

public sealed class ErrorCategoryCreateRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool Active { get; set; } = true;
}

/// <summary>Solo se envían los campos que cambian; <c>Active = false</c> deshabilita la categoría.</summary>
public sealed class ErrorCategoryUpdateRequest
{
    public Guid Id { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 100 caracteres.")]
    public string? Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Active { get; set; }
}
