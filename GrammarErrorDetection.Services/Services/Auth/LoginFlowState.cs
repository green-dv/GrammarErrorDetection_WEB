using GrammarErrorDetection.Models.Auth;

namespace GrammarErrorDetection.Services.Services.Auth;

public sealed class LoginFlowState : IDisposable
{
    private string? password;
    private Timer? expiration;
    public string? PendingEmail { get; private set; }
    public bool IsTotpEnabled { get; private set; }
    public bool IsTotpSelected { get; private set; }
    public AuthChallengeResponse? Challenge { get; private set; }
    public bool HasPendingVerification => IsTotpSelected ? PendingEmail is not null && password is not null : Challenge is not null && Challenge.ExpiresAt > DateTimeOffset.UtcNow;

    public void BeginVerification(string email, bool isTotpEnabled, string loginPassword)
    {
        Clear();
        PendingEmail = email;
        IsTotpEnabled = isTotpEnabled;
        password = loginPassword;
        ResetExpiration(TimeSpan.FromMinutes(10));
    }

    public SendVerificationCodeRequest CreateSendCodeRequest()
    {
        if (PendingEmail is null || password is null) throw new InvalidOperationException("Vuelve a iniciar sesión para solicitar el código.");
        return new SendVerificationCodeRequest { Email = PendingEmail, Password = password, Purpose = "login" };
    }

    public void BeginEmailVerification(AuthChallengeResponse challenge)
    {
        if (challenge.ChallengeId == Guid.Empty || challenge.ExpiresAt <= DateTimeOffset.UtcNow || !string.Equals(challenge.Email, PendingEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La API no devolvió un código de verificación válido. Inténtalo de nuevo.");
        Challenge = challenge;
        IsTotpSelected = false;
        password = null;
        ResetExpiration(challenge.ExpiresAt - DateTimeOffset.UtcNow);
    }

    public void SelectTotp()
    {
        if (!IsTotpEnabled || PendingEmail is null || password is null) throw new InvalidOperationException("Vuelve a iniciar sesión para usar el autenticador.");
        IsTotpSelected = true;
    }

    public VerifyLoginRequest CreateEmailVerificationRequest(string code)
    {
        if (!HasPendingVerification || IsTotpSelected) throw new InvalidOperationException("El código venció. Vuelve a iniciar sesión.");
        return new VerifyLoginRequest { ChallengeId = Challenge!.ChallengeId, Code = code };
    }

    public VerifyTotpLoginRequest CreateTotpVerificationRequest(string code)
    {
        if (!HasPendingVerification || !IsTotpSelected) throw new InvalidOperationException("Vuelve a iniciar sesión para verificar el código.");
        return new VerifyTotpLoginRequest { Email = PendingEmail!, Password = password!, Code = code };
    }

    private void ResetExpiration(TimeSpan duration)
    {
        expiration?.Dispose();
        expiration = new Timer(_ => Clear(), null, duration > TimeSpan.Zero ? duration : TimeSpan.Zero, Timeout.InfiniteTimeSpan);
    }

    public void Clear()
    {
        expiration?.Dispose();
        expiration = null;
        password = null;
        PendingEmail = null;
        IsTotpEnabled = false;
        IsTotpSelected = false;
        Challenge = null;
    }

    public void Dispose() => Clear();
}
