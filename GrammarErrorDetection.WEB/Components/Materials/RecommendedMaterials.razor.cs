using System.Globalization;
using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Materials;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using GrammarErrorDetection.WEB.Views.Materials;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace GrammarErrorDetection.WEB.Components.Materials;

/// <summary>
/// Material de apoyo recomendado según los errores gramaticales del estudiante.
/// Sin UserId muestra las recomendaciones del usuario autenticado (estudiante).
/// </summary>
public partial class RecommendedMaterials : IDisposable
{
    [Parameter] public Guid? UserId { get; set; }
    [Parameter] public Guid? ClassId { get; set; }
    [Parameter] public string Title { get; set; } = "Recomendado para ti";
    [Parameter] public int CategoryLimit { get; set; } = 3;
    [Parameter] public int MaterialsPerCategory { get; set; } = 3;

    [Inject] private ISupportMaterialService MaterialService { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;

    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");
    private readonly string headingId = $"recommended-{Guid.NewGuid():N}";
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? request;
    private List<SupportMaterialRecommendationResponse> recommendations = [];
    private Guid? loadedUser, loadedClass;
    private bool initialized, loading, disposed;
    private string? error;

    private string ApiBaseUrl => Configuration["Api:BaseUrl"]!;

    protected override async Task OnParametersSetAsync()
    {
        if (initialized && loadedUser == UserId && loadedClass == ClassId) return;
        initialized = true;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (disposed) return;
        if (Session.Current?.User is null) { Navigation.NavigateTo("/auth/login", replace: true); return; }
        request?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        request = cancellation;
        loadedUser = UserId;
        loadedClass = ClassId;
        loading = true;
        error = null;
        try
        {
            var response = await MaterialService.GetRecommendationsAsync(UserId, ClassId, CategoryLimit, MaterialsPerCategory, cancellation.Token);
            if (disposed || cancellation.IsCancellationRequested) return;
            if (response?.Error is { } apiError)
                throw new InvalidOperationException(apiError.ErrorMessage ?? "No se pudieron obtener las recomendaciones.");
            var rows = response?.Data ?? throw new InvalidOperationException("La respuesta de recomendaciones no tiene el formato esperado.");
            recommendations = rows.OrderBy(row => row.Rank)
                .Select(row => { row.Materials = row.Materials.OrderBy(material => material.Rank).ToList(); return row; })
                .Where(row => row.Materials.Count > 0)
                .ToList();
        }
        catch (OperationCanceledException)
        {
            if (!disposed && !lifetime.IsCancellationRequested && ReferenceEquals(request, cancellation))
                error = "La consulta tardó demasiado. Inténtalo de nuevo.";
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or ArgumentException)
        {
            if (!disposed && ReferenceEquals(request, cancellation)) error = ErrorMessage(ex);
        }
        finally
        {
            if (ReferenceEquals(request, cancellation))
            {
                loading = false;
                request = null;
                if (error is not null) recommendations = [];
            }
        }
    }

    private MaterialView? viewing;

    private static string Format(RecommendedMaterialResponse material) =>
        MaterialPresentation.IsFileServerLink(material.FileLink)
            ? MaterialPresentation.FormatFromExtension(material.FileExtension, material.MimeType)
            : "URL";

    // ---- Interacciones: solo el propio estudiante registra vistas y responde "¿Te sirvió?". ----
    private readonly Dictionary<Guid, Task> pendingViews = [];
    private readonly HashSet<Guid> voting = [];
    private RecommendedMaterialResponse? viewingMaterial;
    private string commentDraft = "";
    private bool commentSent;
    private string? feedbackError;

    private bool TracksInteractions => UserId is null && Session.IsInRole("Student");

    private static string? ReasonLabel(RecommendedMaterialResponse material) => material.ReasonCode switch
    {
        "EFFECTIVE" => "Ayudó a otros a reducir este error",
        "PEERS_HELPFUL" => "Útil para tus compañeros",
        "FORMAT_PREFERENCE" => "En un formato que te funciona",
        "REVIEW" => "Para repasar",
        "NEW" => "Nuevo para ti",
        _ => null
    };

    // Registra la vista sin bloquear la apertura: un fallo aquí no debe impedir ver el material.
    private void TrackView(RecommendedMaterialResponse material, bool downloaded)
    {
        if (!TracksInteractions) return;
        material.Seen = true;
        pendingViews[material.Id] = RegisterViewAsync(material.Id, downloaded);
    }

    private async Task RegisterViewAsync(Guid materialId, bool downloaded)
    {
        try { await MaterialService.RegisterViewAsync(materialId, downloaded, lifetime.Token); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException or ArgumentException) { }
    }

    private async Task VoteAsync(RecommendedMaterialResponse material, bool helpful, string? comment = null)
    {
        if (!TracksInteractions || !voting.Add(material.Id)) return;
        feedbackError = null;
        try
        {
            if (pendingViews.TryGetValue(material.Id, out var view)) await view;
            var response = await MaterialService.SetFeedbackAsync(material.Id, helpful, comment, lifetime.Token);
            if (response?.Error is { } apiError) throw new InvalidOperationException(apiError.ErrorMessage);
            material.Helpful = helpful;
            if (comment is not null) commentSent = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or ArgumentException)
        {
            feedbackError = "No se pudo guardar tu respuesta. Inténtalo de nuevo.";
        }
        catch (OperationCanceledException) { }
        finally { voting.Remove(material.Id); }
    }

    private Task SendCommentAsync() =>
        viewingMaterial is { } material && !string.IsNullOrWhiteSpace(commentDraft)
            ? VoteAsync(material, false, commentDraft)
            : Task.CompletedTask;

    private void Open(SupportMaterialRecommendationResponse category, RecommendedMaterialResponse material)
    {
        var link = MaterialPresentation.ResolveLink(material.FileLink, ApiBaseUrl);
        if (link is null) return;
        TrackView(material, false);
        viewingMaterial = material;
        commentDraft = "";
        commentSent = false;
        feedbackError = null;
        viewing = new MaterialView(material.Name, category.ErrorClassificationName, category.ErrorClassificationDescription,
            Format(material), string.IsNullOrWhiteSpace(material.FileTypeName) ? "Recurso" : material.FileTypeName,
            material.FileSize, material.UploadDate,
            string.IsNullOrWhiteSpace(material.UploadedByName) ? null : material.UploadedByName,
            link, material.FileExtension);
    }

    private void Close()
    {
        viewing = null;
        viewingMaterial = null;
    }

    private string ErrorMessage(Exception exception)
    {
        if (exception is HttpRequestException http)
        {
            if (http.StatusCode == HttpStatusCode.Unauthorized)
            {
                Session.Clear();
                Navigation.NavigateTo("/auth/login", replace: true);
                return "Tu sesión venció. Vuelve a iniciar sesión.";
            }
            return http.StatusCode switch
            {
                HttpStatusCode.Forbidden => "No tienes permiso para ver las recomendaciones de este estudiante.",
                HttpStatusCode.BadRequest => "No se pudieron calcular las recomendaciones para la selección actual.",
                HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed => "La API en ejecución todavía no ofrece recomendaciones de material.",
                _ => "No se pudo consultar el material recomendado. Inténtalo de nuevo."
            };
        }
        return exception switch
        {
            JsonException => "La respuesta no tiene el formato esperado.",
            // Mensajes de la API o propios (en español); las guardas de argumentos traen textos técnicos.
            InvalidOperationException when !string.IsNullOrWhiteSpace(exception.Message) => exception.Message,
            _ => "No se pudo preparar la consulta. Recarga la página e inténtalo de nuevo."
        };
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        request?.Cancel();
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
