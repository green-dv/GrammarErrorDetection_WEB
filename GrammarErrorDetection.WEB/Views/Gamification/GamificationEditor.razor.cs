using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using GrammarErrorDetection.Models.Gamification;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using GrammarErrorDetection.WEB.Views.Gamification.Badges;

namespace GrammarErrorDetection.WEB.Views.Gamification;

public partial class GamificationEditor : IDisposable
{
    [Parameter] public bool IsAchievement { get; set; }
    [Parameter] public Guid? InitialCollectionId { get; set; }
    [Parameter] public AchievementResponse? Achievement { get; set; }
    [Parameter] public ChallengeResponse? Challenge { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }
    [Inject] private IGamificationService Service { get; set; } = default!;
    [Inject] private AuthSessionState Session { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private readonly CancellationTokenSource lifetime = new();
    private readonly string formId = $"gamification-{Guid.NewGuid():N}";
    private EditorModel model = new();
    private EditContext context = default!;
    private List<AchievementCategoryResponse> categories = [];
    private List<AchievementCollectionResponse> collections = [];
    private List<ConditionTypeResponse> conditions = [];
    private bool saving, loading, readingImage, disposed, uncertain;
    private string? error, catalogError, fileError;
    private byte[]? imageBytes;
    private string? imageName, imageType, imagePreview;
    private int fileVersion;
    private bool showBadgeDesigner;
    private BadgeDesign? badgeDesign;
    private void OpenBadgeDesigner()
    {
        if (!saving && !readingImage && !loading && !uncertain && CheckPermission()) showBadgeDesigner = true;
    }
    private void CloseBadgeDesigner() => showBadgeDesigner = false;
    private void BadgeGenerated(GeneratedBadge badge)
    {
        if (disposed) return;
        imageBytes = badge.Bytes; imageName = badge.FileName; imageType = badge.ContentType;
        imagePreview = $"data:image/png;base64,{Convert.ToBase64String(badge.Bytes)}";
        badgeDesign = badge.Design with { }; fileError = null; fileVersion++;
        showBadgeDesigner = false;
    }
    private List<string> errors = [];
    // Condiciones: lista editable; se cumplen todas. Se envían completas en la misma llamada de guardar.
    private const int MaxConditions = 10;
    private List<ConditionRow> conditionRows = [];
    private readonly Dictionary<Guid, string?> originalCodes = [];
    private readonly Dictionary<Guid, string?> originalNames = [];
    private string originalConditions = "[]";
    private bool IsEdit => Achievement is not null || Challenge is not null;
    private bool ConditionLocked => !IsAchievement && Challenge is { Status: not "Draft" };
    private string Title => $"{(IsEdit ? "Editar" : "Crear")} {(IsAchievement ? "logro" : "desafío")}";
    private string? CodeOf(ConditionRow row) => conditions.FirstOrDefault(item => item.Id == row.TypeId)?.Code ?? originalCodes.GetValueOrDefault(row.TypeId);
    private bool ConditionsChanged => ConditionsKey() != originalConditions;

    protected override async Task OnInitializedAsync()
    {
        if (IsAchievement && Achievement is null && InitialCollectionId is { } collectionId)
            model.CollectionId = collectionId;
        if (Achievement is { } a)
            model = new() { Name = a.Name, Description = a.Description, Points = a.Points, Active = a.Active,
                IsSecret = a.IsSecret, CategoryId = a.AchievementCategoryId,
                CollectionId = a.AchievementCollectionId ?? Guid.Empty, TierOrder = a.TierOrder };
        else if (Challenge is { } c)
            model = new() { Name = c.Title, Description = c.Description, Instructions = c.Instructions, Difficulty = c.Difficulty,
                Points = c.Points, Active = c.Active, StartDate = c.StartDate, EndDate = c.EndDate, Status = c.Status };
        foreach (var condition in Achievement?.Conditions ?? Challenge?.Conditions ?? [])
        {
            originalCodes[condition.ConditionTypeId] = condition.ConditionCode;
            originalNames[condition.ConditionTypeId] = condition.ConditionName;
            conditionRows.Add(new()
            {
                TypeId = condition.ConditionTypeId, Operator = condition.ComparisonOperator, Target = condition.TargetValue,
                MinimumWords = ConditionConfiguration.ReadNumber(condition.ConditionParameters, "MinimumWords"),
                MinimumScore = ConditionConfiguration.ReadNumber(condition.ConditionParameters, "MinimumScore")
            });
        }
        originalConditions = ConditionsKey();
        // Un logro nuevo siempre necesita una condición: se ofrece la primera fila vacía.
        if (IsAchievement && conditionRows.Count == 0) conditionRows.Add(new());
        context = new EditContext(model);
        await LoadCatalogsAsync();
    }
    private void AddCondition() { if (!ConditionLocked && conditionRows.Count < MaxConditions) conditionRows.Add(new()); }
    private void RemoveCondition(ConditionRow row) { if (!ConditionLocked) conditionRows.Remove(row); }
    private static void ConditionTypeChanged(ConditionRow row, object? value)
    {
        row.TypeId = Guid.TryParse(value?.ToString(), out var id) ? id : Guid.Empty;
        row.MinimumWords = null;
        row.MinimumScore = null;
    }

    // Lista para la API; los parámetros solo se incluyen para las actividades que los usan.
    private List<GamificationConditionRequest> BuildConditions() => conditionRows.Select(row =>
    {
        var code = CodeOf(row);
        return new GamificationConditionRequest
        {
            ConditionTypeId = row.TypeId, ComparisonOperator = row.Operator, TargetValue = row.Target,
            ConditionParameters = ConditionConfiguration.IsSupported(code) && row.TypeId != Guid.Empty
                ? NullIfEmpty(ConditionConfiguration.BuildParameters(code, row.MinimumWords, code == "EVALUATIONS_ABOVE_SCORE" ? row.MinimumScore ?? 0 : null))
                : null
        };
    }).ToList();
    private static Dictionary<string, object?>? NullIfEmpty(Dictionary<string, object?> value) => value.Count == 0 ? null : value;
    private string ConditionsKey() => JsonSerializer.Serialize(conditionRows.Select(row =>
        new { row.TypeId, row.Operator, row.Target, row.MinimumWords, row.MinimumScore }));

    private List<string> ValidateConditions()
    {
        var problems = new List<string>();
        if (ConditionLocked) return problems;
        if (IsAchievement && conditionRows.Count == 0) problems.Add("El logro requiere al menos una condición.");
        if (!IsAchievement && model.Status == "Published" && conditionRows.Count == 0) problems.Add("Para publicar el desafío agrega al menos una condición.");
        if (conditionRows.Count > MaxConditions) problems.Add($"Se permiten hasta {MaxConditions} condiciones.");
        for (var index = 0; index < conditionRows.Count; index++)
        {
            var row = conditionRows[index];
            if (row.TypeId == Guid.Empty) { problems.Add($"Condición {index + 1}: selecciona la actividad."); continue; }
            problems.AddRange(ConditionConfiguration.Validate(CodeOf(row), row.Target, row.Operator, row.MinimumWords, row.MinimumScore)
                .Select(problem => $"Condición {index + 1}: {problem}"));
        }
        return problems;
    }

    private bool CheckPermission()
    {
        if (Session.Current is null) { Navigation.NavigateTo("/auth/login", replace: true); return false; }
        if (Session.IsInRole("Coordinator")) return true;
        error = "No tienes permiso para gestionar la gamificación."; return false;
    }

    private async Task LoadCatalogsAsync()
    {
        if (loading || disposed || !CheckPermission()) return;
        loading = true; catalogError = null;
        try
        {
            conditions = await ReadAllAsync<ConditionTypeResponse>((page) => Service.SearchConditionTypesAsync(new() { PageNumber = page, PageSize = 100 }, lifetime.Token), item => item.Id);
            if (IsAchievement)
            {
                categories = await ReadAllAsync<AchievementCategoryResponse>((page) => Service.SearchAchievementCategoriesAsync(new() { PageNumber = page, PageSize = 100 }, lifetime.Token), item => item.Id);
                collections = await ReadAllAsync<AchievementCollectionResponse>((page) => Service.SearchAchievementCollectionsAsync(new() { PageNumber = page, PageSize = 100 }, lifetime.Token), item => item.Id);
            }
        }
        catch (OperationCanceledException) { if (!disposed) catalogError = "La carga de catálogos tardó demasiado."; }
        catch (HttpRequestException ex) { if (!disposed) catalogError = HttpError(ex); }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException)
        { if (!disposed) catalogError = "No se pudieron cargar los catálogos de categorías, colecciones o condiciones."; }
        finally { loading = false; }
    }

    private async Task<List<T>> ReadAllAsync<T>(Func<int, Task<GrammarErrorDetection.Models.Responses.ApiResponse<List<T>>?>> fetch, Func<T, Guid> id)
    {
        var result = new Dictionary<Guid, T>();
        for (var page = 1; ; page++)
        {
            var rows = GamificationAdmin.Require(await fetch(page));
            lifetime.Token.ThrowIfCancellationRequested();
            var added = 0;
            foreach (var item in rows) if (id(item) != Guid.Empty && result.TryAdd(id(item), item)) added++;
            if (rows.Count < 100) return result.Values.ToList();
            if (added == 0) throw new InvalidOperationException("El catálogo no avanzó de página.");
        }
    }

    private async Task SelectFileAsync(InputFileChangeEventArgs args)
    {
        if (readingImage || saving) return;
        readingImage = true;
        fileError = null; imageBytes = null; imagePreview = null; imageName = imageType = null;
        try
        {
            var file = args.File;
            if (file.Size is <= 0 or > 5 * 1024 * 1024) throw new InvalidOperationException("La imagen debe pesar entre 1 byte y 5 MB.");
            if (file.ContentType is not ("image/png" or "image/jpeg" or "image/webp" or "image/gif"))
                throw new InvalidOperationException("Selecciona una imagen PNG, JPG, WebP o GIF.");
            using var stream = file.OpenReadStream(5 * 1024 * 1024, lifetime.Token);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, lifetime.Token);
            imageBytes = buffer.ToArray(); imageName = file.Name; imageType = file.ContentType;
            imagePreview = $"data:{imageType};base64,{Convert.ToBase64String(imageBytes)}";
        }
        catch (OperationCanceledException) { if (!disposed) fileError = "No se completó la carga de la imagen."; }
        catch (Exception ex) when (ex is IOException or InvalidOperationException) { fileError = ex.Message; }
        finally { readingImage = false; }
    }
    private void ClearImage() { imageBytes = null; imagePreview = null; imageName = imageType = null; fileError = null; fileVersion++; }

    private async Task SaveAsync()
    {
        if (saving || showBadgeDesigner || loading || readingImage || uncertain || disposed || !CheckPermission()) return;
        error = null; errors.Clear();
        if (!context.Validate()) errors.AddRange(context.GetValidationMessages());
        if (fileError is not null) errors.Add(fileError);
        if (IsAchievement && model.CategoryId == Guid.Empty) errors.Add("Selecciona una categoría.");
        if (IsAchievement && model.CollectionId != Guid.Empty && model.TierOrder is not > 0)
            errors.Add("Indica un orden de nivel entero mayor que cero para la colección.");
        if (!IsAchievement)
        {
            if (string.IsNullOrWhiteSpace(model.Instructions)) errors.Add("Indica las instrucciones del desafío.");
            if (model.StartDate is null || model.EndDate is null) errors.Add("Indica las fechas de inicio y fin.");
            else if (model.EndDate <= model.StartDate) errors.Add("La fecha de fin debe ser posterior al inicio.");
        }
        errors.AddRange(ValidateConditions());
        if (errors.Count > 0) return;
        var conditionList = BuildConditions();
        var sendConditions = !ConditionLocked && (!IsEdit || ConditionsChanged);
        saving = true;
        var sent = false;
        var saved = false;
        try
        {
            if (IsAchievement)
            {
                using var image = imageBytes is null ? null : new MemoryStream(imageBytes);
                if (Achievement is { } a)
                {
                    var request = new AchievementUpdateRequest
                    {
                        Id = a.Id, Name = Changed(model.Name.Trim(), a.Name), Description = Changed(model.Description, a.Description),
                        Points = Changed(model.Points, a.Points), Active = Changed(model.Active, a.Active), IsSecret = Changed(model.IsSecret, a.IsSecret),
                        AchievementCategoryId = Changed(model.CategoryId, a.AchievementCategoryId),
                        Conditions = sendConditions ? conditionList : null
                    };
                    if (model.CollectionId == Guid.Empty && a.AchievementCollectionId.HasValue)
                        request.ClearCollection = true;
                    else if (model.CollectionId != Guid.Empty &&
                        (a.AchievementCollectionId != model.CollectionId || a.TierOrder != model.TierOrder))
                    {
                        request.AchievementCollectionId = model.CollectionId;
                        request.TierOrder = model.TierOrder;
                    }
                    if (!HasChanges(request) && image is null) { error = "No hay cambios para guardar."; return; }
                    sent = true;
                    var response = RequireSaved(await Service.UpdateAchievementAsync(request, image, imageName, imageType, lifetime.Token));
                    if (response.Id != a.Id) throw new JsonException("Respuesta de actualización inesperada.");
                }
                else
                {
                    sent = true;
                    var response = RequireSaved(await Service.CreateAchievementAsync(new()
                    {
                        Name = model.Name.Trim(), Description = model.Description, Points = model.Points, Active = model.Active,
                        IsSecret = model.IsSecret, AchievementCategoryId = model.CategoryId, Conditions = conditionList,
                        AchievementCollectionId = model.CollectionId == Guid.Empty ? null : model.CollectionId,
                        TierOrder = model.CollectionId == Guid.Empty ? null : model.TierOrder
                    }, image, imageName, imageType, lifetime.Token));
                    if (response.Id == Guid.Empty) throw new JsonException("Respuesta de creación inesperada.");
                }
            }
            else if (Challenge is { } c)
            {
                var request = new ChallengeUpdateRequest
                {
                    Id = c.Id, Title = Changed(model.Name.Trim(), c.Title), Description = Changed(model.Description, c.Description),
                    Instructions = Changed(model.Instructions, c.Instructions), Difficulty = Changed(model.Difficulty, c.Difficulty),
                    Points = Changed(model.Points, c.Points), Active = Changed(model.Active, c.Active),
                    StartDate = model.StartDate == c.StartDate ? null : BoliviaWallTime(model.StartDate), EndDate = model.EndDate == c.EndDate ? null : BoliviaWallTime(model.EndDate),
                    Status = Changed(model.Status, c.Status),
                    Conditions = sendConditions ? conditionList : null
                };
                if (!HasChanges(request)) { error = "No hay cambios para guardar."; return; }
                sent = true;
                var response = RequireSaved(await Service.UpdateChallengeAsync(request, lifetime.Token));
                if (response.Id != c.Id) throw new JsonException("Respuesta de actualización inesperada.");
            }
            else
            {
                sent = true;
                var response = RequireSaved(await Service.CreateChallengeAsync(new()
                {
                    Title = model.Name.Trim(), Description = model.Description, Instructions = model.Instructions, Difficulty = model.Difficulty,
                    Points = model.Points, Active = model.Active, StartDate = BoliviaWallTime(model.StartDate)!.Value, EndDate = BoliviaWallTime(model.EndDate)!.Value,
                    Status = model.Status, Conditions = conditionList
                }, lifetime.Token));
                if (response.Id == Guid.Empty) throw new JsonException("Respuesta de creación inesperada.");
            }
            saved = true;
        }
        catch (HttpRequestException ex)
        {
            if (!disposed)
            {
                uncertain = sent && (ex.StatusCode is null || (int)ex.StatusCode >= 500 || ex.StatusCode == HttpStatusCode.RequestTimeout);
                error = uncertain ? UncertainMessage : HttpError(ex);
            }
        }
        catch (OperationCanceledException) { if (!disposed) { uncertain = sent; error = sent ? UncertainMessage : "Se canceló la operación."; } }
        catch (JsonException) { if (!disposed) { uncertain = sent; error = UncertainMessage; } }
        catch (InvalidOperationException ex) { if (!disposed) error = ex.Message; }
        finally { saving = false; }
        if (saved && !disposed) await OnSaved.InvokeAsync();
    }
    private const string UncertainMessage = "No se pudo confirmar el resultado. Cierra esta ventana y consulta el listado antes de volver a guardar.";
    private static T RequireSaved<T>(GrammarErrorDetection.Models.Responses.ApiResponse<T>? response) where T : class
    {
        if (response?.Error is { ErrorCode: not 0 } error)
            throw new InvalidOperationException(error.ErrorMessage ?? "La API rechazó la operación.");
        return response?.Data ?? throw new JsonException("No se recibió confirmación del guardado.");
    }
    private string HttpError(HttpRequestException ex)
    {
        if (ex.StatusCode == HttpStatusCode.Unauthorized) { Session.Clear(); Navigation.NavigateTo("/auth/login", replace: true); return "La sesión expiró."; }
        return ex.StatusCode switch
        {
            HttpStatusCode.Forbidden => "No tienes permiso para realizar esta operación.",
            HttpStatusCode.BadRequest => IsAchievement && IsEdit
                ? "La API rechazó los cambios. Si alguien ya obtuvo este logro, no se pueden modificar sus condiciones. Revisa los datos o crea otro logro."
                : "La API rechazó los datos. Revisa los campos y las condiciones.",
            HttpStatusCode.Conflict => "Los datos cambiaron o entran en conflicto. Cierra y actualiza el listado.",
            _ => "No se pudo completar la solicitud."
        };
    }
    private static string? Changed(string value, string? original) => value == original ? null : value;
    // El formulario contiene horas de Bolivia; la API espera TIMESTAMP sin zona.
    private static DateTime? BoliviaWallTime(DateTime? value) => value.HasValue
        ? DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified) : null;
    private static T? Changed<T>(T value, T original) where T : struct => EqualityComparer<T>.Default.Equals(value, original) ? null : value;
    private static bool HasChanges<T>(T value) => JsonSerializer.SerializeToElement(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)).EnumerateObject().Count() > 1;
    public void Dispose() { disposed = true; lifetime.Cancel(); lifetime.Dispose(); }

    private sealed class ConditionRow
    {
        public Guid TypeId { get; set; }
        public string Operator { get; set; } = ">=";
        public decimal Target { get; set; } = 1;
        public decimal? MinimumWords { get; set; }
        public decimal? MinimumScore { get; set; }
    }

    private sealed class EditorModel
    {
        [Required(ErrorMessage = "Indica el nombre o título.")] public string Name { get; set; } = "";
        [Required(ErrorMessage = "Indica una descripción.")] public string Description { get; set; } = "";
        [Range(0, int.MaxValue, ErrorMessage = "Los puntos deben ser cero o mayores.")] public int Points { get; set; } = 20;
        public bool Active { get; set; } = true;
        public bool IsSecret { get; set; }
        public Guid CategoryId { get; set; }
        public Guid CollectionId { get; set; }
        public int? TierOrder { get; set; }
        public string Instructions { get; set; } = "";
        [Required(ErrorMessage = "Indica la dificultad.")] public string Difficulty { get; set; } = "Normal";
        public string Status { get; set; } = "Draft";
        public DateTime? StartDate { get; set; } = BoliviaWallTime(DateTime.UtcNow.AddHours(-4).Date);
        public DateTime? EndDate { get; set; } = BoliviaWallTime(DateTime.UtcNow.AddHours(-4).Date.AddDays(7).AddSeconds(-1));
    }
}

