using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Classrooms;

/// <summary>Aula (<c>api/Classroom</c>): nombre y capacidad máxima de estudiantes.</summary>
public sealed class ClassroomItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class ClassroomSearchRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public ClassroomSearchFilter? Filter { get; set; }
}

public sealed record ClassroomSearchFilter
{
    [StringLength(30, ErrorMessage = "El nombre admite hasta 30 caracteres.")]
    public string? Name { get; set; }
    [Range(1, 1000, ErrorMessage = "La capacidad debe estar entre 1 y 1000.")]
    public int? Capacity { get; set; }
    /// <summary>null = todas; true = activas; false = deshabilitadas.</summary>
    public bool? Active { get; set; }

    public ClassroomSearchFilter? Normalize()
    {
        var normalized = this with { Name = string.IsNullOrWhiteSpace(Name) ? null : Name.Trim() };
        return normalized.Name is null && normalized.Capacity is null && normalized.Active is null ? null : normalized;
    }
}

public sealed class ClassroomCreateRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(30, ErrorMessage = "El nombre admite hasta 30 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "La capacidad debe estar entre 1 y 1000.")]
    public int Capacity { get; set; } = 20;

    public bool Active { get; set; } = true;
}

/// <summary>Solo se envían los campos que cambian; <c>Active = false</c> deshabilita el aula.</summary>
public sealed class ClassroomUpdateRequest
{
    public Guid Id { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [StringLength(30, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 30 caracteres.")]
    public string? Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Range(1, 1000, ErrorMessage = "La capacidad debe estar entre 1 y 1000.")]
    public int? Capacity { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Active { get; set; }
}
