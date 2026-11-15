using GrammarErrorDetection.Models;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IOcrService
{
    Task<OcrResponse?> RecognizeAsync(
        Stream image,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
