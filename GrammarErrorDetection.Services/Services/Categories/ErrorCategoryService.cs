using System.ComponentModel.DataAnnotations;
using System.Globalization;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Categories;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Categories;

public sealed class ErrorCategoryService(IApiClient apiClient) : IErrorCategoryService
{
    private const string Route = "api/ErrorClassification";

    public Task<ApiResponse<List<ErrorCategory>>?> SearchAsync(ErrorCategorySearchRequest request, CancellationToken cancellationToken = default)
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
            if (filter.Description is not null) query.Add($"Filter.Description={Uri.EscapeDataString(filter.Description)}");
            if (filter.Active is { } active) query.Add(active ? "Filter.Active=true" : "Filter.Active=false");
        }
        return apiClient.GetAsync<ApiResponse<List<ErrorCategory>>>($"{Route}?{string.Join('&', query)}", cancellationToken);
    }

    public Task<ApiResponse<ErrorCategory>?> CreateAsync(ErrorCategoryCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = new ErrorCategoryCreateRequest
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Active = request.Active
        };
        Validator.ValidateObject(normalized, new ValidationContext(normalized), validateAllProperties: true);
        return apiClient.PostAsync<ErrorCategoryCreateRequest, ApiResponse<ErrorCategory>>(Route, normalized, cancellationToken);
    }

    public Task<ApiResponse<ErrorCategory>?> UpdateAsync(ErrorCategoryUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id == Guid.Empty) throw new ArgumentException("Selecciona una categoría válida.", nameof(request));
        var normalized = new ErrorCategoryUpdateRequest
        {
            Id = request.Id,
            Name = request.Name?.Trim(),
            Description = request.Description?.Trim(),
            Active = request.Active
        };
        Validator.ValidateObject(normalized, new ValidationContext(normalized), validateAllProperties: true);
        return apiClient.PutAsync<ErrorCategoryUpdateRequest, ApiResponse<ErrorCategory>>(Route, normalized, cancellationToken);
    }
}
