using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.Speech.ModelRuntime.PromptTemplates;

/// <summary>
/// Liefert getrennte Rückmeldungen für wiederholbare Aufnahmefehler und nicht verfügbare Erkennung. Der Aufnahmeablauf entscheidet über den passenden Fehlerfall.
/// </summary>
public static class RecoverableCapturePromptTextResolver
{
    public static string GetRecoverableFailurePrompt(string isoCode)
        => GetRecoverableFailurePrompt(isoCode, PolitenessLevel.Formal);

    /// <summary>
    /// Löst die modellierte Ansage für nicht verfügbare Spracherkennung auf.
    /// Sie beschreibt das technische Hindernis, statt fehlendes Verständnis
    /// der gesprochenen Antwort zuzuschreiben.
    /// </summary>
    public static string GetRecognizerUnavailablePrompt(string isoCode, PolitenessLevel politenessLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        if (!VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(
                FormDialogPromptIds.CaptureRecognizerUnavailable,
                isoCode,
                politenessLevel,
                out var template))
        {
            throw new InvalidOperationException(
                $"Prompt template '{FormDialogPromptIds.CaptureRecognizerUnavailable}' is missing for '{isoCode}'.");
        }

        return template;
    }

    public static string GetRecoverableFailurePrompt(string isoCode, PolitenessLevel politenessLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        if (!VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(
                FormDialogPromptIds.CaptureRecoverableFailure,
                isoCode,
                politenessLevel,
                out var template))
        {
            throw new InvalidOperationException(
                $"Prompt template '{FormDialogPromptIds.CaptureRecoverableFailure}' is missing for '{isoCode}'.");
        }

        return template;
    }
}
