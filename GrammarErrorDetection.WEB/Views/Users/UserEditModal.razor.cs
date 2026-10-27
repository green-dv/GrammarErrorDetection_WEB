using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Users;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.WEB.Views.Users.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Views.Users;

public partial class UserEditModal : IDisposable
{
    [Parameter, EditorRequired] public UserSearchResponse User { get; set; } = default!;
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<UserUpdateResponse> OnSaved { get; set; }
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private UserEditForm model = default!;
    private EditContext editContext = default!;
    private List<string> validationErrors = [];
    private readonly CancellationTokenSource lifetime = new();
    private bool saving, disposed;
    private string? error;
    private bool HasChanges => model.HasChangesFrom(User);

    protected override void OnInitialized()
    {
        model = UserEditForm.From(User);
        editContext = new EditContext(model);
    }

    private async Task SaveAsync()
    {
        if (saving || disposed) return;
        error = null;
        validationErrors.Clear();
        if (!HasChanges) return;
        if (!editContext.Validate())
        {
            validationErrors = editContext.GetValidationMessages().Distinct().ToList();
            return;
        }
        if (Session.Current is null) { Navigation.NavigateTo("/auth/login", replace: true); return; }
        if (!Session.Roles.Contains("Coordinator", StringComparer.Ordinal)) { error = "No tienes permiso para actualizar usuarios."; return; }
        saving = true;
        try
        {
            var response = await UserService.UpdateDetailsAsync(model.ToRequest(User.Id), lifetime.Token);
            if (disposed) return;
            if (response?.Data is null || response.Data.Id != User.Id || response.Error is { ErrorCode: not 0 })
            {
                error = "No se pudo confirmar la actualización. Revisa los datos e inténtalo de nuevo.";
                return;
            }
            await OnSaved.InvokeAsync(response.Data);
        }
        catch (HttpRequestException ex)
        {
            if (disposed) return;
            if (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                Session.Clear();
                Navigation.NavigateTo("/auth/login", replace: true);
            }
            error = ex.StatusCode switch
            {
                HttpStatusCode.Forbidden => "No tienes permiso para actualizar usuarios.",
                HttpStatusCode.Conflict => "El correo o el CI ya están registrados. Revisa los datos.",
                HttpStatusCode.BadRequest => "Revisa los datos del usuario e inténtalo de nuevo.",
                _ => "No se pudo guardar el usuario. Inténtalo de nuevo."
            };
        }
        catch (OperationCanceledException) { if (!disposed) error = "La actualización tardó demasiado. Inténtalo de nuevo."; }
        catch (JsonException) { if (!disposed) error = "No se pudo interpretar la respuesta de actualización."; }
        finally { saving = false; }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
