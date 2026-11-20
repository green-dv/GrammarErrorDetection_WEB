using System.Globalization;
using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Users;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.AspNetCore.Components;

namespace GrammarErrorDetection.WEB.Pages;

public partial class Leaderboard : IDisposable
{
    [Inject] private ILeaderboardService LeaderboardService { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private List<LeaderboardEntry> entries = [];
    private List<UserLookupOption> modules = [];
    private LeaderboardRequest applied = new();
    private string moduleId = "", bimester = "", year = "";
    private string periodLabel = I18n.T("Todos los tiempos"), moduleLabel = I18n.T("Todos los módulos");
    private string? error, moduleError, validationError;
    private bool loading, loadingModules, disposed;

    protected override Task OnInitializedAsync() => Task.WhenAll(LoadAsync(), LoadModulesAsync());

    private async Task LoadModulesAsync()
    {
        if (loadingModules || disposed) return;
        loadingModules = true;
        moduleError = null;
        try
        {
            Dictionary<Guid, UserLookupOption> catalog = [];
            for (var page = 1; ; page++)
            {
                var response = await UserService.GetModulesAsync(page, lifetime.Token);
                if (response?.Error is not null || response?.Data is null)
                    throw new InvalidOperationException(I18n.T("No se pudo consultar el catálogo de módulos."));
                foreach (var module in response.Data)
                    if (module.Id == Guid.Empty || !catalog.TryAdd(module.Id, module))
                        throw new InvalidOperationException(I18n.T("El catálogo de módulos contiene páginas repetidas o identificadores inválidos."));
                if (response.Data.Count < 100) break;
            }
            if (!disposed) modules = catalog.Values.OrderBy(item => item.Name).ToList();
        }
        catch (OperationCanceledException) { if (!disposed) moduleError = I18n.T("La consulta de módulos tardó demasiado. Inténtalo de nuevo."); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        { if (!disposed) moduleError = I18n.T("No se pudieron cargar los módulos. ") + ErrorMessage(ex); }
        finally { loadingModules = false; }
    }

    private Task SearchAsync()
    {
        if (loading || disposed) return Task.CompletedTask;
        validationError = null;
        int? selectedBimester = null, selectedYear = null;
        Guid? selectedModule = null;
        if (bimester.Length > 0)
        {
            if (!int.TryParse(bimester, out var value) || value is < 1 or > 6)
            { validationError = I18n.T("Selecciona un bimestre entre 1 y 6."); return Task.CompletedTask; }
            selectedBimester = value;
        }
        if (!string.IsNullOrWhiteSpace(year))
        {
            if (!int.TryParse(year, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value <= 0)
            { validationError = I18n.T("El año debe ser un entero mayor que cero."); return Task.CompletedTask; }
            selectedYear = value;
        }
        if (moduleId.Length > 0)
        {
            if (!Guid.TryParse(moduleId, out var value) || value == Guid.Empty || !modules.Any(item => item.Id == value))
            { validationError = I18n.T("Selecciona un módulo disponible."); return Task.CompletedTask; }
            selectedModule = value;
        }
        applied = new() { Bimester = selectedBimester, Year = selectedYear, ModuleNameId = selectedModule };
        periodLabel = (selectedBimester, selectedYear) switch
        {
            (null, null) => I18n.T("Todos los tiempos"),
            (null, var y) => I18n.T("Todos los bimestres · {0}", y),
            (var b, null) => I18n.T("Bimestre {0} · Todos los años", b),
            (var b, var y) => I18n.T("Bimestre {0} · {1}", b, y)
        };
        moduleLabel = modules.FirstOrDefault(item => item.Id == selectedModule)?.Name ?? I18n.T("Todos los módulos");
        return LoadAsync();
    }

    private Task ClearAsync()
    {
        if (loading) return Task.CompletedTask;
        moduleId = bimester = year = "";
        return SearchAsync();
    }

    private async Task LoadAsync()
    {
        if (loading || disposed) return;
        loading = true;
        error = null;
        entries = [];
        try
        {
            var response = await LeaderboardService.GetAsync(applied, lifetime.Token);
            if (disposed) return;
            if (response?.Error is not null) throw new InvalidOperationException(response.Error.ErrorMessage ?? I18n.T("No se pudo consultar la clasificación."));
            entries = response?.Data ?? throw new InvalidOperationException(I18n.T("La API no devolvió una clasificación válida."));
        }
        catch (OperationCanceledException) { if (!disposed) error = I18n.T("La consulta tardó demasiado. Inténtalo de nuevo."); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        { if (!disposed) error = ErrorMessage(ex); }
        finally { loading = false; }
    }

    private static string ErrorMessage(Exception ex) => ex switch
    {
        HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } => I18n.T("Tu sesión expiró. Vuelve a iniciar sesión."),
        HttpRequestException { StatusCode: HttpStatusCode.Forbidden } => I18n.T("La API no permitió consultar estos datos."),
        HttpRequestException { StatusCode: HttpStatusCode.BadRequest } => I18n.T("La API rechazó los datos de la consulta. Revisa los filtros y vuelve a iniciar sesión si el problema continúa."),
        HttpRequestException => I18n.T("No se pudo conectar con la API. Inténtalo de nuevo."),
        JsonException => I18n.T("La API devolvió datos con un formato inesperado."),
        _ => ex.Message
    };

    private static string Name(LeaderboardEntry entry) => !string.IsNullOrWhiteSpace(entry.UserName) ? entry.UserName
        : string.Join(" ", new[] { entry.FirstName, entry.LastName }.Where(part => !string.IsNullOrWhiteSpace(part))) is { Length: > 0 } name ? name : I18n.T("Participante");
    private static string Initials(LeaderboardEntry entry) => string.Concat(Name(entry).Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => StringInfo.GetNextTextElement(part))).ToUpperInvariant();
    private static string Module(LeaderboardEntry entry) => string.IsNullOrWhiteSpace(entry.ModuleName) ? I18n.T("Sin módulo") : entry.ModuleName;

    public void Dispose()
    {
        disposed = true;
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
