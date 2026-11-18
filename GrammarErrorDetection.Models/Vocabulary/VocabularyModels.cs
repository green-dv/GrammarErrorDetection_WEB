namespace GrammarErrorDetection.Models.Vocabulary;

/// <summary>Verificación de vocabulario de un párrafo: por oración, las palabras con error y sus alternativas.</summary>
public sealed class VocabularyAnalysisResponse
{
    public Guid TextId { get; set; }
    public List<VocabularySentenceResponse> Sentences { get; set; } = [];
    public int IssueCount { get; set; }

    /// <summary>Palabras distintas que el servicio externo no pudo verificar; volver a consultar completa la verificación.</summary>
    public int UncheckedCount { get; set; }

    public string Source { get; set; } = "Datamuse";
}

public sealed class VocabularySentenceResponse
{
    public Guid SentenceId { get; set; }
    public int SentenceIndex { get; set; }
    public string OriginalSentence { get; set; } = "";
    public List<VocabularyIssueResponse> Issues { get; set; } = [];
    public int UncheckedCount { get; set; }
}

/// <summary>Palabra con error. Los índices son posiciones de la oración; el final es exclusivo.</summary>
public sealed class VocabularyIssueResponse
{
    public string Token { get; set; } = "";
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public List<VocabularyAlternativeResponse> Alternatives { get; set; } = [];
}

public sealed class VocabularyAlternativeResponse
{
    public string Word { get; set; } = "";
    public List<VocabularyDefinitionResponse> Definitions { get; set; } = [];
}

public sealed class VocabularyDefinitionResponse
{
    /// <summary>noun, verb, adjective o adverb; nulo si no se conoce.</summary>
    public string? PartOfSpeech { get; set; }

    public string Text { get; set; } = "";
}
