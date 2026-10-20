namespace GrammarErrorDetection.Models.Users;

public sealed class UserSearchResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Ci { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool HasAuthenticator { get; set; }
    public bool IsTotpEnabled { get; set; }
    public bool ChangePassword { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Active { get; set; }
    public UserCurrentCourseResponse? CurrentCourse { get; set; }
    public decimal? CurrentCourseFinalScore { get; set; }
    public UserCourseSummaryResponse? LastCourse { get; set; }
}

public class UserCourseSummaryResponse
{
    public Guid ClassId { get; set; }
    public Guid ModuleNameId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Bimester { get; set; }
    public string? Parallel { get; set; }
    public string? RegistrationStatus { get; set; }
}

public sealed class UserCurrentCourseResponse : UserCourseSummaryResponse
{
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public Guid? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public int? ClassroomCapacity { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public Guid? RegistrationId { get; set; }
    public DateTime? RegistrationDate { get; set; }
}

public sealed class UserLookupOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
}
