using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Texts;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;

namespace GrammarErrorDetection.WEB.Views.Profiles;

public partial class StudentGrammarList : IDisposable
{
    [Parameter] public Guid UserId { get; set; }
    [Parameter] public Guid? RegistrationId { get; set; }
    [Inject] private ITextService Texts { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? requestCancellation;
    private Guid viewer, loadedUser;
    private Guid? loadedRegistration;
    private int version, paragraphCount, page = 1, pageSize = 20;
    private bool loading, disposed;
    private string category = "";
    private string? error;
    private List<SentenceRow> rows = [];
    private bool Allowed => UserId != Guid.Empty && viewer != Guid.Empty && Session.Current?.User?.Id == viewer &&
        (UserId == viewer || Session.IsInRole("Teacher") || Session.IsInRole("Coordinator"));
    private List<SentenceRow> FilteredRows => string.IsNullOrEmpty(category) ? rows : rows.Where(row => row.Sentence.Errors.Any(item => item.ErrorClassificationId.ToString() == category)).ToList();
    private IEnumerable<SentenceRow> VisibleRows => FilteredRows.Skip((page - 1) * pageSize).Take(pageSize);
    private IEnumerable<CategoryOption> Categories => rows.SelectMany(row => RowCategories(row.Sentence)).GroupBy(item => item.Id).Select(group => group.First()).OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase);
    private static IEnumerable<CategoryOption> RowCategories(TextSentenceResponse sentence) => sentence.Errors
        .GroupBy(item => item.ErrorClassificationId).Select(group => new CategoryOption(group.Key,
            group.Select(item => item.ErrorClassificationName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? $"Categoría {group.Key.ToString("D")[..8]}"));

    protected override void OnInitialized()
    {
        viewer = Session.Current?.User?.Id ?? Guid.Empty;
        Session.Changed += SessionChanged;
    }
    protected override async Task OnParametersSetAsync()
    {
        if (loadedUser == UserId && loadedRegistration == RegistrationId) return;
        loadedUser = UserId; loadedRegistration = RegistrationId;
        category = "";
        await ReloadAsync();
    }
    private void SessionChanged()
    {
        if (Allowed) return;
        version++; requestCancellation?.Cancel(); rows = []; paragraphCount = 0; error = null; loading = false;
        _ = InvokeAsync(StateHasChanged);
    }
    private void FilterChanged() => page = 1;
    private void PreviousPage() => page = Math.Max(1, page - 1);
    private void NextPage() { if (page * pageSize < FilteredRows.Count) page++; }
    private async Task ReloadAsync()
    {
        requestCancellation?.Cancel();
        var current = ++version;
        rows = []; paragraphCount = 0; page = 1; error = null; loading = false;
        if (disposed || !Allowed) return;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        requestCancellation = cancellation;
        var token = cancellation.Token;
        var target = UserId;
        var registration = RegistrationId;
        loading = true;
        try
        {
            var paragraphs = new Dictionary<Guid, TextResponse>();
            // Text has no category/sentence filter: collect every paragraph page for the
            // selected profile before applying sentence-category filtering and pagination.
            const int batchSize = 100;
            for (var number = 1; ; number++)
            {
                var response = await Texts.SearchAsync(new() { UserId = target, RegistrationId = registration, PageNumber = number, PageSize = batchSize }, token);
                token.ThrowIfCancellationRequested();
                if (disposed || !Allowed || current != version || UserId != target) return;
                if (response?.Data is null || response.Error is { ErrorCode: not 0 })
                    throw new InvalidOperationException("No se pudieron consultar los párrafos de este usuario.");
                if (response.Data.Any(item => item.UserId != target || registration.HasValue && item.RegistrationId != registration))
                    throw new InvalidOperationException("La respuesta contiene párrafos que no corresponden al usuario de este perfil.");
                var added = 0;
                foreach (var paragraph in response.Data)
                {
                    if (paragraph.Id == Guid.Empty || paragraph.Sentences.Any(sentence => sentence.TextId != paragraph.Id))
                        throw new InvalidOperationException("La respuesta contiene oraciones que no corresponden a su párrafo.");
                    if (paragraphs.TryAdd(paragraph.Id, paragraph)) added++;
                }
                if (response.Data.Count < batchSize) break;
                if (added == 0) throw new InvalidOperationException("El listado de párrafos no avanzó. Intenta actualizarlo.");
            }
            rows = paragraphs.Values.SelectMany(paragraph => paragraph.Sentences.Select(sentence => new SentenceRow(paragraph.Id, paragraph.SubmissionDate, sentence)))
                .OrderByDescending(row => row.SubmissionDate).ThenBy(row => row.ParagraphId).ThenBy(row => row.Sentence.SentenceIndex).ToList();
            paragraphCount = paragraphs.Count;
            if (!string.IsNullOrEmpty(category) && !Categories.Any(item => item.Id.ToString() == category)) category = "";
        }
        catch (OperationCanceledException) { if (!disposed && current == version && !token.IsCancellationRequested) error = "La consulta tardó demasiado. Inténtalo de nuevo."; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            if (disposed || current != version || !Allowed) return;
            error = ex switch
            {
                HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } => "Tu sesión venció. Vuelve a iniciar sesión.",
                HttpRequestException { StatusCode: HttpStatusCode.Forbidden } => "No tienes permiso para consultar las oraciones de este usuario.",
                HttpRequestException => "No se pudo conectar con el servicio de textos. Inténtalo de nuevo.",
                JsonException => "No se pudo interpretar el listado de oraciones.",
                _ => ex.Message
            };
            if (ex is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized }) { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); }
        }
        finally
        {
            if (current == version) loading = false;
            if (ReferenceEquals(requestCancellation, cancellation)) requestCancellation = null;
        }
    }
    public void Dispose() { disposed = true; version++; Session.Changed -= SessionChanged; lifetime.Cancel(); lifetime.Dispose(); }
    private sealed record SentenceRow(Guid ParagraphId, DateTime SubmissionDate, TextSentenceResponse Sentence);
    private sealed record CategoryOption(Guid Id, string Name);
}
