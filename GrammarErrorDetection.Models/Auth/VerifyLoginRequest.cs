namespace GrammarErrorDetection.Models.Auth;

public sealed class VerifyLoginRequest
{
    public Guid ChallengeId { get; set; }
    public string Code { get; set; } = string.Empty;
}

public sealed class VerifyTotpLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
