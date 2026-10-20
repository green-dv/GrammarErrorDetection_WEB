namespace GrammarErrorDetection.Models.Courses;

public sealed class CourseSearchResponse
{
    public Guid Id { get; set; }
    public Guid? TeacherId { get; set; }
    public Guid ModuleNameId { get; set; }
    public int Bimester { get; set; }
    public int Year { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? Parallel { get; set; }
    public Guid? ClassroomId { get; set; }
    // El contrato admite la terminación Z; conservar la hora sin convertir zonas horarias.
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool Active { get; set; }
    public string? TeacherName { get; set; }
    public string? ModuleName { get; set; }
    public int? ModuleLevel { get; set; }
    public string? ClassroomName { get; set; }
    public int? ClassroomCapacity { get; set; }
    public long? StudentCount { get; set; }
    public decimal? Exam1Average { get; set; }
    public decimal? Exam2Average { get; set; }
}
