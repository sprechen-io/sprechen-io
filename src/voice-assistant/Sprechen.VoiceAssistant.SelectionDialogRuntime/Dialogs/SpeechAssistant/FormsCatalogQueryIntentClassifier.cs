using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet modellierte Such- und Beschreibungspräfixe ihren jeweiligen Nutzdaten zu. Die spätere
/// Katalogbehandlung erhält damit entweder eine Suchanfrage oder einen zu beschreibenden Namen.
/// </summary>
internal sealed class FormsCatalogQueryIntentClassifier
{
    private readonly IFormsCatalogIntentMatcher _matcher;

    public FormsCatalogQueryIntentClassifier(IFormsCatalogIntentMatcher matcher)
    {
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
    }

    public FormsCatalogIntentResult? TryClassify(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var searchQuery = _matcher.TryExtractSearchQuery(catalog, normalized, isoCode, politenessLevel);
        if (searchQuery is not null)
        {
            return new FormsCatalogIntentResult(FormsCatalogIntent.Search, Query: searchQuery);
        }

        var describeTarget = _matcher.TryExtractDescribeTarget(catalog, normalized, isoCode, politenessLevel);
        return describeTarget is null
            ? null
            : new FormsCatalogIntentResult(FormsCatalogIntent.Describe, TargetName: describeTarget);
    }
}
