using System.Text.Json;
using GrammarErrorDetection.Models.Registrations;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using static GrammarErrorDetection.WEB.Views.Registrations.RegistrationViewSupport;

namespace GrammarErrorDetection.WEB.Views.Registrations;

public partial class RegistrationList : IDisposable
{
    [Parameter] public bool Mine { get; set; }
    [Parameter] public bool CoursesOnly { get; set; }
    [Inject] private IRegistrationService Registrations { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private RegistrationListFilters filters = new();
    private RegistrationFilter? appliedFilter;
    private List<RegistrationListItem> entries = [];
    private RegistrationListItem? reviewing;
    private Guid owner;
    private bool loading, deciding, applying, approving, hasNext, disposed;
    private long? total;
    private string PagerInfo => total is { } all ? I18n.T("Mostrando {0}–{1} de {2}", (page - 1) * pageSize + 1, (page - 1) * pageSize + entries.Count, all) : I18n.T("{0} resultados en esta página", entries.Count);
    private string PageOf => total is { } all ? I18n.T(" de {0}", Math.Max(1, (all + pageSize - 1) / pageSize)) : "";
    private string? error, message, decisionError;
    private int page = 1, pageSize = 20;
    private bool Allowed => Session.Current?.User?.Id == owner && (Mine ? Session.IsInRole("Student") : Session.IsStaff);
    private string Title => CoursesOnly ? I18n.T("Mis cursos") : Mine ? I18n.T("Mis inscripciones") : I18n.T("Inscripciones");
    private const int Columns = 8;

    protected override async Task OnInitializedAsync()
    {
        owner = Session.Current?.User?.Id ?? Guid.Empty;
        if (!Mine) filters = new() { Status = "Pending" };
        appliedFilter = Mine ? null : filters.ToFilter(Mine, CoursesOnly);
        Session.Changed += SessionChanged;
        await LoadAsync(1);
    }
    private void SessionChanged()
    {
        if (Session.Current?.User?.Id == owner) return;
        lifetime.Cancel();
        entries.Clear();
        reviewing = null;
        applying = false;
        _ = InvokeAsync(StateHasChanged);
    }
    private Task SearchAsync()
    {
        appliedFilter = filters.ToFilter(Mine, CoursesOnly);
        return LoadAsync(1);
    }
    private Task ClearAsync()
    {
        filters = new();
        appliedFilter = filters.ToFilter(Mine, CoursesOnly);
        return LoadAsync(1);
    }
    private Task ChangeSizeAsync() => LoadAsync(1);
    private Task RetryAsync() => LoadAsync(page);
    private Task PreviousAsync() => LoadAsync(Math.Max(1, page - 1));
    private Task NextAsync() => hasNext ? LoadAsync(page + 1) : Task.CompletedTask;
    private async Task LoadAsync(int requestedPage)
    {
        if (disposed || loading || !Allowed) return;
        loading = true;
        error = null;
        entries = [];
        total = null;
        hasNext = false;
        page = requestedPage;
        try
        {
            var request = new RegistrationSearchRequest { PageNumber = requestedPage, PageSize = pageSize, Filter = Mine ? null : appliedFilter };
            var response = Mine ? await Registrations.GetMineAsync(request, lifetime.Token) : await Registrations.SearchAsync(request, lifetime.Token);
            if (disposed || !Allowed) return;
            var data = Data(response);
            if (data.Any(item => Mine && item.UserId.HasValue && item.UserId != owner))
                throw new InvalidOperationException(I18n.T("El servicio devolvió inscripciones no válidas para esta consulta."));
            total = response?.TotalCount;
            hasNext = total is { } all ? (long)requestedPage * pageSize < all : data.Count == pageSize;
            entries = CoursesOnly ? data.Where(item => item.Active != false && !string.Equals(item.Status, "Rejected", StringComparison.OrdinalIgnoreCase)).ToList() : data;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
        {
            if (!disposed) { error = Error(ex); HandleUnauthorized(ex); }
        }
        finally { loading = false; }
    }
    private void Review(RegistrationListItem item, bool approve)
    {
        if (!Allowed || Mine || deciding || !IsPending(item)) return;
        reviewing = item; approving = approve; decisionError = null;
    }
    private void CloseReview() { if (!deciding) { reviewing = null; decisionError = null; } }
    private async Task ConfirmAsync()
    {
        if (!Allowed || Mine || deciding || reviewing is not { } item || !IsPending(item)) return;
        deciding = true;
        decisionError = null;
        try
        {
            var response = Data(await Registrations.DecideAsync(item.RegistrationId!.Value, approving, lifetime.Token));
            if (disposed || !Allowed) return;
            if (response.Id != item.RegistrationId || (item.UserId.HasValue && response.UserId != item.UserId) || (item.ClassId.HasValue && response.ClassId != item.ClassId)
                || response.Active != approving || !string.Equals(response.Status, approving ? "Approved" : "Rejected", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(I18n.T("No se pudo confirmar la decisión. Actualiza el listado antes de volver a intentarlo."));
            reviewing = null;
            message = approving ? I18n.T("Inscripción aprobada.") : I18n.T("Inscripción rechazada y desactivada. El estudiante ya no pertenece al curso.");
            await LoadAsync(page);
            if (!disposed && error is null && entries.Count == 0 && page > 1) await LoadAsync(page - 1);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
        {
            if (!disposed) { decisionError = Error(ex); HandleUnauthorized(ex); }
        }
        finally { deciding = false; }
    }
    private void HandleUnauthorized(Exception ex)
    {
        if (ex is not HttpRequestException { StatusCode: System.Net.HttpStatusCode.Unauthorized }) return;
        Session.Clear();
        Navigation.NavigateTo("/auth/login", replace: true);
    }
    private void OpenApply() { if (Allowed && Mine && !CoursesOnly) applying = true; }
    private void CloseApply() => applying = false;
    private async Task AppliedAsync(RegistrationResponse item)
    {
        applying = false;
        message = I18n.T("Solicitud guardada. La inscripción está pendiente de aprobación.");
        await LoadAsync(1);
    }
    public void Dispose()
    {
        disposed = true;
        Session.Changed -= SessionChanged;
        lifetime.Cancel(); lifetime.Dispose();
    }
}
