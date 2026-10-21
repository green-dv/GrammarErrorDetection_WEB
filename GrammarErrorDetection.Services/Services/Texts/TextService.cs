using System.Globalization;
using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Texts;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Texts;

public sealed class TextService(IApiClient apiClient) : ITextService
{
    public Task<ApiResponse<List<TextResponse>>?> SearchAsync(TextSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageSize, 1);
        if (request.UserId == Guid.Empty) throw new ArgumentException("El usuario no es válido.");
        if (request.Id == Guid.Empty) throw new ArgumentException("El texto no es válido.");
        var query = $"api/Text?PageNumber={request.PageNumber.ToString(CultureInfo.InvariantCulture)}&PageSize={request.PageSize.ToString(CultureInfo.InvariantCulture)}&Filter.Active=true";
        if (request.UserId is { } userId) query += $"&Filter.UserId={userId:D}";
        if (request.Id is { } id) query += $"&Filter.Id={id:D}";
        if (!string.IsNullOrWhiteSpace(request.OriginalContent)) query += $"&Filter.OriginalContent={Uri.EscapeDataString(request.OriginalContent.Trim())}";
        return apiClient.GetAsync<ApiResponse<List<TextResponse>>>(query, cancellationToken);
    }

    public Task<ApiResponse<TextResponse>?> UploadAsync(TextUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.OriginalContent)) throw new ArgumentException("Escribe un párrafo antes de subirlo.");
        return apiClient.PostAsync<TextUploadRequest, ApiResponse<TextResponse>>("api/Text/upload", request, cancellationToken);
    }
}
