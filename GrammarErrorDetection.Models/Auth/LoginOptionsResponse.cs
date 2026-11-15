namespace GrammarErrorDetection.Models.Auth;

public sealed class LoginOptionsResponse
{
    public string Email { get; set; } = string.Empty;
    public bool IsTotpEnabled { get; set; }
}
