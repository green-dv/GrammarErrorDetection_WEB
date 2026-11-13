namespace GrammarErrorDetection.Models.Materials;

// Respuesta de GET api/SupportMaterial/recommendations: una clasificación de error del estudiante
// con el material recomendado para reforzarla, ordenada por Rank (1 = la más frecuente).
public sealed class SupportMaterialRecommendationResponse
{
    public Guid ErrorClassificationId { get; set; }
    public string ErrorClassificationName { get; set; } = "";
    public string? ErrorClassificationDescription { get; set; }
    public long ErrorCount { get; set; }
    public long RelatedTextCount { get; set; }
    public decimal? ErrorPercentage { get; set; }
    public DateTime? LastErrorAt { get; set; }
    public int Rank { get; set; }
    public List<RecommendedMaterialResponse> Materials { get; set; } = [];
}

public sealed class RecommendedMaterialResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid ErrorClassificationId { get; set; }
    public DateTime UploadDate { get; set; }
    public string FileLink { get; set; } = "";
    public Guid FileTypeId { get; set; }
    public long? FileSize { get; set; }
    public Guid UploadedBy { get; set; }
    public string UploadedByName { get; set; } = "";
    public string FileTypeName { get; set; } = "";
    public string FileExtension { get; set; } = "";
    public string MimeType { get; set; } = "";
    public int Rank { get; set; }
    // Personalización: si ya lo abrió, qué respondió a "¿Te sirvió?" (null = sin respuesta),
    // el puntaje 0..1 y el motivo principal de la recomendación.
    public bool Seen { get; set; }
    public bool? Helpful { get; set; }
    public decimal Score { get; set; }
    public string ReasonCode { get; set; } = "";
}

// Cuerpo de POST api/SupportMaterial/{id}/view.
public sealed class SupportMaterialViewRequest
{
    public Guid? RegistrationId { get; set; }
    public bool Downloaded { get; set; }
}

// Cuerpo de PUT api/SupportMaterial/{id}/feedback. El comentario solo se guarda con Helpful = false.
public sealed class SupportMaterialFeedbackRequest
{
    public bool Helpful { get; set; }
    public string? Comment { get; set; }
}

public sealed class SupportMaterialInteractionResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SupportMaterialId { get; set; }
    public Guid? RegistrationId { get; set; }
    public DateTime FirstViewedAt { get; set; }
    public DateTime LastViewedAt { get; set; }
    public int ViewCount { get; set; }
    public bool Downloaded { get; set; }
    public bool? Helpful { get; set; }
    public DateTime? FeedbackAt { get; set; }
    public string? Comment { get; set; }
}
