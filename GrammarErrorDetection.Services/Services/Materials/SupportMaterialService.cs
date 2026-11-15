using System.Globalization;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Materials;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Materials;

public sealed class SupportMaterialService(IApiClient apiClient) : ISupportMaterialService
{
    public async Task<ApiResponse<MaterialFileUploadResponse>?> UploadFileAsync(Stream file, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Selecciona un archivo.");
        using var form = new MultipartFormDataContent();
        var content = new StreamContent(file);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(content, "file", Path.GetFileName(fileName.Replace('\\', '/')));
        return await apiClient.PostContentAsync<ApiResponse<MaterialFileUploadResponse>>("api/SupportMaterial/file", form, cancellationToken);
    }
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
            if (filter.OnlyHelpful == true) Add("OnlyHelpful", "true");
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

    public Task<ApiResponse<List<SupportMaterialRecommendationResponse>>?> GetRecommendationsAsync(Guid? userId, Guid? classId,
        int categoryLimit = 3, int materialsPerCategory = 3, CancellationToken cancellationToken = default)
    {
        ValidateGuid(userId); ValidateGuid(classId);
        ArgumentOutOfRangeException.ThrowIfLessThan(categoryLimit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(categoryLimit, 50);
        ArgumentOutOfRangeException.ThrowIfLessThan(materialsPerCategory, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(materialsPerCategory, 10);
        var query = new List<string>
        {
            $"CategoryLimit={categoryLimit.ToString(CultureInfo.InvariantCulture)}",
            $"MaterialsPerCategory={materialsPerCategory.ToString(CultureInfo.InvariantCulture)}"
        };
        if (userId.HasValue) query.Add($"UserId={userId.Value:D}");
        if (classId.HasValue) query.Add($"ClassId={classId.Value:D}");
        return apiClient.GetAsync<ApiResponse<List<SupportMaterialRecommendationResponse>>>(
            $"api/SupportMaterial/recommendations?{string.Join("&", query)}", cancellationToken);
    }

    public Task<ApiResponse<SupportMaterialInteractionResponse>?> RegisterViewAsync(Guid materialId, bool downloaded,
        CancellationToken cancellationToken = default)
    {
        ValidateGuid(materialId);
        return apiClient.PostAsync<SupportMaterialViewRequest, ApiResponse<SupportMaterialInteractionResponse>>(
            $"api/SupportMaterial/{materialId:D}/view", new SupportMaterialViewRequest { Downloaded = downloaded }, cancellationToken);
    }

    public Task<ApiResponse<SupportMaterialInteractionResponse>?> SetFeedbackAsync(Guid materialId, bool helpful, string? comment,
        CancellationToken cancellationToken = default)
    {
        ValidateGuid(materialId);
        var text = helpful || string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (text?.Length > 500) throw new ArgumentException("El comentario admite hasta 500 caracteres.");
        return apiClient.PutAsync<SupportMaterialFeedbackRequest, ApiResponse<SupportMaterialInteractionResponse>>(
            $"api/SupportMaterial/{materialId:D}/feedback", new SupportMaterialFeedbackRequest { Helpful = helpful, Comment = text },
            cancellationToken);
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
