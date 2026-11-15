using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bereitet die übergebenen Anzeigenamen von Feldoptionen als begrenzte
/// Erkennungshinweise auf. Der Feldtyp wird hier nicht geprüft. Ohne
/// Optionen entstehen keine zusätzlichen Begriffe.
/// </summary>
internal sealed class FieldChoiceKeytermVocabulary
{
    private readonly SpeechKeytermProjectionPolicy _projectionPolicy = new();

    /// <summary>
    /// Liefert die vom gemeinsamen Projektor begrenzte, bereinigte und deduplizierte
    /// Folge der Optionsnamen in ihrer Eingangsreihenfolge.
    /// </summary>
    public IReadOnlyList<string> GetKeyterms(IReadOnlyList<FormChoiceOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return _projectionPolicy.Project(options.Select(static option => option?.DisplayName));
    }
}
