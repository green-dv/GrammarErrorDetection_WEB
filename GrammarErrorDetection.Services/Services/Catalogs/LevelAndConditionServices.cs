using System.ComponentModel.DataAnnotations;
using System.Globalization;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.ConditionTypes;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.UserLevels;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Catalogs;

public sealed class UserLevelCatalogService(IApiClient apiClient) : IUserLevelCatalogService
{
    private const string Route = "api/UserLevel";

    public Task<ApiResponse<List<UserLevelItem>>?> SearchAsync(UserLevelSearchRequest request, CancellationToken cancellationToken = default)
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
            if (filter.Active is { } active) query.Add(active ? "Filter.Active=true" : "Filter.Active=false");
        }
        return apiClient.GetAsync<ApiResponse<List<UserLevelItem>>>($"{Route}?{string.Join('&', query)}", cancellationToken);
    }

    public Task<ApiResponse<UserLevelItem>?> CreateAsync(UserLevelCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = new UserLevelCreateRequest { Level = request.Level, Name = request.Name.Trim(), MinPoints = request.MinPoints, Active = request.Active };
        Validator.ValidateObject(normalized, new ValidationContext(normalized), validateAllProperties: true);
        return apiClient.PostAsync<UserLevelCreateRequest, ApiResponse<UserLevelItem>>(Route, normalized, cancellationToken);
    }

    public Task<ApiResponse<UserLevelItem>?> UpdateAsync(UserLevelUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id == Guid.Empty) throw new ArgumentException("Selecciona un nivel válido.", nameof(request));
        var normalized = new UserLevelUpdateRequest
        {
            Id = request.Id, Level = request.Level, Name = request.Name?.Trim(), MinPoints = request.MinPoints, Active = request.Active
        };
        Validator.ValidateObject(normalized, new ValidationContext(normalized), validateAllProperties: true);
        return apiClient.PutAsync<UserLevelUpdateRequest, ApiResponse<UserLevelItem>>(Route, normalized, cancellationToken);
    }
}

public sealed class ConditionTypeCatalogService(IApiClient apiClient) : IConditionTypeCatalogService
{
    private const string Route = "api/ConditionType";

    public Task<ApiResponse<List<ConditionTypeItem>>?> SearchAsync(ConditionTypeSearchRequest request, CancellationToken cancellationToken = default)
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
            if (filter.Code is not null) query.Add($"Filter.Code={Uri.EscapeDataString(filter.Code)}");
            if (filter.Name is not null) query.Add($"Filter.Name={Uri.EscapeDataString(filter.Name)}");
            if (filter.Active is { } active) query.Add(active ? "Filter.Active=true" : "Filter.Active=false");
        }
        return apiClient.GetAsync<ApiResponse<List<ConditionTypeItem>>>($"{Route}?{string.Join('&', query)}", cancellationToken);
    }

    public Task<ApiResponse<ConditionTypeItem>?> CreateAsync(ConditionTypeCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = new ConditionTypeCreateRequest
        {
            Code = request.Code.Trim(), Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ValueType = request.ValueType, Active = request.Active
        };
        Validator.ValidateObject(normalized, new ValidationContext(normalized), validateAllProperties: true);
        return apiClient.PostAsync<ConditionTypeCreateRequest, ApiResponse<ConditionTypeItem>>(Route, normalized, cancellationToken);
    }

    public Task<ApiResponse<ConditionTypeItem>?> UpdateAsync(ConditionTypeUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id == Guid.Empty) throw new ArgumentException("Selecciona un tipo de condición válido.", nameof(request));
        var normalized = new ConditionTypeUpdateRequest
        {
            Id = request.Id, Name = request.Name?.Trim(), Description = request.Description?.Trim(), Active = request.Active
        };
        Validator.ValidateObject(normalized, new ValidationContext(normalized), validateAllProperties: true);
        return apiClient.PutAsync<ConditionTypeUpdateRequest, ApiResponse<ConditionTypeItem>>(Route, normalized, cancellationToken);
    }
}
