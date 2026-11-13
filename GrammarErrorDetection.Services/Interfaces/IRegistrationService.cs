using GrammarErrorDetection.Models.Registrations;
using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Models.Responses;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IRegistrationService
{
    Task<ApiResponse<List<StudentCourseResponse>>?> GetMyCoursesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AvailableRegistrationCourse>>?> GetAvailableCoursesAsync(AvailableRegistrationCoursesRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<RegistrationResponse>?> ApplyAsync(RegistrationApplyRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<RegistrationListItem>>?> GetMineAsync(RegistrationSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<RegistrationListItem>>?> SearchAsync(RegistrationSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<RegistrationResponse>?> DecideAsync(Guid id, bool approved, CancellationToken cancellationToken = default);
}
