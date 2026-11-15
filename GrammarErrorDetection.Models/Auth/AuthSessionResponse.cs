namespace GrammarErrorDetection.Models.Auth;

public sealed class AuthSessionResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public AuthUserResponse? User { get; set; }
}

public sealed class AuthUserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
