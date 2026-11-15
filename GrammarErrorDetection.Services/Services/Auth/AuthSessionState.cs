using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services.Auth;

public sealed class AuthSessionState(IAuthSessionStorage? storage = null)
{
    private AuthSessionResponse? current;
    public event Action? Changed;
    public bool? IsTotpEnabled { get; private set; }
    public IReadOnlyList<string> Roles => Current is { } session ? JwtRoleReader.ReadRoles(session.AccessToken) : [];
    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.Ordinal);

    public void Restore()
    {
        var snapshot = storage?.Read();
        if (snapshot?.Session is not { } session)
        {
            Clear();
            return;
        }

        try { SignIn(session, session.User?.Email ?? string.Empty, snapshot.IsTotpEnabled); }
        catch (InvalidOperationException) { Clear(); }
    }

    public AuthSessionResponse? Current
    {
        get
        {
            if (current?.ExpiresAt <= DateTimeOffset.UtcNow) Clear();
            return current;
        }
    }

    public void SignIn(AuthSessionResponse response, string expectedEmail, bool? isTotpEnabled = null)
    {
        if (string.IsNullOrWhiteSpace(response.AccessToken) || !string.Equals(response.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase) || response.ExpiresAt <= DateTimeOffset.UtcNow || response.User is null || response.User.Id == Guid.Empty || !string.Equals(response.User.Email, expectedEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La API no devolvió una sesión válida. Vuelve a iniciar sesión.");
        current = response;
        IsTotpEnabled = isTotpEnabled;
        Persist();
        Changed?.Invoke();
    }

    public void MarkTotpEnabled()
    {
        if (Current is null) return;
        IsTotpEnabled = true;
        Persist();
        Changed?.Invoke();
    }

    public void Clear()
    {
        var hadSession = current is not null;
        current = null;
        IsTotpEnabled = null;
        storage?.Remove();
        if (hadSession) Changed?.Invoke();
    }

    private void Persist() => storage?.Write(new AuthSessionSnapshot { Session = current, IsTotpEnabled = IsTotpEnabled });
}
