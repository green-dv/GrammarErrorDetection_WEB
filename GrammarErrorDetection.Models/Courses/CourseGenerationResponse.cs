namespace GrammarErrorDetection.Models.Courses;

public sealed class CourseGenerationResponse
{
    public bool Generated { get; set; }
    public int SourceYear { get; set; }
    public int SourceBimester { get; set; }
    public List<Guid>? CreatedClassIds { get; set; }
    public List<Guid>? ReusedClassIds { get; set; }
    public List<Guid>? CreatedRegistrationIds { get; set; }
    public List<CourseGenerationSkipped>? Skipped { get; set; }
}

public sealed class CourseGenerationSkipped
{
    public Guid RegistrationId { get; set; }
    public Guid UserId { get; set; }
    public string? Reason { get; set; }
}
