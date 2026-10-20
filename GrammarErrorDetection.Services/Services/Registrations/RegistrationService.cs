using System.ComponentModel.DataAnnotations;
using System.Globalization;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Registrations;
using GrammarErrorDetection.Models.Courses;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Registrations;

public sealed class RegistrationService(IApiClient apiClient) : IRegistrationService
{
    public Task<ApiResponse<List<StudentCourseResponse>>?> GetMyCoursesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        return apiClient.GetAsync<ApiResponse<List<StudentCourseResponse>>>($"api/Registration/mine/courses?PageNumber={Number(pageNumber)}&PageSize={Number(pageSize)}", cancellationToken);
    }
    public Task<ApiResponse<List<AvailableRegistrationCourse>>?> GetAvailableCoursesAsync(AvailableRegistrationCoursesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new(request), true);
        return apiClient.GetAsync<ApiResponse<List<AvailableRegistrationCourse>>>($"api/Registration/available-courses?PageNumber={Number(request.PageNumber)}&PageSize={Number(request.PageSize)}&Filter.Year={Number(request.Year)}&Filter.Bimester={Number(request.Bimester)}", cancellationToken);
    }
    public Task<ApiResponse<RegistrationResponse>?> ApplyAsync(RegistrationApplyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ClassId == Guid.Empty) throw new ArgumentException("Selecciona un curso válido.");
        Validator.ValidateObject(request, new(request), true);
        return apiClient.PostAsync<RegistrationApplyRequest, ApiResponse<RegistrationResponse>>("api/Registration/apply", request, cancellationToken);
    }
    public Task<ApiResponse<RegistrationResponse>?> DecideAsync(Guid id, bool approved, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Selecciona una inscripción válida.");
        return apiClient.PutAsync<RegistrationDecisionRequest, ApiResponse<RegistrationResponse>>($"api/Registration/{id:D}/decision", new(approved), cancellationToken);
    }
    public Task<ApiResponse<List<RegistrationListItem>>?> GetMineAsync(RegistrationSearchRequest request, CancellationToken cancellationToken = default)
        => ListAsync(request, true, cancellationToken);
    public Task<ApiResponse<List<RegistrationListItem>>?> SearchAsync(RegistrationSearchRequest request, CancellationToken cancellationToken = default)
        => ListAsync(request, false, cancellationToken);
    private Task<ApiResponse<List<RegistrationListItem>>?> ListAsync(RegistrationSearchRequest request, bool mine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageSize, 1);
        var query = new List<string> { $"PageNumber={Number(request.PageNumber)}", $"PageSize={Number(request.PageSize)}" };
        void Add(string name, string? value) { if (!string.IsNullOrWhiteSpace(value)) query.Add($"Filter.{name}={Uri.EscapeDataString(value.Trim())}"); }
        if (request.Filter is { } filter)
        {
            if (!mine) Add("UserId", filter.UserId?.ToString("D"));
            Add("ModuleNameId", filter.ModuleNameId?.ToString("D"));
            Add("RegistrationDate", filter.RegistrationDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Add("Status", filter.Status);
            if (mine) Add("Active", filter.Active?.ToString().ToLowerInvariant());
        }
        return apiClient.GetAsync<ApiResponse<List<RegistrationListItem>>>($"api/Registration{(mine ? "/mine" : "")}?{string.Join("&", query)}", cancellationToken);
    }
    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
