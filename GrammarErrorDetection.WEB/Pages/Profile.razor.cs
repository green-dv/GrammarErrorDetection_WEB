using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;

namespace GrammarErrorDetection.WEB.Pages;

public partial class Profile
{
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "tab")] public string? Tab { get; set; }

    protected override void OnParametersSet()
    {
        if (Session.Current is null) Navigation.NavigateTo("/auth/login", replace: true);
        else if (Tab == "totp") Navigation.NavigateTo("/profile?tab=settings", replace: true);
    }
}
