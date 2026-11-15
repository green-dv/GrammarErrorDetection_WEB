namespace GrammarErrorDetection.Models.Courses;

public sealed class ClassroomLookupOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
}
