using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.WEB.Views.Auth.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Views.Auth;

public partial class LoginVerification : IDisposable
{
    [Inject] private LoginFlowState Flow { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly VerificationCodeModel model = new();
    private readonly CancellationTokenSource lifetime = new();
    private EditContext editContext = default!;
    private List<string> validationErrors = [];
    private string? status;
    private bool busy;

    protected override void OnInitialized()
    {
        editContext = new EditContext(model);
        if (!Flow.HasPendingVerification) Navigation.NavigateTo("/auth/login", replace: true);
    }

    private void ShowValidationErrors(EditContext context)
    {
        status = null;
        validationErrors = context.GetValidationMessages().Distinct().ToList();
    }

    private async Task VerifyAsync()
    {
        if (busy) return;
        busy = true;
        status = null;
        validationErrors.Clear();
        VerifyTotpLoginRequest? totpRequest = null;
        try
        {
            var email = Flow.PendingEmail ?? throw new InvalidOperationException("Vuelve a iniciar sesión.");
            var response = Flow.IsTotpSelected
                ? await AuthService.VerifyTotpAsync(totpRequest = Flow.CreateTotpVerificationRequest(model.Code), lifetime.Token)
                : await AuthService.VerifyLoginAsync(Flow.CreateEmailVerificationRequest(model.Code), lifetime.Token);
            if (lifetime.IsCancellationRequested) return;
            if (response?.Error is { ErrorCode: not 0 } || response?.Data is null)
            {
                status = "No se pudo verificar el acceso. Revisa el código e inténtalo de nuevo.";
                return;
            }
            Session.SignIn(response.Data, email, Flow.IsTotpEnabled);
            Flow.Clear();
            model.Code = string.Empty;
            Navigation.NavigateTo("/home", replace: true);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            status = "El código es incorrecto o venció. Revísalo e inténtalo de nuevo.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            status = "Demasiados intentos. Espera un momento e inténtalo de nuevo.";
        }
        catch (HttpRequestException)
        {
            status = "No se pudo verificar el acceso con la API. Inténtalo de nuevo.";
        }
        catch (OperationCanceledException)
        {
            if (!lifetime.IsCancellationRequested) status = "La solicitud tardó demasiado. Inténtalo de nuevo.";
        }
        catch (JsonException)
        {
            status = "No se pudo interpretar la respuesta de verificación.";
        }
        catch (InvalidOperationException ex)
        {
            status = ex.Message;
        }
        finally
        {
            if (totpRequest is not null) totpRequest.Password = string.Empty;
            busy = false;
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
        Flow.Clear();
        model.Code = string.Empty;
    }
}
