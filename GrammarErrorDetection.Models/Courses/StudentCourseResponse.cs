namespace GrammarErrorDetection.Models.Courses;

public sealed class StudentCourseResponse
{
    public Guid RegistrationId { get; set; }
    public Guid ClassId { get; set; }
    public Guid ModuleId { get; set; }
    public string? ModuleName { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? TeacherName { get; set; }
    public decimal? Exam1Score { get; set; }
    public decimal? Exam2Score { get; set; }
    public string? Parallel { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int Year { get; set; }
    public int Bimester { get; set; }
    public string? Status { get; set; }
}
