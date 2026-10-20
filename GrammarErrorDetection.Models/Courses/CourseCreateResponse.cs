namespace GrammarErrorDetection.Models.Courses;

public sealed class CourseCreateResponse
{
    public CourseSearchResponse? Class { get; set; }
    public List<StudentCourseAssignmentResult> Students { get; set; } = [];
    public List<Guid> RemovedRegistrationIds { get; set; } = [];
}

public sealed class StudentCourseAssignmentResult
{
    public Guid UserId { get; set; }
    public Guid RegistrationId { get; set; }
    public Guid? DeactivatedRegistrationId { get; set; }
    public string? Status { get; set; }
}
