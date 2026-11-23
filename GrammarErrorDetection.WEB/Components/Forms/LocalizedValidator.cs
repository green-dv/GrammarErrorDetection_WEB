using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GrammarErrorDetection.WEB.Components.Forms;

/// <summary>
/// Sustituye a <c>DataAnnotationsValidator</c>: valida con DataAnnotations y traduce cada mensaje
/// (los <c>ErrorMessage</c> de los modelos están en español y no pueden llamar a código).
/// </summary>
public sealed class LocalizedValidator : ComponentBase, IDisposable
{
    private ValidationMessageStore? store;

    [CascadingParameter] private EditContext? Context { get; set; }

    protected override void OnInitialized()
    {
        if (Context is null) throw new InvalidOperationException(I18n.T("{0} requiere un EditContext.", nameof(LocalizedValidator)));
        store = new ValidationMessageStore(Context);
        Context.OnValidationRequested += ValidateModel;
        Context.OnFieldChanged += ValidateField;
    }

    private void ValidateModel(object? sender, ValidationRequestedEventArgs args)
    {
        var model = Context!.Model;
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        store!.Clear();
        foreach (var result in results)
        {
            var message = I18n.T(result.ErrorMessage ?? string.Empty);
            var members = result.MemberNames.Any() ? result.MemberNames : [string.Empty];
            foreach (var member in members) store.Add(new FieldIdentifier(model, member), message);
        }
        Context.NotifyValidationStateChanged();
    }

    private void ValidateField(object? sender, FieldChangedEventArgs args)
    {
        var field = args.FieldIdentifier;
        var property = field.Model.GetType().GetProperty(field.FieldName);
        if (property is null) return;
        var results = new List<ValidationResult>();
        var context = new ValidationContext(field.Model) { MemberName = field.FieldName };
        Validator.TryValidateProperty(property.GetValue(field.Model), context, results);
        store!.Clear(field);
        foreach (var result in results) store.Add(field, I18n.T(result.ErrorMessage ?? string.Empty));
        Context!.NotifyValidationStateChanged();
    }

    public void Dispose()
    {
        if (Context is null) return;
        Context.OnValidationRequested -= ValidateModel;
        Context.OnFieldChanged -= ValidateField;
        store?.Clear();
        Context.NotifyValidationStateChanged();
    }
}
