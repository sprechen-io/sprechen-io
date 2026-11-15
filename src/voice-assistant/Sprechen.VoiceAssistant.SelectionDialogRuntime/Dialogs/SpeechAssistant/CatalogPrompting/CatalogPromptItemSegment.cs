namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant.CatalogPrompting;

/// <summary>
/// Verbindet den Vorlagenschlüssel eines Listeneintrags mit der Funktion, die seine
/// Parameter erzeugt. So können verschiedene Kandidatentypen denselben Aufbau nummerierter Ansagen nutzen.
/// </summary>
internal sealed record CatalogPromptItemSegment<TItem>
{
    public CatalogPromptItemSegment(
        string promptId,
        Func<int, TItem, IReadOnlyDictionary<string, string?>> templateParametersFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentNullException.ThrowIfNull(templateParametersFactory);

        PromptId = promptId;
        TemplateParametersFactory = templateParametersFactory;
    }

    public string PromptId { get; }

    public Func<int, TItem, IReadOnlyDictionary<string, string?>> TemplateParametersFactory { get; }
}
