using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant.CatalogPrompting;

/// <summary>
/// Beschreibt Einleitung, Einträge und Abschluss einer nummerierten Ansage samt Sprache
/// und Anrede. Daraus lassen sich sowohl ein zusammenhängender Text als auch einzelne Abschnitte aufbauen.
/// </summary>
internal sealed record CatalogNumberedSequencePromptRequest<TItem>
{
    public CatalogNumberedSequencePromptRequest(
        CatalogPromptContext promptContext,
        IReadOnlyList<TItem> items,
        string isoCode,
        PolitenessLevel politenessLevel,
        CatalogPromptSegment intro,
        CatalogPromptItemSegment<TItem> item,
        CatalogPromptSegment? trailing = null)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(intro);
        ArgumentNullException.ThrowIfNull(item);

        PromptContext = promptContext;
        Items = items;
        IsoCode = isoCode;
        PolitenessLevel = politenessLevel;
        Intro = intro;
        Item = item;
        Trailing = trailing;
    }

    public CatalogPromptContext PromptContext { get; }

    public IReadOnlyList<TItem> Items { get; }

    public string IsoCode { get; }

    public PolitenessLevel PolitenessLevel { get; }

    public CatalogPromptSegment Intro { get; }

    public CatalogPromptItemSegment<TItem> Item { get; }

    public CatalogPromptSegment? Trailing { get; }
}
