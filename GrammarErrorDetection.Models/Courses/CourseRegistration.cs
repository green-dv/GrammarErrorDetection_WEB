namespace GrammarErrorDetection.Models.Courses;

public sealed class CourseRegistration
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ClassId { get; set; }
    public bool Active { get; set; }
}
