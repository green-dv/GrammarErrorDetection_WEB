using System.ComponentModel.DataAnnotations;
using System.Globalization;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Classrooms;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Catalogs;

public sealed class ClassroomCatalogService(IApiClient apiClient) : IClassroomCatalogService
{
    private const string Route = "api/Classroom";

    public Task<ApiResponse<List<ClassroomItem>>?> SearchAsync(ClassroomSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageSize, 1);
        var query = new List<string>
        {
            $"PageNumber={request.PageNumber.ToString(CultureInfo.InvariantCulture)}",
            $"PageSize={request.PageSize.ToString(CultureInfo.InvariantCulture)}"
        };
        if (request.Filter?.Normalize() is { } filter)
        {
            Validator.ValidateObject(filter, new ValidationContext(filter), validateAllProperties: true);
            if (filter.Name is not null) query.Add($"Filter.Name={Uri.EscapeDataString(filter.Name)}");
            if (filter.Capacity is { } capacity) query.Add($"Filter.Capacity={capacity.ToString(CultureInfo.InvariantCulture)}");
            if (filter.Active is { } active) query.Add($"Filter.Active={(active ? "true" : "false")}");
        }
        return apiClient.GetAsync<ApiResponse<List<ClassroomItem>>>($"{Route}?{string.Join('&', query)}", cancellationToken);
    }

    public Task<ApiResponse<ClassroomItem>?> CreateAsync(ClassroomCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = new ClassroomCreateRequest { Name = request.Name.Trim(), Capacity = request.Capacity, Active = request.Active };
        Validator.ValidateObject(normalized, new ValidationContext(normalized), validateAllProperties: true);
        return apiClient.PostAsync<ClassroomCreateRequest, ApiResponse<ClassroomItem>>(Route, normalized, cancellationToken);
    }

    public Task<ApiResponse<ClassroomItem>?> UpdateAsync(ClassroomUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id == Guid.Empty) throw new ArgumentException("Selecciona un aula válida.", nameof(request));
        var normalized = new ClassroomUpdateRequest { Id = request.Id, Name = request.Name?.Trim(), Capacity = request.Capacity, Active = request.Active };
        Validator.ValidateObject(normalized, new ValidationContext(normalized), validateAllProperties: true);
        return apiClient.PutAsync<ClassroomUpdateRequest, ApiResponse<ClassroomItem>>(Route, normalized, cancellationToken);
    }
}
