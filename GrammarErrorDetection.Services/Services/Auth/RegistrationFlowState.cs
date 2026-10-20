using GrammarErrorDetection.Models.Auth;

namespace GrammarErrorDetection.Services.Services.Auth;

public sealed class RegistrationFlowState : IDisposable
{
    private SignupRequest? pendingUser;
    private Timer? expiration;
    public AuthChallengeResponse? Challenge { get; private set; }
    public bool HasPendingSignup => pendingUser is not null && Challenge is not null && Challenge.ExpiresAt > DateTimeOffset.UtcNow;

    public void BeginVerification(AuthChallengeResponse challenge, SignupRequest user)
    {
        Clear();
        if (challenge.ExpiresAt <= DateTimeOffset.UtcNow) throw new InvalidOperationException("El código ya venció.");
        pendingUser = new SignupRequest { FirstName = user.FirstName, LastName = user.LastName, CI = user.CI, Email = user.Email, Password = user.Password, Phone = user.Phone };
        Challenge = challenge;
        expiration = new Timer(_ =>
        {
            if (ReferenceEquals(Challenge, challenge)) Clear();
        }, null, TimeSpan.FromTicks(Math.Max(0, (challenge.ExpiresAt - DateTimeOffset.UtcNow).Ticks)), Timeout.InfiniteTimeSpan);
    }

    public SignupConfirmationRequest CreateConfirmation(string code)
    {
        if (!HasPendingSignup)
        {
            Clear();
            throw new InvalidOperationException("El registro pendiente venció. Vuelve a comenzar.");
        }
        return new SignupConfirmationRequest { FirstName = pendingUser!.FirstName, LastName = pendingUser.LastName, CI = pendingUser.CI, Email = pendingUser.Email, Password = pendingUser.Password, Phone = pendingUser.Phone, ChallengeId = Challenge!.ChallengeId, Code = code };
    }

    public void Clear()
    {
        expiration?.Dispose();
        expiration = null;
        if (pendingUser is not null) pendingUser.Password = string.Empty;
        pendingUser = null;
        Challenge = null;
    }

    public void Dispose() => Clear();
}
