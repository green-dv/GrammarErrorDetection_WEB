namespace GrammarErrorDetection.Models.Auth;

public sealed class SendVerificationCodeRequest
{
    public string Email { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
