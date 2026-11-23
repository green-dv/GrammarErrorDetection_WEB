using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Registrations;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Registrations;

public sealed class CourseInvitationService(IApiClient apiClient) : ICourseInvitationService
{
    public Task<ApiResponse<CourseInvitationLink>?> CreateAsync(Guid classId, CancellationToken cancellationToken = default)
    {
        if (classId == Guid.Empty) throw new ArgumentException(I18n.T("Selecciona un curso válido."));
        return apiClient.PostAsync<object?, ApiResponse<CourseInvitationLink>>($"api/Class/{classId:D}/invitation", null, cancellationToken);
    }

    public Task<ApiResponse<CourseJoinResult>?> PreviewAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException(I18n.T("El enlace de invitación no es válido."));
        return apiClient.PostAsync<CourseJoinRequest, ApiResponse<CourseJoinResult>>("api/Registration/join/preview", new() { Token = token.Trim() }, cancellationToken);
    }

    public Task<ApiResponse<CourseJoinResult>?> JoinAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException(I18n.T("El enlace de invitación no es válido."));
        return apiClient.PostAsync<CourseJoinRequest, ApiResponse<CourseJoinResult>>("api/Registration/join", new() { Token = token.Trim() }, cancellationToken);
    }
}
