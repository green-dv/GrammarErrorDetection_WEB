namespace GrammarErrorDetection.Models.Auth;

public sealed class TotpSetupResponse
{
    public string OtpAuthUri { get; set; } = string.Empty;
}

public sealed class TotpConfirmRequest
{
    public string Code { get; set; } = string.Empty;
}
