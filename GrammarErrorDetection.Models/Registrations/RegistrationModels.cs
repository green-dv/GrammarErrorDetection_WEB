using System.ComponentModel.DataAnnotations;

namespace GrammarErrorDetection.Models.Registrations;

// Proyección del listado; registrationId identifica la solicitud para las decisiones.
public sealed class RegistrationListItem
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Ci { get; set; }
    public string? Phone { get; set; }
    public string? ModuleName { get; set; }
    public Guid? ModuleId { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? Status { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? RegistrationId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ClassId { get; set; }
    public bool? Active { get; set; }
}

public sealed class RegistrationResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ClassId { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? Status { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Ci { get; set; }
    public string? Phone { get; set; }
    public Guid? ModuleNameId { get; set; }
    public string? ModuleName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool Active { get; set; }
}

public sealed class AvailableRegistrationCourse
{
    public Guid ClassId { get; set; }
    public Guid ModuleNameId { get; set; }
    public string? ModuleName { get; set; }
    public Guid? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public string? Parallel { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Year { get; set; }
    public int Bimester { get; set; }
    public int AvailableSeats { get; set; }
}

public sealed class AvailableRegistrationCoursesRequest
{
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, int.MaxValue)] public int PageSize { get; set; } = 20;
    [Range(1, 9999, ErrorMessage = "Selecciona un año válido.")] public int Year { get; set; }
    [Range(1, 6, ErrorMessage = "Selecciona un bimestre entre 1 y 6.")] public int Bimester { get; set; }
}

public sealed class RegistrationApplyRequest
{
    public Guid ClassId { get; set; }
}

public sealed record RegistrationDecisionRequest(bool Approved);
public sealed class RegistrationSearchRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public RegistrationFilter? Filter { get; set; }
}
public sealed class RegistrationFilter
{
    public Guid? UserId { get; set; }
    public Guid? ModuleNameId { get; set; }
    public DateOnly? RegistrationDate { get; set; }
    public string? Status { get; set; }
    public bool? Active { get; set; }
}
