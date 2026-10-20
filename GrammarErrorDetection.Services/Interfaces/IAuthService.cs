using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Models.Responses;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginOptionsResponse>?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthChallengeResponse>?> SendCodeAsync(SendVerificationCodeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthSessionResponse>?> SignupAsync(SignupConfirmationRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthSessionResponse>?> VerifyLoginAsync(VerifyLoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthSessionResponse>?> VerifyTotpAsync(VerifyTotpLoginRequest request, CancellationToken cancellationToken = default);
}
