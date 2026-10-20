using GrammarErrorDetection.Infraestructure.Interfaces;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Auth;

public sealed class AuthService(IApiClient apiClient) : IAuthService
{
    public Task<ApiResponse<LoginOptionsResponse>?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostAsync<LoginRequest, ApiResponse<LoginOptionsResponse>>("api/Auth/Login", request, cancellationToken);

    public Task<ApiResponse<AuthChallengeResponse>?> SendCodeAsync(SendVerificationCodeRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostAsync<SendVerificationCodeRequest, ApiResponse<AuthChallengeResponse>>("api/Auth/SendCode", request, cancellationToken);

    public Task<ApiResponse<AuthSessionResponse>?> SignupAsync(SignupConfirmationRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostAsync<SignupConfirmationRequest, ApiResponse<AuthSessionResponse>>("api/Auth/Signup", request, cancellationToken);

    public Task<ApiResponse<AuthSessionResponse>?> VerifyLoginAsync(VerifyLoginRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostAsync<VerifyLoginRequest, ApiResponse<AuthSessionResponse>>("api/Auth/Login/Verify", request, cancellationToken);

    public Task<ApiResponse<AuthSessionResponse>?> VerifyTotpAsync(VerifyTotpLoginRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostAsync<VerifyTotpLoginRequest, ApiResponse<AuthSessionResponse>>("api/Auth/Login/VerifyTotp", request, cancellationToken);

    public Task<ApiResponse<TotpSetupResponse>?> SetupTotpAsync(CancellationToken cancellationToken = default)
        => apiClient.PostAsync<object?, ApiResponse<TotpSetupResponse>>("api/Auth/Totp/Setup", null, cancellationToken);

    public Task<ApiResponse<bool>?> ConfirmTotpAsync(TotpConfirmRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostAsync<TotpConfirmRequest, ApiResponse<bool>>("api/Auth/Totp/Confirm", request, cancellationToken);
}
