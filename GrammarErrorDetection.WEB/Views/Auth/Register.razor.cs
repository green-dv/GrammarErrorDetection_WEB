using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.WEB.Views.Auth.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Views.Auth;

public partial class Register : IDisposable
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private RegistrationFlowState Flow { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private readonly RegisterFormModel model = new();
    private readonly CancellationTokenSource lifetime = new();
    private EditContext editContext = default!;
    private List<string> validationErrors = [];
    private string? status;
    private bool busy;

    protected override void OnInitialized()
    {
        Flow.Clear();
        editContext = new EditContext(model);
    }

    private void ShowValidationErrors(EditContext context)
    {
        status = null;
        validationErrors = context.GetValidationMessages().Distinct().ToList();
    }

    private async Task SubmitRegistrationAsync()
    {
        if (busy) return;
        busy = true;
        status = null;
        validationErrors.Clear();
        Flow.Clear();
        var request = new SendVerificationCodeRequest { Email = model.Email.Trim(), Purpose = "signup", Password = model.Password };
        try
        {
            var response = await AuthService.SendCodeAsync(request, lifetime.Token);
            if (lifetime.IsCancellationRequested) return;
            if (response?.Error is { ErrorCode: not 0 })
            {
                status = "No se pudo iniciar el registro. Revisa tus datos e inténtalo de nuevo.";
                return;
            }
            var challenge = response?.Data;
            if (challenge is null || challenge.ChallengeId == Guid.Empty || string.IsNullOrWhiteSpace(challenge.Email) || !string.Equals(challenge.Email.Trim(), request.Email, StringComparison.OrdinalIgnoreCase))
            {
                status = "La API devolvió una respuesta de registro incompleta.";
                return;
            }
            if (challenge.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                status = "El código recibido ya venció. Solicita uno nuevo.";
                return;
            }
            Flow.BeginVerification(challenge, new SignupRequest { FirstName = model.FirstName.Trim(), LastName = model.LastName.Trim(), CI = model.CI.Trim(), Email = request.Email, Password = model.Password, Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim() });
            Navigation.NavigateTo("/auth/register/verification");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            status = "El correo ya está registrado. Inicia sesión o usa otro correo.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            status = "Revisa el correo y los datos enviados.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            status = "Demasiados intentos. Espera un momento e inténtalo de nuevo.";
        }
        catch (HttpRequestException)
        {
            status = "No se pudo conectar con la API. Inténtalo de nuevo.";
        }
        catch (OperationCanceledException)
        {
            if (!lifetime.IsCancellationRequested) status = "La solicitud tardó demasiado. Inténtalo de nuevo.";
        }
        catch (JsonException)
        {
            status = "No se pudo interpretar la respuesta de la API.";
        }
        finally
        {
            request.Password = string.Empty;
            model.Password = string.Empty;
            model.ConfirmPassword = string.Empty;
            busy = false;
        }
    }

    public void Dispose()
    {
        lifetime.Cancel();
        lifetime.Dispose();
        model.Password = string.Empty;
        model.ConfirmPassword = string.Empty;
    }
}
