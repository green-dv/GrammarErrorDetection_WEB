using System.ComponentModel.DataAnnotations;
using System.Globalization;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Modules;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Catalogs;

public sealed class ModuleCatalogService(IApiClient apiClient) : IModuleCatalogService
{
    private const string Route = "api/ModuleName";

    public Task<ApiResponse<List<ModuleItem>>?> SearchAsync(ModuleSearchRequest request, CancellationToken cancellationToken = default)
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
            if (filter.Level is { } level) query.Add($"Filter.Level={level.ToString(CultureInfo.InvariantCulture)}");
            if (filter.Active is { } active) query.Add(active ? "Filter.Active=true" : "Filter.Active=false");
        }
        return apiClient.GetAsync<ApiResponse<List<ModuleItem>>>($"{Route}?{string.Join('&', query)}", cancellationToken);
    }

    public Task<ApiResponse<ModuleItem>?> CreateAsync(ModuleCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = new ModuleCreateRequest
        {
            Name = request.Name.Trim(), Level = request.Level, NextModule = request.NextModule, Active = request.Active
        };
        Validator.ValidateObject(normalized, new ValidationContext(normalized), validateAllProperties: true);
        return apiClient.PostAsync<ModuleCreateRequest, ApiResponse<ModuleItem>>(Route, normalized, cancellationToken);
    }

    public Task<ApiResponse<ModuleItem>?> UpdateAsync(ModuleUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id == Guid.Empty) throw new ArgumentException("Selecciona un módulo válido.", nameof(request));
        if (request.NextModule == request.Id) throw new ArgumentException("El siguiente módulo debe ser diferente al actual.", nameof(request));
        if (request.ClearNextModule && request.NextModule is not null)
            throw new ArgumentException("No se puede asignar y quitar el siguiente módulo a la vez.", nameof(request));
        var normalized = new ModuleUpdateRequest
        {
            Id = request.Id, Name = request.Name?.Trim(), Level = request.Level, NextModule = request.NextModule,
            ClearNextModule = request.ClearNextModule, Active = request.Active
        };
        Validator.ValidateObject(normalized, new ValidationContext(normalized), validateAllProperties: true);
        return apiClient.PutAsync<ModuleUpdateRequest, ApiResponse<ModuleItem>>(Route, normalized, cancellationToken);
    }
}
