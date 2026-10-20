using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Courses;

public sealed class CourseSearchRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public CourseSearchFilter? Filter { get; set; }
}

public sealed record CourseSearchFilter
{
    private const string GuidPattern = @"^\s*[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\s*$";

    [RegularExpression(GuidPattern, ErrorMessage = "El ID del curso debe ser un UUID válido.")]
    public string? Id { get; set; }
    [RegularExpression(GuidPattern, ErrorMessage = "El ID del profesor debe ser un UUID válido.")]
    public string? TeacherId { get; set; }
    [RegularExpression(GuidPattern, ErrorMessage = "Selecciona un módulo válido.")]
    public string? ModuleNameId { get; set; }
    public int? Bimester { get; set; }
    public int? Year { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    [StringLength(10, ErrorMessage = "El paralelo admite hasta 10 caracteres.")]
    public string? Parallel { get; set; }
    [RegularExpression(GuidPattern, ErrorMessage = "El ID del aula debe ser un UUID válido.")]
    public string? ClassroomId { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public DateOnly? CreatedAt { get; set; }
    public bool? Active { get; set; }

    public CourseSearchFilter Normalize() => this with
    {
        Id = Trim(Id), TeacherId = Trim(TeacherId), ModuleNameId = Trim(ModuleNameId),
        ClassroomId = Trim(ClassroomId), Parallel = Trim(Parallel)
    };

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
