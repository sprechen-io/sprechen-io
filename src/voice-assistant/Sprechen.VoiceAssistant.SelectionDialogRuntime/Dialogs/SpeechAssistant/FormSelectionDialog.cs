using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bereitet die sprachgesteuerte Formularauswahl einer Anwendung vor und übergibt sie
/// an die gemeinsame Auswahllaufzeit. Formularmodell und Dialogsteuerung bleiben getrennt.
/// </summary>
internal sealed class FormSelectionDialog(
    SelectionDialogSpeechTurnRunner speech,
    IFormsCatalogDataClient mdaCatalogClient,
    IFormsCatalogIntentParser intentParser,
    IFormSelectionMatcher matcher,
    IUserLanguageState userLanguageState,
    ISpeechAssistantTrace speechAssistantTrace) : IFormSelectionDialog
{
    private readonly IFormsCatalogDataClient _mdaCatalogClient = mdaCatalogClient ?? throw new ArgumentNullException(nameof(mdaCatalogClient));
    private readonly FormSelectionDialogSessionFactory _sessionFactory = new(userLanguageState ?? throw new ArgumentNullException(nameof(userLanguageState)));
    private readonly FormSelectionDialogRuntimeRunner _runtimeRunner = new(
        speech ?? throw new ArgumentNullException(nameof(speech)),
        intentParser ?? throw new ArgumentNullException(nameof(intentParser)),
        matcher ?? throw new ArgumentNullException(nameof(matcher)),
        speechAssistantTrace ?? throw new ArgumentNullException(nameof(speechAssistantTrace)));

    /// <inheritdoc />
    public async Task<FormSelectionDialogResult> ExecuteAsync(
        string languageCode,
        string applicationId,
        IReadOnlyCollection<string> completedFormIds,
        CancellationToken cancellationToken)
        => await ExecuteAsync(
                languageCode,
                applicationId,
                Guid.NewGuid().ToString("N"),
                completedFormIds,
                cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<FormSelectionDialogResult> ExecuteAsync(
        string languageCode,
        string applicationId,
        string correlationId,
        IReadOnlyCollection<string> completedFormIds,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(completedFormIds);

        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(languageCode);
        if (normalizedIsoCode is null)
        {
            throw new ArgumentException("Language code must be a valid ISO 639-1 code.", nameof(languageCode));
        }

        var catalog = await _mdaCatalogClient.GetFormsCatalogAsync(cancellationToken).ConfigureAwait(false);
        var session = _sessionFactory.Create(catalog, applicationId, normalizedIsoCode, completedFormIds);
        return await _runtimeRunner.RunAsync(session, correlationId, cancellationToken).ConfigureAwait(false);
    }
}
