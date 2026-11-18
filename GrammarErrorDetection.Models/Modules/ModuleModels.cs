using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Modules;

/// <summary>Módulo del ciclo de cursos (<c>api/ModuleName</c>): un nivel y el módulo al que se promueve después.</summary>
public sealed class ModuleItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public Guid? NextModule { get; set; }
    public string? NextModuleName { get; set; }
    public bool Active { get; set; }
}

public sealed class ModuleSearchRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public ModuleSearchFilter? Filter { get; set; }
}

public sealed record ModuleSearchFilter
{
    [StringLength(30, ErrorMessage = "El nombre admite hasta 30 caracteres.")]
    public string? Name { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "El nivel debe ser un entero positivo.")]
    public int? Level { get; set; }
    /// <summary>null = todos; true = activos; false = deshabilitados.</summary>
    public bool? Active { get; set; }

    public ModuleSearchFilter? Normalize()
    {
        var normalized = this with { Name = string.IsNullOrWhiteSpace(Name) ? null : Name.Trim() };
        return normalized.Name is null && normalized.Level is null && normalized.Active is null ? null : normalized;
    }
}

public sealed class ModuleCreateRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(30, ErrorMessage = "El nombre admite hasta 30 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "El nivel debe ser un entero positivo.")]
    public int Level { get; set; } = 1;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? NextModule { get; set; }

    public bool Active { get; set; } = true;
}

/// <summary>Solo se envían los campos que cambian. <c>ClearNextModule</c> quita el módulo siguiente; <c>Active = false</c> deshabilita.</summary>
public sealed class ModuleUpdateRequest
{
    public Guid Id { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [StringLength(30, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 30 caracteres.")]
    public string? Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Range(1, int.MaxValue, ErrorMessage = "El nivel debe ser un entero positivo.")]
    public int? Level { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? NextModule { get; set; }

    public bool ClearNextModule { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Active { get; set; }
}
