namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet ein auswählbares Fachobjekt mit Kennung, Anzeigenamen, stabiler Nummer
/// und Revision, damit Auswahlergebnisse ihrem Kandidatenkontext zugeordnet bleiben.
/// </summary>
public sealed record SelectionCandidate<TSelection>
    where TSelection : class
{
    private SelectionCandidate(
        TSelection item,
        string identifier,
        string displayName,
        int stableNumber,
        SelectionRevision revision)
    {
        Item = item;
        Identifier = identifier;
        DisplayName = displayName;
        StableNumber = stableNumber;
        Revision = revision;
    }

    public TSelection Item { get; }

    /// <summary>
    /// Stellt die stabile fachliche Kennung bereit.
    /// </summary>
    public string Identifier { get; }

    public string DisplayName { get; }

    /// <summary>
    /// Stellt die positive, vom Aufrufer zugeordnete Kandidatennummer bereit.
    /// </summary>
    public int StableNumber { get; }

    /// <summary>
    /// Stellt die Revision des zugehörigen Kandidatenbestands bereit.
    /// </summary>
    public SelectionRevision Revision { get; }

    /// <summary>
    /// Erzeugt einen Kandidaten mit geprüften Pflichtangaben und positiver Nummer. Die Zugehörigkeit
    /// zu einem Katalog wird hier nicht geprüft.
    /// </summary>
    public static SelectionCandidate<TSelection> Create(
        TSelection item,
        string identifier,
        string displayName,
        int stableNumber,
        SelectionRevision revision)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(revision);

        if (stableNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stableNumber),
                stableNumber,
                "A candidate number must be positive.");
        }

        return new SelectionCandidate<TSelection>(item, identifier, displayName, stableNumber, revision);
    }
}
