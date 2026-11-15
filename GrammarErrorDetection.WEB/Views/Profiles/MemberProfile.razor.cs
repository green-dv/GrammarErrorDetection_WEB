using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Gamification;
using GrammarErrorDetection.Models.Responses;
using GrammarErrorDetection.Models.Users;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;

namespace GrammarErrorDetection.WEB.Views.Profiles;

public partial class MemberProfile : IDisposable
{
    [Parameter] public Guid UserId { get; set; }
    [Parameter] public string Section { get; set; } = "all";
    private bool ShowInformation => Section is "all" or "information";
    private bool ShowCourses => Section is "all" or "courses";
    private bool ShowAchievements => Section is "all" or "achievements";
    [Inject] private IUserService Users { get; set; } = default!;
    [Inject] private IGamificationService Gamification { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private CancellationTokenSource? cancellation;
    private bool disposed, loadingInfo, loadingMedals, hasNext;
    private int version, page = 1;
    private const int PageSize = 6;
    private UserSearchResponse? member;
    private int? academicLevel;
    private List<UserAchievementBestResponse> medals = [];
    private string? infoError, medalsError, pageMessage;
    private bool IsOwn => Session.Current?.User?.Id == UserId;
    private bool CanView => Session.Current is not null;
    private bool CanViewContact => IsOwn || Session.IsInRole("Coordinator");

    /// <summary>Edad cumplida a hoy.</summary>
    internal static int Age(DateOnly birth)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - birth.Year;
        return birth > today.AddYears(-age) ? age - 1 : age;
    }
    private long? PageAwardPoints => medals.Where(item => item.IsObtained).Any(item => item.AwardPoints is null)
        ? null : medals.Where(item => item.IsObtained).Sum(item => (long)(item.AwardPoints ?? 0));
    private string Name => member is null ? (IsOwn ? Session.Current?.User?.Name ?? "Mi perfil" : "Perfil de usuario") : $"{member.FirstName} {member.LastName}".Trim();
    private string Email => member?.Email ?? (IsOwn ? Session.Current?.User?.Email ?? "—" : "—");
    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => p[0])).ToUpperInvariant();

    protected override async Task OnParametersSetAsync() => await ReloadAsync();
    private async Task ReloadAsync()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var current = ++version;
        member = null; academicLevel = null; medals = []; infoError = medalsError = pageMessage = null; page = 1; hasNext = false;
        loadingInfo = loadingMedals = false;
        if (Session.Current is null) { Navigation.NavigateTo("/auth/login", replace: true); return; }
        if (!CanView || UserId == Guid.Empty) return;
        var viewerId = Session.Current.User!.Id;
        await Task.WhenAll(
            ShowInformation || ShowCourses ? LoadInformationAsync(current, viewerId, token) : Task.CompletedTask,
            ShowAchievements ? LoadMedalsAsync(1, current, viewerId, token) : Task.CompletedTask);
    }
    private bool IsCurrent(int current, Guid viewerId) => !disposed && current == version && Session.Current?.User?.Id == viewerId && CanView;

    private async Task LoadInformationAsync(int current, Guid viewerId, CancellationToken token)
    {
        loadingInfo = true; infoError = null;
        try
        {
            var profileId = UserId;
            var found = Require(await Users.GetAsync(profileId, token));
            token.ThrowIfCancellationRequested();
            if (!IsCurrent(current, viewerId)) return;
            if (found.Id != profileId) throw new InvalidOperationException("La información recibida no corresponde al perfil solicitado.");
            member = found;
            if (found.CurrentCourse is { } course)
            {
                var moduleIds = new HashSet<Guid>();
                for (var number = 1; ; number++)
                {
                    var modules = Require(await Users.SearchModulesAsync(course.ModuleName, number, 100, token));
                    token.ThrowIfCancellationRequested();
                    if (!IsCurrent(current, viewerId)) return;
                    var module = modules.FirstOrDefault(item => item.Id == course.ModuleNameId);
                    if (module is not null) { academicLevel = module.Level; break; }
                    if (modules.Count < 100) break;
                    if (modules.Count(item => moduleIds.Add(item.Id)) == 0) break;
                }
            }
        }
        catch (OperationCanceledException) { if (IsCurrent(current, viewerId) && !token.IsCancellationRequested) infoError = "La consulta tardó demasiado."; }
        catch (HttpRequestException ex) { if (IsCurrent(current, viewerId)) infoError = HttpError(ex); }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        { if (IsCurrent(current, viewerId)) infoError = ex is JsonException ? "No se pudo interpretar la información recibida." : ex.Message; }
        finally { if (current == version) loadingInfo = false; }
    }

    private Task LoadMedalPageAsync(int number) => loadingMedals || !CanView || cancellation is null
        ? Task.CompletedTask : LoadMedalsAsync(number, version, Session.Current!.User!.Id, cancellation.Token);

    private async Task LoadMedalsAsync(int number, int current, Guid viewerId, CancellationToken token)
    {
        if (!IsCurrent(current, viewerId)) return;
        loadingMedals = true; medalsError = pageMessage = null;
        try
        {
            var rows = Require(await Gamification.SearchBestUserAchievementsAsync(new() { UserId = UserId, PageNumber = number, PageSize = PageSize }, token));
            token.ThrowIfCancellationRequested();
            if (!IsCurrent(current, viewerId)) return;
            if (number > page && rows.Count == 0) { hasNext = false; pageMessage = "Has llegado al final de las medallas."; return; }
            medals = rows; page = number; hasNext = rows.Count == PageSize;
        }
        catch (OperationCanceledException) { if (IsCurrent(current, viewerId) && !token.IsCancellationRequested) medalsError = "La consulta de medallas tardó demasiado."; }
        catch (HttpRequestException ex) { if (IsCurrent(current, viewerId)) medalsError = HttpError(ex); }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        { if (IsCurrent(current, viewerId)) medalsError = "No se pudieron cargar las medallas."; }
        finally { if (current == version) { loadingMedals = false; if (medalsError is not null) { medals = []; hasNext = false; } } }
    }
    private static T Require<T>(ApiResponse<T>? response) where T : class
        => response is { Data: not null } && response.Error is not { ErrorCode: not 0 }
            ? response.Data : throw new InvalidOperationException(response?.Error?.ErrorMessage ?? "No se pudo obtener la información.");

    private string HttpError(HttpRequestException exception)
    {
        if (exception.StatusCode == HttpStatusCode.Unauthorized) { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); return "La sesión expiró."; }
        return exception.StatusCode == HttpStatusCode.Forbidden ? "No tienes permiso para consultar esta información." : "No se pudo conectar con el servidor. Intenta de nuevo.";
    }
    private static string? Icon(UserAchievementBestResponse item)
    {
        var value = item.IsObtained ? item.BestAchievement?.IconLink ?? item.IconLink : item.NextAchievement?.IconLink ?? item.IconLink;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http" ? uri.AbsoluteUri : null;
    }
    private static string MedalName(UserAchievementBestResponse item) => (item.IsObtained ? item.BestAchievement?.Name : item.NextAchievement?.Name) ?? item.Name;
    private static decimal Progress(UserAchievementBestResponse item) => Math.Clamp(item.IsMaxTier ? 100 : item.ProgressPercent ?? 0, 0, 100);
    public void Dispose() { disposed = true; version++; cancellation?.Cancel(); cancellation?.Dispose(); }
}

