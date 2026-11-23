using System.Text;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using QRCoder;

namespace GrammarErrorDetection.WEB.Views.Courses;

public partial class CourseInvitationModal : IDisposable
{
    [Parameter, EditorRequired] public Guid ClassId { get; set; }
    [Parameter] public string? CourseLabel { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Inject] private ICourseInvitationService Invitations { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private string? link, qrImage, error, copyStatus;
    private DateTime? expires;
    private bool loading = true, disposed;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var response = await Invitations.CreateAsync(ClassId, lifetime.Token);
            if (disposed) return;
            if (response?.Data is not { Token.Length: > 0 } data || response.Error is { ErrorCode: not 0 })
                throw new InvalidOperationException(I18n.T("No se pudo generar el enlace de invitación."));
            link = Navigation.ToAbsoluteUri($"auth/join/{Uri.EscapeDataString(data.Token)}").ToString();
            expires = data.ExpiresAt;
            // El QR se genera en el navegador: el enlace no sale a ningún servicio externo.
            using var qr = QRCodeGenerator.GenerateQrCode(link, QRCodeGenerator.ECCLevel.M);
            using var renderer = new SvgQRCode(qr);
            qrImage = "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(renderer.GetGraphic(6)));
        }
        catch (HttpRequestException ex)
        {
            if (disposed) return;
            if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized) { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); return; }
            error = ApiErrors.Describe(ex);
        }
        catch (InvalidOperationException ex) { if (!disposed) error = ex.Message; }
        catch (OperationCanceledException) { }
        finally { loading = false; }
    }

    private async Task CopyAsync()
    {
        if (link is null) return;
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", link);
            copyStatus = I18n.T("Enlace copiado.");
        }
        catch (JSException) { copyStatus = I18n.T("No se pudo copiar. Selecciona el enlace y cópialo manualmente."); }
    }

    private async Task SelectAsync(FocusEventArgs _) => await JS.InvokeVoidAsync("eval", "document.querySelector('.course-invitation-dialog input')?.select()");

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
