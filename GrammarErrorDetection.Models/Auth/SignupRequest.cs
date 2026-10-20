namespace GrammarErrorDetection.Models.Auth;

public class SignupRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CI { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public sealed class SignupConfirmationRequest : SignupRequest
{
    public Guid ChallengeId { get; set; }
    public string Code { get; set; } = string.Empty;
}
