namespace GrammarErrorDetection.Models.Registrations;

public sealed class CourseInvitationLink
{
    public Guid ClassId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public sealed class CourseJoinRequest
{
    public string Token { get; set; } = string.Empty;
}

public sealed class CourseJoinResult
{
    public Guid RegistrationId { get; set; }
    public Guid ClassId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string? Parallel { get; set; }
    public int Year { get; set; }
    public int Bimester { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? TeacherName { get; set; }
    public string? ClassroomName { get; set; }
    public bool AlreadyMember { get; set; }
}
