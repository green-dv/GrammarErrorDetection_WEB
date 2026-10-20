namespace GrammarErrorDetection.Models.Auth;

public sealed record AuthChallengeResponse
{
    public Guid ChallengeId { get; init; }
    public bool RequiresMfa { get; init; }
    public string Method { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
}
