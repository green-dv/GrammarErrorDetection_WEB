namespace GrammarErrorDetection.Models.Auth;

public sealed class AuthSessionResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public AuthUserResponse? User { get; set; }
    /// <summary>La cuenta debe cambiar su contraseña: no hay JWT, solo <see cref="PasswordChange"/>.</summary>
    public bool RequiresPasswordChange { get; set; }
    public PasswordChangeTicket? PasswordChange { get; set; }
}

public sealed class AuthUserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
