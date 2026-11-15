using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Auth;

public sealed class LoginFlowService(IAuthService authService, LoginFlowState flow)
{
    public async Task SendEmailCodeAsync(CancellationToken cancellationToken = default)
    {
        var request = flow.CreateSendCodeRequest();
        try
        {
            var response = await authService.SendCodeAsync(request, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (response?.Error is { ErrorCode: not 0 } || response?.Data is null)
                throw new InvalidOperationException("No se pudo enviar el código. Inténtalo de nuevo.");
            flow.BeginEmailVerification(response.Data);
        }
        finally
        {
            request.Password = string.Empty;
        }
    }
}
