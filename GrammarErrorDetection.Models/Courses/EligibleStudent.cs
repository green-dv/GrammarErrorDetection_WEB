namespace GrammarErrorDetection.Models.Courses;

public sealed class EligibleStudent
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CI { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string EligibilityReason { get; set; } = string.Empty;
    public decimal? LastFinalScore { get; set; }
    public bool IsAssigned { get; set; }
    public string? RegistrationStatus { get; set; }
    public List<StudentCourseAssignment> Assignments { get; set; } = [];
    public bool RequiresTransfer => IsAssigned || string.Equals(EligibilityReason, "AlreadyAssigned", StringComparison.OrdinalIgnoreCase);
}

public sealed class StudentCourseAssignment
{
    public Guid RegistrationId { get; set; }
    public Guid ClassId { get; set; }
    public Guid ModuleNameId { get; set; }
    public string? ModuleName { get; set; }
    public string? Parallel { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? RegistrationStatus { get; set; }
}

public sealed class EligibleStudentSearchRequest
{
    public bool? IsAssigned { get; set; }
    public Guid ModuleNameId { get; set; }
    public int Year { get; set; }
    public int Bimester { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CI { get; set; }
}
