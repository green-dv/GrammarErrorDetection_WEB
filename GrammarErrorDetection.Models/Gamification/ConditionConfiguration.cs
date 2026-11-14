namespace GrammarErrorDetection.Models.Gamification;

/// <summary>Configuración de las actividades implementadas por el motor de gamificación.</summary>
public static class ConditionConfiguration
{
    public static bool IsSupported(string? code) => code is
        "TEXTS_ANALYZED" or "ERROR_FREE_TEXTS" or "PRACTICE_DAYS" or "PRACTICE_STREAK" or
        "TEXT_IMPROVEMENTS" or "EVALUATIONS_PASSED" or "EVALUATIONS_ABOVE_SCORE" or "COURSES_PASSED";

    public static string Description(string? code) => code switch
    {
        "TEXTS_ANALYZED" => "Cantidad de textos analizados correctamente.",
        "ERROR_FREE_TEXTS" => "Textos analizados sin errores que cumplen el mínimo de palabras.",
        "PRACTICE_DAYS" => "Días distintos con análisis completados, según la fecha de Bolivia.",
        "PRACTICE_STREAK" => "Mayor cantidad de días consecutivos de práctica.",
        "TEXT_IMPROVEMENTS" => "Análisis con menor proporción de errores que el anterior de la misma inscripción.",
        "EVALUATIONS_PASSED" => "Evaluaciones completas con nota mayor o igual a 70. La nota de aprobación es fija.",
        "EVALUATIONS_ABOVE_SCORE" => "Evaluaciones completas con nota estrictamente mayor que la nota indicada.",
        "COURSES_PASSED" => "Módulos distintos aprobados con dos períodos completos y promedio mayor o igual a 70. Estos requisitos son fijos.",
        _ => "Esta actividad no está admitida por la configuración actual."
    };

    public static List<string> Validate(string? code, decimal target, string comparisonOperator, decimal? minimumWords, decimal? minimumScore)
    {
        var errors = new List<string>();
        if (!IsSupported(code)) errors.Add("Selecciona una de las ocho actividades implementadas.");
        if (target <= 0 || decimal.Truncate(target) != target) errors.Add("La meta debe ser un entero positivo.");
        if (comparisonOperator is not (">=" or ">" or "=")) errors.Add("El operador de cantidad debe ser >=, > o =.");
        if (code == "ERROR_FREE_TEXTS" && minimumWords is { } words && (words <= 0 || decimal.Truncate(words) != words))
            errors.Add("El mínimo de palabras debe ser un entero positivo.");
        if (code == "EVALUATIONS_ABOVE_SCORE" && minimumScore is null)
            errors.Add("Indica la nota que las evaluaciones deben superar.");
        return errors;
    }

    public static string OperatorSymbol(string? comparisonOperator) => comparisonOperator switch
    {
        ">=" => "≥", ">" => ">", "=" => "=", null or "" => "≥", _ => comparisonOperator
    };

    /// <summary>"Textos sin errores ≥ 3 (mín. 20 palabras)".</summary>
    public static string Summary(GamificationConditionResponse condition)
    {
        var name = condition.ConditionName ?? condition.ConditionCode ?? "Condición";
        var text = $"{name} {OperatorSymbol(condition.ComparisonOperator)} {condition.TargetValue:0.##}";
        var extra = ReadNumber(condition.ConditionParameters, "MinimumWords") is { } words ? $" (mín. {words:0.##} palabras)"
            : ReadNumber(condition.ConditionParameters, "MinimumScore") is { } score ? $" (nota > {score:0.##})" : "";
        return text + extra;
    }

    /// <summary>Todas las condiciones: se deben cumplir todas ("… y …").</summary>
    public static string Summary(IReadOnlyCollection<GamificationConditionResponse>? conditions, string empty = "Sin condiciones")
        => conditions is not { Count: > 0 } ? empty : string.Join(" y ", conditions.Select(Summary));

    /// <summary>Lee un número de ConditionParameters (llega como JsonElement al deserializar).</summary>
    public static decimal? ReadNumber(Dictionary<string, object?>? parameters, string key)
    {
        if (parameters is null || !parameters.TryGetValue(key, out var value) || value is null) return null;
        if (value is System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Number } json && json.TryGetDecimal(out var number)) return number;
        return value is IConvertible convertible ? convertible.ToDecimal(System.Globalization.CultureInfo.InvariantCulture) : null;
    }

    public static Dictionary<string, object?> BuildParameters(string? code, decimal? minimumWords, decimal? minimumScore)
    {
        if (!IsSupported(code)) throw new ArgumentException("Actividad no implementada.", nameof(code));
        var parameters = new Dictionary<string, object?>();
        if (code == "ERROR_FREE_TEXTS" && minimumWords is { } words) parameters["MinimumWords"] = words;
        if (code == "EVALUATIONS_ABOVE_SCORE")
            parameters["MinimumScore"] = minimumScore ?? throw new ArgumentException("La nota a superar es obligatoria.", nameof(minimumScore));
        return parameters;
    }
}
