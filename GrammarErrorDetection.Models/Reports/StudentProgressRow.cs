namespace GrammarErrorDetection.Models.Reports;

public sealed record StudentProgressRow(
    Guid UserId, string? StudentName, Guid ClassId, Guid RegistrationId,
    Guid ModuleNameId, string ModuleName, int Level, int Year, int Bimester,
    string? Parallel, decimal? Exam1Score, decimal? Exam2Score, decimal? FinalScore);
