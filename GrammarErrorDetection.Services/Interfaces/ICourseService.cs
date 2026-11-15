using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Models.Responses;

namespace GrammarErrorDetection.Services.Interfaces;

public interface ICourseService
{
    Task<ApiResponse<List<CourseSearchResponse>>?> SearchAsync(CourseSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<ClassroomLookupOption>>?> GetClassroomsAsync(int pageNumber, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<EligibleStudent>>?> GetEligibleStudentsAsync(EligibleStudentSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CourseCreateResponse>?> CreateWithStudentsAsync(CourseCreateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CourseCreateResponse>?> UpdateWithStudentsAsync(CourseUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<CourseRegistration>>?> GetRegistrationsAsync(Guid classId, int pageNumber, CancellationToken cancellationToken = default);
    Task<ApiResponse<CourseGenerationResponse>?> GenerateNextBimesterAsync(CourseGenerationRequest request, CancellationToken cancellationToken = default);
}
