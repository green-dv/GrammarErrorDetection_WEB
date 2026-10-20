using GrammarErrorDetection.Models.Auth;

namespace GrammarErrorDetection.Services.Services.Auth;

public sealed class AuthSessionState
{
    private AuthSessionResponse? current;
    public IReadOnlyList<string> Roles => Current is { } session ? JwtRoleReader.ReadRoles(session.AccessToken) : [];

    public AuthSessionResponse? Current
    {
        get
        {
            if (current?.ExpiresAt <= DateTimeOffset.UtcNow) Clear();
            return current;
        }
    }

    public void SignIn(AuthSessionResponse response, string expectedEmail)
    {
        if (string.IsNullOrWhiteSpace(response.AccessToken) || !string.Equals(response.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase) || response.ExpiresAt <= DateTimeOffset.UtcNow || response.User is null || response.User.Id == Guid.Empty || !string.Equals(response.User.Email, expectedEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La API no devolvió una sesión válida. Vuelve a iniciar sesión.");
        current = response;
    }

    public void Clear() => current = null;
}
