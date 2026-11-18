using System.Globalization;
using System.Net;
using System.Text;
using GrammarErrorDetection.Models.Texts;
using GrammarErrorDetection.Models.Vocabulary;

namespace GrammarErrorDetection.WEB.Components.Analysis;

/// <summary>Qué muestra el panel de análisis de un párrafo.</summary>
public enum AnalysisView { Grammar, Vocabulary }

/// <summary>Rango de caracteres de una oración (posiciones UTF-16, fin exclusivo).</summary>
public readonly record struct TextRange(int Start, int End);

/// <summary>Qué tipo de error marca una palabra; una misma palabra puede tener los dos.</summary>
[Flags]
public enum MarkKind { None = 0, Grammar = 1, Vocabulary = 2 }

/// <summary>Trozo de una oración con el tipo de marca que le corresponde (None si no está marcado).</summary>
public readonly record struct KindSegment(string Text, MarkKind Kind);

/// <summary>Trozo de una oración, resaltado o no.</summary>
public readonly record struct Segment(string Text, bool Marked);

/// <summary>Ayudas de presentación del panel de análisis (errores gramaticales y vocabulario).</summary>
public static class AnalysisFormat
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

    /// <summary>Nombre legible de una categoría de error del modelo: simple_present → "Simple present".</summary>
    public static string Category(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Sin clasificación";
        var text = string.Join(' ', name.Replace('_', ' ').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return char.ToUpper(text[0], CultureInfo.InvariantCulture) + text[1..];
    }

    /// <summary>Oración corregida sin los rótulos que el modelo añade ("Corrección:", "Regla: …").</summary>
    public static string CorrectionText(string? correctedSentence)
    {
        if (string.IsNullOrWhiteSpace(correctedSentence)) return "";
        var correction = WebUtility.HtmlDecode(correctedSentence).Trim();
        var marker = correction.IndexOf("Corrección:", StringComparison.OrdinalIgnoreCase);
        if (marker >= 0) correction = correction[(marker + "Corrección:".Length)..].Trim();
        foreach (var ruleMarker in new[] { "Regla:", "Rule:" })
        {
            var rule = correction.IndexOf(ruleMarker, StringComparison.OrdinalIgnoreCase);
            if (rule >= 0) correction = correction[..rule].Trim();
        }
        return correction.Trim('*', ' ', '\t', '\r', '\n');
    }

    public static string Plural(int count, string singular, string plural) => count == 1 ? singular : plural;

    /// <summary>Confianza del modelo (0–1) como porcentaje: 0,95 → "95".</summary>
    public static string Confidence(decimal score) => (score * 100m).ToString("0.#", Spanish);

    /// <summary>
    /// Los índices de error del NLP son posiciones Unicode (como en Python) con fin exclusivo; se pasan a posiciones
    /// UTF-16 para poder cortar el texto de .NET sin partir caracteres suplementarios. Los inválidos se omiten.
    /// </summary>
    public static List<TextRange> GrammarRanges(string sentence, IEnumerable<TextErrorResponse> errors)
    {
        ArgumentNullException.ThrowIfNull(sentence);
        ArgumentNullException.ThrowIfNull(errors);
        var boundaries = new List<int> { 0 };
        var offset = 0;
        foreach (var rune in sentence.EnumerateRunes())
        {
            offset += rune.Utf16SequenceLength;
            boundaries.Add(offset);
        }
        var ranges = new List<TextRange>();
        foreach (var error in errors)
            if (error.ErrorTokenStartIndex is { } start && error.ErrorTokenEndIndex is { } end
                && start >= 0 && end > start && end < boundaries.Count)
                ranges.Add(new TextRange(boundaries[start], boundaries[end]));
        return ranges;
    }

    public static List<TextRange> VocabularyRanges(IEnumerable<VocabularyIssueResponse> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        return issues.Select(issue => new TextRange(issue.StartIndex, issue.EndIndex)).ToList();
    }

    /// <summary>
    /// Parte la oración para resaltar los rangos. Los rangos inválidos o solapados se ignoran:
    /// el texto nunca se pierde ni se duplica.
    /// </summary>
    public static List<Segment> Segments(string sentence, IEnumerable<TextRange> ranges)
    {
        ArgumentNullException.ThrowIfNull(sentence);
        ArgumentNullException.ThrowIfNull(ranges);
        var parts = new List<Segment>();
        var position = 0;
        foreach (var range in ranges.OrderBy(item => item.Start))
        {
            if (range.Start < position || range.End <= range.Start || range.End > sentence.Length) continue;
            if (range.Start > position) parts.Add(new Segment(sentence[position..range.Start], false));
            parts.Add(new Segment(sentence[range.Start..range.End], true));
            position = range.End;
        }
        if (position < sentence.Length) parts.Add(new Segment(sentence[position..], false));
        return parts;
    }

    public static string PartOfSpeechLabel(string? partOfSpeech) => partOfSpeech?.ToLowerInvariant() switch
    {
        "noun" => "sustantivo",
        "verb" => "verbo",
        "adjective" => "adjetivo",
        "adverb" => "adverbio",
        _ => ""
    };

    /// <summary>Errores del párrafo: por oración, el mayor entre el conteo guardado y el detalle recibido.</summary>
    public static int ErrorCount(TextResponse analysis)
        => analysis.Sentences.Sum(sentence => Math.Max(sentence.ErrorCount, sentence.Errors.Count));

    /// <summary>Oraciones con al menos un error (el conteo guardado o el detalle recibido).</summary>
    public static int SentencesWithErrors(TextResponse analysis)
        => analysis.Sentences.Count(sentence => Math.Max(sentence.ErrorCount, sentence.Errors.Count) > 0);

    /// <summary>
    /// Superpone marcas de distinto tipo sobre una oración. Donde dos marcas se cruzan, el trozo lleva los dos tipos;
    /// los rangos inválidos se ignoran y el texto nunca se pierde ni se duplica.
    /// </summary>
    public static List<KindSegment> Overlay(string sentence, IEnumerable<(TextRange Range, MarkKind Kind)> marks)
    {
        ArgumentNullException.ThrowIfNull(sentence);
        ArgumentNullException.ThrowIfNull(marks);
        var valid = marks.Where(mark => mark.Kind != MarkKind.None && mark.Range.Start >= 0 && mark.Range.End > mark.Range.Start
            && mark.Range.End <= sentence.Length).ToList();
        var cuts = new SortedSet<int> { 0, sentence.Length };
        foreach (var (range, _) in valid) { cuts.Add(range.Start); cuts.Add(range.End); }
        var points = cuts.ToList();
        var result = new List<KindSegment>();
        for (var index = 0; index + 1 < points.Count; index++)
        {
            var (from, to) = (points[index], points[index + 1]);
            var kind = MarkKind.None;
            foreach (var (range, markKind) in valid)
                if (range.Start <= from && range.End >= to) kind |= markKind;
            var text = sentence[from..to];
            if (result.Count > 0 && result[^1].Kind == kind) result[^1] = result[^1] with { Text = result[^1].Text + text };
            else result.Add(new KindSegment(text, kind));
        }
        return result;
    }

    /// <summary>Clases CSS de una marca: gramática, vocabulario o ambas (se distinguen por color y por trazo).</summary>
    public static string MarkLabel(MarkKind kind) => kind switch
    {
        MarkKind.Grammar => "error de gramática",
        MarkKind.Vocabulary => "error de vocabulario",
        _ => "error de gramática y de vocabulario"
    };

    public static string MarkClass(MarkKind kind) => kind switch
    {
        MarkKind.Grammar => "an-mark an-mark--grammar",
        MarkKind.Vocabulary => "an-mark an-mark--vocab",
        _ => "an-mark an-mark--both"
    };
}
