using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.ConditionTypes;

/// <summary>Tipo de condición de gamificación (<c>api/ConditionType</c>). El <c>Code</c> lo calcula el motor y no se edita.</summary>
public sealed class ConditionTypeItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ValueType { get; set; } = string.Empty;
    public bool Active { get; set; }
    /// <summary>Condiciones activas de logros y desafíos que usan el tipo; solo viene en los listados.</summary>
    public long UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class ConditionTypeSearchRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public ConditionTypeSearchFilter? Filter { get; set; }
}

public sealed record ConditionTypeSearchFilter
{
    [StringLength(50, ErrorMessage = "El código admite hasta 50 caracteres.")]
    public string? Code { get; set; }
    [StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
    public string? Name { get; set; }
    /// <summary>null = todos; true = activos; false = deshabilitados.</summary>
    public bool? Active { get; set; }

    public ConditionTypeSearchFilter? Normalize()
    {
        var normalized = this with
        {
            Code = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim(),
            Name = string.IsNullOrWhiteSpace(Name) ? null : Name.Trim()
        };
        return normalized.Code is null && normalized.Name is null && normalized.Active is null ? null : normalized;
    }
}

public sealed class ConditionTypeCreateRequest
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [RegularExpression("^[A-Z][A-Z0-9_]{1,49}$", ErrorMessage = "El código va en MAYÚSCULAS, con números o guion bajo (2 a 50 caracteres).")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    public string ValueType { get; set; } = "Integer";

    public bool Active { get; set; } = true;
}

/// <summary>Solo se editan nombre, descripción y estado; <c>Code</c> y <c>ValueType</c> nunca se envían.</summary>
public sealed class ConditionTypeUpdateRequest
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
