using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Gamification;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Gamification;

public sealed class GamificationService(IApiClient apiClient) : IGamificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ApiResponse<List<UserAchievementBestResponse>>?> SearchBestUserAchievementsAsync(UserAchievementBestSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.UserId is { } userId) ValidateId(userId);
        if (request.AchievementCollectionId is { } collectionId) ValidateId(collectionId);
        if (request.AchievementCategoryId is { } categoryId) ValidateId(categoryId);
        var pagination = new GamificationSearchRequest { PageNumber = request.PageNumber, PageSize = request.PageSize };
        var uri = BuildQuery("api/UserAchievement/best", pagination,
            ("AchievementCollectionId", request.AchievementCollectionId?.ToString("D")),
            ("AchievementCategoryId", request.AchievementCategoryId?.ToString("D")));
        if (request.UserId is { } targetUserId) uri += $"&UserId={targetUserId:D}";
        return apiClient.GetAsync<ApiResponse<List<UserAchievementBestResponse>>>(uri, cancellationToken);
    }

    public Task<ApiResponse<AchievementCollectionResponse>?> CreateAchievementCollectionAsync(AchievementCollectionCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        return apiClient.PostAsync<AchievementCollectionCreateRequest, ApiResponse<AchievementCollectionResponse>>("api/AchievementCollection", request, cancellationToken);
    }

    public Task<ApiResponse<AchievementCollectionResponse>?> UpdateAchievementCollectionAsync(AchievementCollectionUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(request.Id);
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        return apiClient.PutAsync<AchievementCollectionUpdateRequest, ApiResponse<AchievementCollectionResponse>>("api/AchievementCollection", request, cancellationToken);
    }

    public Task<ApiResponse<List<AchievementCollectionResponse>>?> SearchAchievementCollectionsAsync(AchievementCollectionSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id is { } id) ValidateId(id);
        if (request.CreatedByUserId is { } creatorId) ValidateId(creatorId);
        return apiClient.GetAsync<ApiResponse<List<AchievementCollectionResponse>>>(BuildQuery("api/AchievementCollection", request,
            ("Id", request.Id?.ToString("D")), ("Name", request.Name),
            ("CreatedByUserId", request.CreatedByUserId?.ToString("D"))), cancellationToken);
    }

    public Task<ApiResponse<List<AchievementResponse>>?> SearchAchievementsAsync(AchievementSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id is { } achievementId) ValidateId(achievementId);
        if (request.AchievementCollectionId is { } collectionId) ValidateId(collectionId);
        return apiClient.GetAsync<ApiResponse<List<AchievementResponse>>>(BuildQuery("api/Achievement", request, ("Id", request.Id?.ToString("D")), ("Name", request.Name),
            ("AchievementCollectionId", request.AchievementCollectionId?.ToString("D"))), cancellationToken);
    }

    public async Task<ApiResponse<AchievementResponse>?> CreateAchievementAsync(AchievementCreateRequest request, Stream? image = null, string? fileName = null, string? contentType = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var form = CreateForm(request, image, fileName, contentType);
        return await apiClient.PostContentAsync<ApiResponse<AchievementResponse>>("api/Achievement", form, cancellationToken);
    }

    public async Task<ApiResponse<AchievementResponse>?> UpdateAchievementAsync(AchievementUpdateRequest request, Stream? image = null, string? fileName = null, string? contentType = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(request.Id);
        using var form = CreateForm(request, image, fileName, contentType);
        return await apiClient.PutContentAsync<ApiResponse<AchievementResponse>>("api/Achievement", form, cancellationToken);
    }

    public Task<ApiResponse<List<ChallengeResponse>>?> SearchChallengesAsync(ChallengeSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return apiClient.GetAsync<ApiResponse<List<ChallengeResponse>>>(BuildQuery("api/Challenge", request, ("Status", request.Status)), cancellationToken);
    }

    public Task<ApiResponse<ChallengeResponse>?> CreateChallengeAsync(ChallengeCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return apiClient.PostAsync<ChallengeCreateRequest, ApiResponse<ChallengeResponse>>("api/Challenge", request, cancellationToken);
    }

    public Task<ApiResponse<ChallengeResponse>?> UpdateChallengeAsync(ChallengeUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(request.Id);
        return apiClient.PutAsync<ChallengeUpdateRequest, ApiResponse<ChallengeResponse>>("api/Challenge", request, cancellationToken);
    }

    public Task<ApiResponse<ChallengeResponse>?> RepeatChallengeAsync(Guid id, ChallengeRepeatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(id);
        if (request.EndDate < request.StartDate) throw new ArgumentException("La fecha de fin debe ser posterior al inicio.");
        return apiClient.PostAsync<ChallengeRepeatRequest, ApiResponse<ChallengeResponse>>($"api/Challenge/{id:D}/repeat", request, cancellationToken);
    }

    public Task<ApiResponse<ChallengeResponse>?> DuplicateChallengeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        return apiClient.PostAsync<object, ApiResponse<ChallengeResponse>>($"api/Challenge/{id:D}/duplicate", new { }, cancellationToken);
    }

    public Task<ApiResponse<List<AchievementCategoryResponse>>?> SearchAchievementCategoriesAsync(AchievementCategorySearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return apiClient.GetAsync<ApiResponse<List<AchievementCategoryResponse>>>(BuildQuery("api/AchievementCategory", request), cancellationToken);
    }

    public Task<ApiResponse<AchievementCategoryResponse>?> CreateAchievementCategoryAsync(AchievementCategoryCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return apiClient.PostAsync<AchievementCategoryCreateRequest, ApiResponse<AchievementCategoryResponse>>("api/AchievementCategory", request, cancellationToken);
    }

    public Task<ApiResponse<AchievementCategoryResponse>?> UpdateAchievementCategoryAsync(AchievementCategoryUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(request.Id);
        return apiClient.PutAsync<AchievementCategoryUpdateRequest, ApiResponse<AchievementCategoryResponse>>("api/AchievementCategory", request, cancellationToken);
    }

    public Task<ApiResponse<List<ConditionTypeResponse>>?> SearchConditionTypesAsync(ConditionTypeSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return apiClient.GetAsync<ApiResponse<List<ConditionTypeResponse>>>(BuildQuery("api/ConditionType", request, ("Code", request.Code)), cancellationToken);
    }

    public Task<ApiResponse<ConditionTypeResponse>?> CreateConditionTypeAsync(ConditionTypeCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return apiClient.PostAsync<ConditionTypeCreateRequest, ApiResponse<ConditionTypeResponse>>("api/ConditionType", request, cancellationToken);
    }

    public Task<ApiResponse<ConditionTypeResponse>?> UpdateConditionTypeAsync(ConditionTypeUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(request.Id);
        return apiClient.PutAsync<ConditionTypeUpdateRequest, ApiResponse<ConditionTypeResponse>>("api/ConditionType", request, cancellationToken);
    }

    public async Task<ApiResponse<AchievementIconResponse>?> UploadAchievementIconAsync(Stream image, string fileName, string? contentType = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        using var form = CreateForm<object>(null, image, fileName, contentType);
        return await apiClient.PostContentAsync<ApiResponse<AchievementIconResponse>>("api/Achievement/icon", form, cancellationToken);
    }

    private static void ValidateId(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Selecciona un identificador válido.", nameof(id));
    }

    private static string BuildQuery(string endpoint, GamificationSearchRequest request, params (string Key, string? Value)[] filters)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageSize, 1);
        var query = new List<string>
        {
            $"PageNumber={request.PageNumber.ToString(CultureInfo.InvariantCulture)}",
            $"PageSize={request.PageSize.ToString(CultureInfo.InvariantCulture)}"
        };
        if (request.Active.HasValue)
            query.Add($"Filter.Active={request.Active.Value.ToString().ToLowerInvariant()}");
        foreach (var (key, value) in filters)
        {
            if (!string.IsNullOrWhiteSpace(value))
                query.Add($"Filter.{key}={Uri.EscapeDataString(value.Trim())}");
        }
        return $"{endpoint}?{string.Join("&", query)}";
    }

    // Disposing the form also disposes the supplied image stream, as in OcrService.
    private static MultipartFormDataContent CreateForm<T>(T? data, Stream? image, string? fileName, string? contentType)
    {
        if (image is not null && string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Indica el nombre del archivo de imagen.", nameof(fileName));

        var mediaType = image is null ? null : MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        var form = new MultipartFormDataContent();
        try
        {
            if (data is not null)
                form.Add(new StringContent(JsonSerializer.Serialize(data, JsonOptions), Encoding.UTF8), "data");
            if (image is not null)
            {
                var content = new StreamContent(image);
                content.Headers.ContentType = mediaType;
                form.Add(content, "file", Path.GetFileName(fileName!));
            }
            return form;
        }
        catch
        {
            form.Dispose();
            throw;
        }
    }
}

