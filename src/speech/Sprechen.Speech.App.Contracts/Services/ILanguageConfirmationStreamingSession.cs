namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Ergänzt den Sitzungslebenszyklus um das abrufbare Ergebnis einer gestreamten Sprachbestätigung.
/// </summary>
public interface ILanguageConfirmationStreamingSession : IStreamingSession
{
    Task<LanguageConfirmationStreamingResult> GetResultAsync(CancellationToken cancellationToken);
}


