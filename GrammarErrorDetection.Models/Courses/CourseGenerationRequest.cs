using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Courses;

public sealed class CourseGenerationRequest : IValidatableObject
{
    [Range(2, 9999, ErrorMessage = "Indica un año entre 2 y 9999.")]
    public int Year { get; set; }
    [Range(1, 6, ErrorMessage = "Selecciona un bimestre entre 1 y 6.")]
    public int Bimester { get; set; }
    [Required(ErrorMessage = "Indica la fecha de inicio.")]
    public DateTime? StartDate { get; set; }
    [Required(ErrorMessage = "Indica la fecha de fin.")]
    public DateTime? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            yield return new("La fecha de fin debe ser igual o posterior a la de inicio.", [nameof(EndDate)]);
        if ((StartDate.HasValue && StartDate.Value.Year != Year) || (EndDate.HasValue && EndDate.Value.Year != Year))
            yield return new("Las fechas deben pertenecer al año de destino.", [nameof(StartDate), nameof(EndDate)]);
    }
}
