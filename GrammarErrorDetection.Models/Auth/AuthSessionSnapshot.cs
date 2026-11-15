namespace GrammarErrorDetection.Models.Auth;

public sealed class AuthSessionSnapshot
{
    public AuthSessionResponse? Session { get; set; }
    public bool? IsTotpEnabled { get; set; }
}
