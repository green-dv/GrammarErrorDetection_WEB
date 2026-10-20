using System.Net.Http;
using System.Text.Json;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;

namespace GrammarErrorDetection.WEB.Views.Auth;

public partial class SelectVerificationMethod : IDisposable
{
    [Inject] private LoginFlowState Flow { get; set; } = default!;
    [Inject] private LoginFlowService LoginFlow { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private string? status;
    private bool busy;

    protected override async Task OnInitializedAsync()
    {
        if (Flow.PendingEmail is null) Navigation.NavigateTo("/auth/login", replace: true);
        else if (Flow.HasPendingVerification) Navigation.NavigateTo("/auth/verify", replace: true);
        else if (!Flow.IsTotpEnabled) await SendEmailAsync();
    }

    private async Task SendEmailAsync()
    {
        if (busy) return;
        busy = true;
        status = null;
        try
        {
            await LoginFlow.SendEmailCodeAsync(lifetime.Token);
            Navigation.NavigateTo("/auth/verify");
        }
        catch (HttpRequestException)
        {
            status = "No se pudo enviar el código. Espera un momento e inténtalo de nuevo.";
        }
        catch (JsonException)
        {
            status = "La API no devolvió una respuesta válida para el envío del código.";
        }
        catch (InvalidOperationException ex)
        {
            status = ex.Message;
        }
        catch (OperationCanceledException)
        {
            if (!lifetime.IsCancellationRequested) status = "La solicitud tardó demasiado. Inténtalo de nuevo.";
        }
        finally
        {
            busy = false;
        }
    }

    private void SelectTotp()
    {
        if (busy) return;
        try
        {
            Flow.SelectTotp();
            Navigation.NavigateTo("/auth/verify");
        }
        catch (InvalidOperationException ex)
        {
            status = ex.Message;
        }
    }

    private void Cancel()
    {
        Flow.Clear();
        Navigation.NavigateTo("/auth/login");
    }

    public void Dispose()
    {
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
