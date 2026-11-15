using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Erkennt einen modellierten Öffnungsbefehl und übergibt dessen Zielnamen an die
/// Auswahlbehandlung. Die Befehlsform bleibt dadurch vom freien Kandidatentext unterscheidbar.
/// </summary>
internal sealed class FormsCatalogOpenCandidateIntentClassifier
{
    private readonly IFormsCatalogIntentMatcher _matcher;

    public FormsCatalogOpenCandidateIntentClassifier(IFormsCatalogIntentMatcher matcher)
    {
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
    }

    public FormsCatalogIntentResult? TryClassify(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var targetName = _matcher.TryExtractOpenCandidateTarget(
            catalog,
            normalized,
            isoCode,
            politenessLevel);
        return targetName is null
            ? null
            : new FormsCatalogIntentResult(FormsCatalogIntent.OpenCandidate, TargetName: targetName);
    }
}
