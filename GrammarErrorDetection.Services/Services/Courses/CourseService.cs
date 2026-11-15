using System.ComponentModel.DataAnnotations;
using System.Globalization;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Courses;

public sealed class CourseService(IApiClient apiClient) : ICourseService
{
    public Task<ApiResponse<CourseGenerationResponse>?> GenerateNextBimesterAsync(CourseGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        return apiClient.PostAsync<CourseGenerationRequest, ApiResponse<CourseGenerationResponse>>("api/Registration/generate-next-bimester", request, cancellationToken);
    }

    public Task<ApiResponse<CourseCreateResponse>?> UpdateWithStudentsAsync(CourseUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        return apiClient.PutAsync<CourseUpdateRequest, ApiResponse<CourseCreateResponse>>("api/Class/with-students", request, cancellationToken);
    }

    public Task<ApiResponse<List<CourseRegistration>>?> GetRegistrationsAsync(Guid classId, int pageNumber, CancellationToken cancellationToken = default)
    {
        if (classId == Guid.Empty) throw new ArgumentException("Selecciona un curso válido.");
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        return apiClient.GetAsync<ApiResponse<List<CourseRegistration>>>($"api/Class/{classId:D}/registrations?PageNumber={pageNumber.ToString(CultureInfo.InvariantCulture)}&PageSize=100", cancellationToken);
    }

    public Task<ApiResponse<CourseCreateResponse>?> CreateWithStudentsAsync(CourseCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        return apiClient.PostAsync<CourseCreateRequest, ApiResponse<CourseCreateResponse>>("api/Class/with-students", request, cancellationToken);
    }

    public Task<ApiResponse<List<EligibleStudent>>?> GetEligibleStudentsAsync(EligibleStudentSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ModuleNameId == Guid.Empty) throw new ArgumentException("Selecciona un módulo válido.");
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageSize, 1);
        if (request.Year is < 1 or > 9999 || request.Bimester is < 1 or > 6) throw new ArgumentException("Selecciona un año y bimestre válidos.");
        var query = new List<string>
        {
            $"Filter.ModuleNameId={request.ModuleNameId:D}",
            $"Filter.Year={request.Year.ToString(CultureInfo.InvariantCulture)}",
            $"Filter.Bimester={request.Bimester.ToString(CultureInfo.InvariantCulture)}",
            $"PageNumber={request.PageNumber.ToString(CultureInfo.InvariantCulture)}",
            $"PageSize={request.PageSize.ToString(CultureInfo.InvariantCulture)}"
        };
        void Add(string key, string? value) { if (!string.IsNullOrWhiteSpace(value)) query.Add($"Filter.{key}={Uri.EscapeDataString(value.Trim())}"); }
        Add("FirstName", request.FirstName);
        Add("LastName", request.LastName);
        Add("CI", request.CI);
        if (request.IsAssigned.HasValue) query.Add($"Filter.IsAssigned={request.IsAssigned.Value.ToString().ToLowerInvariant()}");
        return apiClient.GetAsync<ApiResponse<List<EligibleStudent>>>($"api/Class/eligible-students?{string.Join("&", query)}", cancellationToken);
    }

    public Task<ApiResponse<List<ClassroomLookupOption>>?> GetClassroomsAsync(int pageNumber, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        return apiClient.GetAsync<ApiResponse<List<ClassroomLookupOption>>>($"api/Classroom?PageNumber={pageNumber.ToString(CultureInfo.InvariantCulture)}&PageSize=100", cancellationToken);
    }

    public Task<ApiResponse<List<CourseSearchResponse>>?> SearchAsync(CourseSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageSize, 1);
        var query = new List<string>();
        void Add(string key, string? value)
        {
            if (value is not null) query.Add($"{key}={Uri.EscapeDataString(value)}");
        }
        Add("PageNumber", request.PageNumber.ToString(CultureInfo.InvariantCulture));
        Add("PageSize", request.PageSize.ToString(CultureInfo.InvariantCulture));
        if (request.Filter?.Normalize() is { } filter)
        {
            Validator.ValidateObject(filter, new ValidationContext(filter), validateAllProperties: true);
            Add("Filter.Id", filter.Id);
            Add("Filter.TeacherId", filter.TeacherId);
            Add("Filter.ModuleNameId", filter.ModuleNameId);
            Add("Filter.Bimester", filter.Bimester?.ToString(CultureInfo.InvariantCulture));
            Add("Filter.Year", filter.Year?.ToString(CultureInfo.InvariantCulture));
            Add("Filter.StartDate", filter.StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Add("Filter.EndDate", filter.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Add("Filter.Parallel", filter.Parallel);
            Add("Filter.ClassroomId", filter.ClassroomId);
            Add("Filter.StartTime", filter.StartTime?.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
            Add("Filter.EndTime", filter.EndTime?.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
            Add("Filter.CreatedAt", filter.CreatedAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Add("Filter.Active", filter.Active?.ToString().ToLowerInvariant());
        }
        return apiClient.GetAsync<ApiResponse<List<CourseSearchResponse>>>($"api/Class?{string.Join("&", query)}", cancellationToken);
    }
}
