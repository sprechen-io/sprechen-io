namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant.CatalogPrompting;

/// <summary>
/// Beschreibt einen modellierten Ansageabschnitt durch Prompt-Schlüssel und Parameter. Einleitung
/// und Abschluss bleiben damit unabhängig vom Typ der dazwischen aufgezählten Einträge.
/// </summary>
internal sealed record CatalogPromptSegment
{
    public CatalogPromptSegment(
        string promptId,
        IReadOnlyDictionary<string, string?>? templateParameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);

        PromptId = promptId;
        TemplateParameters = templateParameters;
    }

    public string PromptId { get; }

    public IReadOnlyDictionary<string, string?>? TemplateParameters { get; }
}
