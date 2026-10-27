using System.Globalization;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Materials;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Materials;

public sealed class SupportMaterialService(IApiClient apiClient) : ISupportMaterialService
{
    public Task<ApiResponse<List<ErrorClassificationResponse>>?> GetClassificationsAsync(int pageNumber, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        return apiClient.GetAsync<ApiResponse<List<ErrorClassificationResponse>>>($"api/ErrorClassification?PageNumber={pageNumber.ToString(CultureInfo.InvariantCulture)}&PageSize=100&Filter.Active=true", cancellationToken);
    }

    public Task<ApiResponse<List<FileTypeResponse>>?> GetFileTypesAsync(int pageNumber, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        return apiClient.GetAsync<ApiResponse<List<FileTypeResponse>>>($"api/FileType?PageNumber={pageNumber.ToString(CultureInfo.InvariantCulture)}&PageSize=100&Filter.Active=true", cancellationToken);
    }

    public Task<ApiResponse<ErrorClassificationResponse>?> GetClassificationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateGuid(id);
        return apiClient.GetAsync<ApiResponse<ErrorClassificationResponse>>($"api/ErrorClassification/{id:D}", cancellationToken);
    }

    public Task<ApiResponse<FileTypeResponse>?> GetFileTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateGuid(id);
        return apiClient.GetAsync<ApiResponse<FileTypeResponse>>($"api/FileType/{id:D}", cancellationToken);
    }

    public Task<ApiResponse<List<SupportMaterialResponse>>?> SearchAsync(SupportMaterialSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageSize, 1);
        var query = new List<string>
        {
            $"PageNumber={request.PageNumber.ToString(CultureInfo.InvariantCulture)}",
            $"PageSize={request.PageSize.ToString(CultureInfo.InvariantCulture)}"
        };
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) query.Add($"Filter.{key}={Uri.EscapeDataString(value.Trim())}");
        }
        if (request.Filter is { } filter)
        {
            ValidateGuid(filter.Id); ValidateGuid(filter.ErrorClassificationId);
            ValidateGuid(filter.FileTypeId); ValidateGuid(filter.UploadedBy);
            if (filter.FileSize < 0) throw new ArgumentException("El tamaño no puede ser negativo.");
            Add("Id", filter.Id?.ToString("D"));
            Add("Name", filter.Name);
            Add("ErrorClassificationId", filter.ErrorClassificationId?.ToString("D"));
            Add("UploadDate", filter.UploadDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Add("FileLink", filter.FileLink);
            Add("FileTypeId", filter.FileTypeId?.ToString("D"));
            Add("FileSize", filter.FileSize?.ToString(CultureInfo.InvariantCulture));
            Add("UploadedBy", filter.UploadedBy?.ToString("D"));
            Add("Active", filter.Active?.ToString().ToLowerInvariant());
            Add("CreatedAt", filter.CreatedAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
        return apiClient.GetAsync<ApiResponse<List<SupportMaterialResponse>>>($"api/SupportMaterial?{string.Join("&", query)}", cancellationToken);
    }

    public Task<ApiResponse<SupportMaterialResponse>?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateGuid(id);
        return apiClient.GetAsync<ApiResponse<SupportMaterialResponse>>($"api/SupportMaterial/{id:D}", cancellationToken);
    }

    public Task<ApiResponse<SupportMaterialResponse>?> CreateAsync(SupportMaterialCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateGuid(request.ErrorClassificationId); ValidateGuid(request.FileTypeId); ValidateGuid(request.UploadedBy);
        ValidateName(request.Name); ValidateLink(request.FileLink);
        if (request.UploadDate == default) throw new ArgumentException("Indica la fecha de subida.");
        if (request.FileSize < 0) throw new ArgumentException("El tamaño no puede ser negativo.");
        return apiClient.PostAsync<SupportMaterialCreateRequest, ApiResponse<SupportMaterialResponse>>("api/SupportMaterial", request, cancellationToken);
    }

    public Task<ApiResponse<SupportMaterialResponse>?> UpdateAsync(SupportMaterialUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateGuid(request.Id); ValidateGuid(request.ErrorClassificationId);
        ValidateGuid(request.FileTypeId); ValidateGuid(request.UploadedBy);
        if (!request.HasChanges) throw new ArgumentException("No hay cambios para guardar.");
        if (request.Name is not null) ValidateName(request.Name);
        if (request.FileLink is not null) ValidateLink(request.FileLink);
        if (request.FileSize < 0) throw new ArgumentException("El tamaño no puede ser negativo.");
        if (request.UploadDate == default(DateTime)) throw new ArgumentException("Indica una fecha válida.");
        return apiClient.PutAsync<SupportMaterialUpdateRequest, ApiResponse<SupportMaterialResponse>>("api/SupportMaterial", request, cancellationToken);
    }

    private static void ValidateGuid(Guid? value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Selecciona un identificador válido.");
    }
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 255) throw new ArgumentException("El nombre debe tener entre 1 y 255 caracteres.");
    }
    private static void ValidateLink(string link)
    {
        if (string.IsNullOrWhiteSpace(link)) throw new ArgumentException("Ingresa el enlace del material.");
    }
}
