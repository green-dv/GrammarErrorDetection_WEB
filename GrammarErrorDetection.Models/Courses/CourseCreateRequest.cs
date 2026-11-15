using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Courses;

public abstract class CourseWriteRequest : IValidatableObject
{
    public Guid TeacherId { get; set; }
    public Guid ModuleNameId { get; set; }
    public Guid ClassroomId { get; set; }
    [Range(1, 9999, ErrorMessage = "Indica un año válido.")]
    public int Year { get; set; } = DateTime.Now.Year;
    [Range(1, 6, ErrorMessage = "Selecciona un bimestre.")]
    public int Bimester { get; set; } = (DateTime.Now.Month - 1) / 2 + 1;
    [Required(ErrorMessage = "Indica la fecha de inicio.")]
    public DateTime? StartDate { get; set; }
    [Required(ErrorMessage = "Indica la fecha de fin.")]
    public DateTime? EndDate { get; set; }
    [Required(ErrorMessage = "Indica el paralelo.")]
    [StringLength(10, ErrorMessage = "El paralelo admite hasta 10 caracteres.")]
    public string Parallel { get; set; } = "A";
    public TimeOnly StartTime { get; set; } = new(8, 0);
    public TimeOnly EndTime { get; set; } = new(10, 0);
    public bool Active { get; set; } = true;
    [Required(ErrorMessage = "La lista de estudiantes es obligatoria.")]
    public List<Guid> StudentIds { get; set; } = [];

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TeacherId == Guid.Empty) yield return new("Selecciona un profesor.", [nameof(TeacherId)]);
        if (ModuleNameId == Guid.Empty) yield return new("Selecciona un módulo.", [nameof(ModuleNameId)]);
        if (ClassroomId == Guid.Empty) yield return new("Selecciona un aula.", [nameof(ClassroomId)]);
        if (StartDate.HasValue && EndDate.HasValue && (EndDate < StartDate || StartDate.Value.Year != Year || EndDate.Value.Year != Year))
            yield return new("Las fechas deben estar ordenadas y pertenecer al año del curso.", [nameof(StartDate), nameof(EndDate)]);
        if (EndTime <= StartTime) yield return new("La hora de fin debe ser posterior a la de inicio.", [nameof(EndTime)]);
        if (!Active) yield return new("El curso debe estar activo para inscribir estudiantes.", [nameof(Active)]);
        if (StudentIds is not null && (StudentIds.Contains(Guid.Empty) || StudentIds.Count != StudentIds.Distinct().Count()))
            yield return new("Los estudiantes deben tener IDs válidos y no repetirse.", [nameof(StudentIds)]);
    }
}

public sealed class CourseCreateRequest : CourseWriteRequest
{
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext)) yield return result;
        if (StudentIds is { Count: 0 }) yield return new("Selecciona al menos un estudiante.", [nameof(StudentIds)]);
    }
}

public sealed class CourseUpdateRequest : CourseWriteRequest
{
    public Guid Id { get; set; }
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext)) yield return result;
        if (Id == Guid.Empty) yield return new("Selecciona un curso válido.", [nameof(Id)]);
    }
}
