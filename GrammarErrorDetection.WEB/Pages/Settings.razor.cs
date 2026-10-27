using System.Net;
using System.Text;
using System.Text.Json;
using GrammarErrorDetection.Models.Auth;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.WEB.Views.Auth.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using QRCoder;

namespace GrammarErrorDetection.WEB.Pages;

public partial class Settings : IDisposable
{
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly VerificationCodeModel model = new();
    private readonly CancellationTokenSource lifetime = new();
    private EditContext editContext = default!;
    private List<string> validationErrors = [];
    private string? status;
    private string? qrImage;
    private string? manualKey;
    private bool loading;
    private bool confirming;
    private bool confirmed;

    protected override void OnInitialized() => editContext = new EditContext(model);
    protected override async Task OnParametersSetAsync()
    {
        if (Session.Current is null) { Navigation.NavigateTo("/auth/login", replace: true); return; }
        if (qrImage is null && !confirmed && Session.IsTotpEnabled != true) await SetupTotpAsync();
    }

    private async Task SetupTotpAsync()
    {
        if (loading || confirming || Session.Current is null || Session.IsTotpEnabled == true) return;
        loading = true;
        status = null;
        validationErrors.Clear();
        try
        {
            var response = await AuthService.SetupTotpAsync(lifetime.Token);
            if (lifetime.IsCancellationRequested) return;
            if (response?.Error is { ErrorCode: not 0 } || response?.Data is null) throw new InvalidOperationException("No se pudo preparar el autenticador. Inténtalo de nuevo.");
            var otpUri = response.Data.OtpAuthUri;
            if (!Uri.TryCreate(otpUri, UriKind.Absolute, out var uri) || uri.Scheme != "otpauth" || uri.Host != "totp") throw new InvalidOperationException("La API no devolvió una configuración TOTP válida.");
            manualKey = uri.Query.TrimStart('?').Split('&').Select(item => item.Split('=', 2)).Where(parts => parts.Length == 2 && parts[0] == "secret").Select(parts => Uri.UnescapeDataString(parts[1])).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(manualKey)) throw new InvalidOperationException("La configuración recibida no contiene la clave del autenticador.");
            using var data = QRCodeGenerator.GenerateQrCode(otpUri, QRCodeGenerator.ECCLevel.Q);
            using var renderer = new SvgQRCode(data);
            qrImage = "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(renderer.GetGraphic(6)));
        }
        catch (HttpRequestException ex) { HandleHttpError(ex); }
        catch (JsonException) { status = "No se pudo interpretar la configuración del autenticador."; }
        catch (InvalidOperationException ex) { status = ex.Message; }
        catch (OperationCanceledException) { if (!lifetime.IsCancellationRequested) status = "La solicitud tardó demasiado. Inténtalo de nuevo."; }
        finally { loading = false; }
    }

    private void ShowValidationErrors(EditContext context)
    {
        status = null;
        validationErrors = context.GetValidationMessages().Distinct().ToList();
    }

    private async Task ConfirmTotpAsync()
    {
        if (confirming || loading || qrImage is null || Session.Current is null || confirmed) return;
        confirming = true;
        status = null;
        validationErrors.Clear();
        try
        {
            var response = await AuthService.ConfirmTotpAsync(new TotpConfirmRequest { Code = model.Code }, lifetime.Token);
            if (lifetime.IsCancellationRequested) return;
            if (response?.Error is { ErrorCode: not 0 } || response?.Data != true)
            {
                status = "El código no se pudo confirmar. Revisa el código actual de tu autenticador e inténtalo de nuevo.";
                return;
            }
            confirmed = true;
            qrImage = null;
            manualKey = null;
            model.Code = string.Empty;
            Session.MarkTotpEnabled();
        }
        catch (HttpRequestException ex) { HandleHttpError(ex); }
        catch (JsonException) { status = "No se pudo interpretar la confirmación del código."; }
        catch (OperationCanceledException) { if (!lifetime.IsCancellationRequested) status = "La solicitud tardó demasiado. Inténtalo de nuevo."; }
        finally { confirming = false; }
    }

    private void HandleHttpError(HttpRequestException exception)
    {
        if (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            Session.Clear();
            Navigation.NavigateTo("/auth/login", replace: true);
        }
        else if (exception.StatusCode == HttpStatusCode.Forbidden) status = "Tu cuenta no tiene permiso para realizar esta operación.";
        else if (exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict) status = "No se pudo completar la operación. Revisa el código o si TOTP ya está activado en tu cuenta.";
        else if (exception.StatusCode == HttpStatusCode.TooManyRequests) status = "Demasiados intentos. Espera un momento e inténtalo de nuevo.";
        else status = "No se pudo conectar con la API. Inténtalo de nuevo.";
    }

    public void Dispose()
    {
        lifetime.Cancel();
        lifetime.Dispose();
        qrImage = null;
        manualKey = null;
        model.Code = string.Empty;
    }
}
