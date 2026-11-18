using System.Globalization;

namespace GrammarErrorDetection.WEB.Components.Reports;

/// <summary>Formato de cifras y tendencias de los reportes gramaticales (compartido por el tablero y sus fichas).</summary>
public static class GrammarFormat
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

    public static string Number(decimal? value, string format = "0.00") => value?.ToString(format, Spanish) ?? "—";

    public static string Signed(decimal? value) => value.HasValue ? value.Value.ToString("+0.##;-0.##;0", Spanish) : "Sin comparación";

    public static string ChangeWithUnit(decimal? value, string unit, bool fractional = false)
        => value.HasValue ? $"{(fractional ? value.Value.ToString("+0.00;-0.00;0.00", Spanish) : Signed(value))} {unit}" : "Sin comparación";

    public static string RelativeChange(decimal? value) => value.HasValue ? $"{value.Value.ToString("+0.00;-0.00;0.00", Spanish)} %" : "Sin base porcentual";

    public static string TrendLabel(string trend) => trend switch
    {
        "Improved" => "Mejora", "Worsened" => "Aumenta la dificultad", "Unchanged" => "Sin cambios", _ => "Sin datos comparables"
    };

    public static string TrendClass(string trend) => trend switch
    {
        "Improved" => "improved", "Worsened" => "worsened", "Unchanged" => "unchanged", _ => "insufficient"
    };

    /// <summary>Porcentaje 0–100 para un ancho CSS, acotado.</summary>
    public static string Share(decimal? percentage) => Math.Clamp(percentage ?? 0m, 0m, 100m).ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Valor como porcentaje del máximo, para un ancho CSS.</summary>
    public static string Percent(long value, long maximum) => (100m * value / maximum).ToString("0.##", CultureInfo.InvariantCulture);
}
