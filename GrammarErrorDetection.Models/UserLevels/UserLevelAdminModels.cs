using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.UserLevels;

/// <summary>Nivel de usuario (<c>api/UserLevel</c>): se alcanza al sumar los puntos mínimos.</summary>
public sealed class UserLevelItem
{
    public Guid Id { get; set; }
    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public long MinPoints { get; set; }
    public bool Active { get; set; }
    /// <summary>Usuarios que tienen hoy este nivel; solo viene en los listados.</summary>
    public long UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class UserLevelSearchRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public UserLevelSearchFilter? Filter { get; set; }
}

public sealed record UserLevelSearchFilter
{
    [StringLength(50, ErrorMessage = "El nombre admite hasta 50 caracteres.")]
    public string? Name { get; set; }
    /// <summary>null = todos; true = activos; false = deshabilitados.</summary>
    public bool? Active { get; set; }

    public UserLevelSearchFilter? Normalize()
    {
        var normalized = this with { Name = string.IsNullOrWhiteSpace(Name) ? null : Name.Trim() };
        return normalized.Name is null && normalized.Active is null ? null : normalized;
    }
}

public sealed class UserLevelCreateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "El nivel debe ser un entero positivo.")]
    public int Level { get; set; } = 1;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre admite hasta 50 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, long.MaxValue, ErrorMessage = "Los puntos mínimos no pueden ser negativos.")]
    public long MinPoints { get; set; }

    public bool Active { get; set; } = true;
}

/// <summary>Solo se envían los campos que cambian; <c>Active = false</c> deshabilita el nivel.</summary>
public sealed class UserLevelUpdateRequest
{
    public Guid Id { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Range(1, int.MaxValue, ErrorMessage = "El nivel debe ser un entero positivo.")]
    public int? Level { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 50 caracteres.")]
    public string? Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Range(0, long.MaxValue, ErrorMessage = "Los puntos mínimos no pueden ser negativos.")]
    public long? MinPoints { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Active { get; set; }
}
