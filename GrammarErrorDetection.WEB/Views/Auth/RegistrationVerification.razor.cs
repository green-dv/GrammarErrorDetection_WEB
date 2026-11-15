using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.WEB.Views.Auth.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Views.Auth;

public partial class RegistrationVerification : IDisposable
{
    [Inject] private RegistrationFlowState Flow { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly VerificationCodeModel model = new();
    private readonly CancellationTokenSource lifetime = new();
    private EditContext editContext = default!;
    private List<string> validationErrors = [];
    private string? status;
    private bool busy;
    private bool completed;

    protected override void OnInitialized()
    {
        editContext = new EditContext(model);
        if (!Flow.HasPendingSignup)
        {
            Flow.Clear();
            Navigation.NavigateTo("/auth/register", replace: true);
        }
    }

    private void ShowValidationErrors(EditContext context)
    {
        status = null;
        validationErrors = context.GetValidationMessages().Distinct().ToList();
    }

    private async Task ConfirmRegistrationAsync()
    {
        if (busy || completed) return;
        busy = true;
        status = null;
        validationErrors.Clear();
        SignupConfirmationRequest? request = null;
        try
        {
            request = Flow.CreateConfirmation(model.Code);
            var response = await AuthService.SignupAsync(request, lifetime.Token);
            if (lifetime.IsCancellationRequested) return;
            if (response?.Error is { ErrorCode: not 0 })
            {
                status = "No se pudo confirmar el registro. Revisa el código e inténtalo de nuevo.";
                return;
            }
            if (response?.Data?.User is not { } user || user.Id == Guid.Empty || !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                status = "La API no devolvió la confirmación de la cuenta. Intenta iniciar sesión antes de repetir el registro.";
                return;
            }
            Session.SignIn(response.Data, request.Email, false);
            completed = true;
            model.Code = string.Empty;
            Flow.Clear();
            Navigation.NavigateTo("/home", replace: true);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            status = "El código es incorrecto o venció. Revísalo e inténtalo de nuevo.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            status = "El correo o el CI ya están registrados. Revisa tus datos o inicia sesión.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            status = "Demasiados intentos. Espera un momento e inténtalo de nuevo.";
        }
        catch (HttpRequestException)
        {
            status = "No se pudo confirmar el registro con la API. Inténtalo de nuevo.";
        }
        catch (OperationCanceledException)
        {
            if (!lifetime.IsCancellationRequested) status = "La solicitud tardó demasiado. Inténtalo de nuevo.";
        }
        catch (JsonException)
        {
            status = "No se pudo interpretar la confirmación de la API.";
        }
        catch (InvalidOperationException ex)
        {
            status = ex.Message;
        }
        finally
        {
            if (request is not null) request.Password = string.Empty;
            busy = false;
        }
    }

    private void Cancel()
    {
        Flow.Clear();
        Navigation.NavigateTo("/auth/register");
    }

    public void Dispose()
    {
        lifetime.Cancel();
        lifetime.Dispose();
        Flow.Clear();
        model.Code = string.Empty;
    }
}
