using GrammarErrorDetection.Models.Reports;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IGrammarReportService
{
    Task<GrammarReportData> LoadAsync(GrammarReportScope scope, CancellationToken cancellationToken = default);
    Task<GrammarReportFile> ExportAsync(GrammarReportScope scope, CancellationToken cancellationToken = default);
    Task<GrammarReportFile> ExportAffectedAsync(GrammarReportScope scope, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentProgressRow>> GetStudentProgressAsync(GrammarReportScope scope, CancellationToken cancellationToken = default);
}
