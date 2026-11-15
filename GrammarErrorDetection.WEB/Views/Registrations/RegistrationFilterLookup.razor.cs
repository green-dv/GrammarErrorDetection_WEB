using System.Text.Json;
using GrammarErrorDetection.Models.Users;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using static GrammarErrorDetection.WEB.Views.Registrations.RegistrationViewSupport;

namespace GrammarErrorDetection.WEB.Views.Registrations;

public partial class RegistrationFilterLookup : IDisposable
{
    [Parameter] public bool Students { get; set; }
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Inject] private IUserService Users { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    private const int PageSize = 5;
    private readonly string elementId = $"registration-options-{Guid.NewGuid():N}";
    private List<LookupOption> options = [];
    private CancellationTokenSource? pending;
    private ElementReference searchInput;
    private string term = "", searchField = "FirstName";
    private string? selectedLabel, selectedValue, error;
    private int page = 1, version, activeIndex = -1;
    private bool open, loading, hasNext, disposed, focusSearch;
    private int MaxLength => !Students ? 100 : searchField switch { "Ci" => 15, "Phone" => 12, "Email" => 100, _ => 50 };
    private string? ActiveOptionId => activeIndex >= 0 && activeIndex < options.Count ? $"{elementId}-{activeIndex}" : null;

    protected override void OnParametersSet()
    {
        if (Value != selectedValue) { selectedValue = Value; selectedLabel = null; }
        if (Disabled && open) Close();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!focusSearch || !open || Disabled) return;
        focusSearch = false;
        await searchInput.FocusAsync();
    }
    private async Task ToggleAsync()
    {
        if (Disabled) return;
        if (open) { Close(); return; }
        open = true; focusSearch = true;
        await LoadAsync(1, debounce: false);
    }
    private Task SearchChangedAsync(ChangeEventArgs args)
    {
        term = args.Value?.ToString() ?? "";
        return LoadAsync(1, debounce: true);
    }
    private Task ChangeFieldAsync(ChangeEventArgs args)
    {
        searchField = args.Value?.ToString() ?? "FirstName";
        term = "";
        focusSearch = true;
        return LoadAsync(1, debounce: false);
    }
    private Task RetryAsync() => LoadAsync(page, false);
    private Task PreviousAsync() => LoadAsync(Math.Max(1, page - 1), false);
    private Task NextAsync() => hasNext ? LoadAsync(page + 1, false) : Task.CompletedTask;
    private async Task LoadAsync(int requestedPage, bool debounce)
    {
        if (disposed || !open || Disabled) return;
        pending?.Cancel();
        var cancellation = new CancellationTokenSource();
        pending = cancellation;
        var requestVersion = ++version;
        var owner = Session.Current?.User?.Id;
        loading = true; error = null; options = []; hasNext = false; activeIndex = -1; page = requestedPage;
        var search = term.Trim();
        var field = searchField;
        try
        {
            if (debounce) await Task.Delay(350, cancellation.Token);
            List<LookupOption> found;
            int returned;
            if (Students)
            {
                var filter = new UserSearchFilter();
                switch (field)
                {
                    case "LastName": filter.LastName = search; break;
                    case "Ci": filter.Ci = search; break;
                    case "Phone": filter.Phone = search; break;
                    case "Email": filter.Email = search; break;
                    default: filter.FirstName = search; break;
                }
                var data = Data(await Users.SearchAsync(new() { PageNumber = requestedPage, PageSize = PageSize, Filter = filter.Normalize() }, cancellation.Token));
                returned = data.Count;
                found = data.Where(item => item.Id != Guid.Empty).Select(item => new LookupOption(item.Id,
                    $"{item.FirstName} {item.LastName}".Trim(), $"CIC: {Text(item.Ci)} · {Text(item.Email)} · Tel.: {Text(item.Phone)}")).ToList();
            }
            else
            {
                var data = Data(await Users.SearchModulesAsync(search, requestedPage, PageSize, cancellation.Token));
                returned = data.Count;
                found = data.Where(item => item.Id != Guid.Empty).Select(item => new LookupOption(item.Id, item.Name, null)).ToList();
            }
            if (disposed || requestVersion != version || cancellation.IsCancellationRequested || Session.Current?.User?.Id != owner) return;
            options = found.DistinctBy(item => item.Id).ToList();
            hasNext = returned == PageSize;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
        {
            if (!disposed && requestVersion == version) error = Error(ex);
        }
        finally
        {
            if (requestVersion == version) loading = false;
            if (ReferenceEquals(pending, cancellation)) pending = null;
            cancellation.Dispose();
        }
    }
    private async Task SelectAsync(LookupOption option)
    {
        if (Disabled || loading) return;
        selectedValue = option.Id.ToString("D"); selectedLabel = option.Label;
        Close();
        await ValueChanged.InvokeAsync(selectedValue);
    }
    private async Task ClearAsync()
    {
        selectedValue = null; selectedLabel = null;
        Close();
        await ValueChanged.InvokeAsync(null);
    }
    private void Close()
    {
        open = false; focusSearch = false; pending?.Cancel(); pending = null; version++; loading = false;
    }
    private async Task HandleKeyAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Escape") { Close(); return; }
        if (!open || loading || options.Count == 0) return;
        if (args.Key == "ArrowDown") activeIndex = (activeIndex + 1) % options.Count;
        else if (args.Key == "ArrowUp") activeIndex = activeIndex <= 0 ? options.Count - 1 : activeIndex - 1;
        else if (args.Key == "Enter" && activeIndex >= 0) await SelectAsync(options[activeIndex]);
    }
    public void Dispose() { disposed = true; pending?.Cancel(); version++; }
    private sealed record LookupOption(Guid Id, string Label, string? Detail);
}
