using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Lädt den Anwendungskatalog und startet dessen sprachgesteuerte Auswahl in der
/// gemeinsamen Laufzeit. Ein bestätigter Sprachwechsel lädt den Dialog in der neuen Sprache neu.
/// </summary>
internal sealed class FormsCatalogDialog(
    SelectionDialogSpeechTurnRunner speech,
    IFormsCatalogDataClient mdaCatalogClient,
    IFormsCatalogIntentParser intentParser,
    FormsCatalogLanguageChangeWorkflow languageChangeWorkflow,
    IUserLanguageState userLanguageState,
    ISpeechAssistantTrace speechAssistantTrace) : IFormsCatalogDialog
{
    private readonly IFormsCatalogDataClient _mdaCatalogClient = mdaCatalogClient ?? throw new ArgumentNullException(nameof(mdaCatalogClient));
    private readonly ISpeechAssistantTrace _speechAssistantTrace = speechAssistantTrace ?? throw new ArgumentNullException(nameof(speechAssistantTrace));
    private readonly FormsCatalogDialogSessionFactory _sessionFactory = new(userLanguageState ?? throw new ArgumentNullException(nameof(userLanguageState)));
    private readonly FormsCatalogInitialTranscriptResolver _initialTranscriptResolver = new(
        intentParser ?? throw new ArgumentNullException(nameof(intentParser)),
        new ApplicationMatcher());
    private readonly FormsCatalogDialogRuntimeRunner _runtimeRunner = CreateRuntimeRunner(
        speech ?? throw new ArgumentNullException(nameof(speech)),
        intentParser ?? throw new ArgumentNullException(nameof(intentParser)),
        languageChangeWorkflow ?? throw new ArgumentNullException(nameof(languageChangeWorkflow)),
        speechAssistantTrace ?? throw new ArgumentNullException(nameof(speechAssistantTrace)));

    /// <inheritdoc />
    public async Task<FormsCatalogDialogResult> ExecuteAsync(string languageCode, CancellationToken cancellationToken)
        => await ExecuteAsync(languageCode, Guid.NewGuid().ToString("N"), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<FormsCatalogDialogResult> ExecuteAsync(
        string languageCode,
        string correlationId,
        CancellationToken cancellationToken)
        => await ExecuteCoreAsync(languageCode, correlationId, initialTranscript: null, cancellationToken).ConfigureAwait(false);

    public async Task<FormsCatalogDialogResult> ExecuteAsync(
        string languageCode,
        string correlationId,
        SpeechTranscript initialTranscript,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(initialTranscript);
        ArgumentException.ThrowIfNullOrWhiteSpace(initialTranscript.Text);
        return await ExecuteCoreAsync(languageCode, correlationId, initialTranscript, cancellationToken).ConfigureAwait(false);
    }

    private async Task<FormsCatalogDialogResult> ExecuteCoreAsync(
        string languageCode,
        string correlationId,
        SpeechTranscript? initialTranscript,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await _speechAssistantTrace
            .WriteIfConfiguredAsync(
                correlationId,
                SpeechAssistantTraceScope.FormsCatalogDialog,
                SpeechAssistantTraceLevel.Information,
                initialTranscript is null
                    ? "Application catalog dialog started."
                    : "Application catalog dialog started with a prefetched transcript.",
                cancellationToken)
            .ConfigureAwait(false);

        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(languageCode);
        if (normalizedIsoCode is null)
        {
            await _speechAssistantTrace
                .WriteIfConfiguredAsync(
                    correlationId,
                    SpeechAssistantTraceScope.FormsCatalogDialog,
                    SpeechAssistantTraceLevel.Warning,
                    "Application catalog dialog rejected an invalid language code.",
                    cancellationToken)
                .ConfigureAwait(false);
            throw new ArgumentException("Language code must be a valid ISO 639-1 code.", nameof(languageCode));
        }

        FormsCatalog catalog;
        try
        {
            catalog = await _mdaCatalogClient.GetFormsCatalogAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await _speechAssistantTrace
                .WriteIfConfiguredAsync(
                    correlationId,
                    SpeechAssistantTraceScope.FormsCatalogDialog,
                    SpeechAssistantTraceLevel.Error,
                    "Application catalog dialog failed while loading the forms catalog.",
                    SelectionDialogTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            throw;
        }

        var session = _sessionFactory.Create(catalog, normalizedIsoCode);
        var initialCatalogTranscript = _initialTranscriptResolver.Resolve(
            initialTranscript,
            catalog,
            session.Applications,
            normalizedIsoCode,
            session.PolitenessLevel);
        await _speechAssistantTrace
            .WriteIfConfiguredAsync(
                correlationId,
                SpeechAssistantTraceScope.FormsCatalogDialog,
                SpeechAssistantTraceLevel.Information,
                $"Application catalog dialog loaded catalog. visibleApplications={session.Applications.Count}",
                cancellationToken)
            .ConfigureAwait(false);
        if (initialTranscript is not null)
        {
            await _speechAssistantTrace
                .WriteIfConfiguredAsync(
                    correlationId,
                    SpeechAssistantTraceScope.FormsCatalogDialog,
                    SpeechAssistantTraceLevel.Information,
                    initialCatalogTranscript is null
                        ? "Application catalog dialog discarded the prefetched transcript because it was not actionable."
                        : "Application catalog dialog accepted the prefetched transcript for the first listening turn.",
                    cancellationToken)
                .ConfigureAwait(false);
        }
        var result = await _runtimeRunner
            .RunAsync(session, correlationId, initialCatalogTranscript, cancellationToken)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(result.RestartIsoCode))
        {
            return await ExecuteCoreAsync(
                    result.RestartIsoCode,
                    correlationId,
                    initialTranscript: null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return result.Result;
    }

    private static FormsCatalogDialogRuntimeRunner CreateRuntimeRunner(
        SelectionDialogSpeechTurnRunner speech,
        IFormsCatalogIntentParser intentParser,
        FormsCatalogLanguageChangeWorkflow languageChangeWorkflow,
        ISpeechAssistantTrace speechAssistantTrace)
    {
        var languageChangeHandler = new FormsCatalogDialogLanguageChangeHandler(
            speech,
            intentParser,
            languageChangeWorkflow,
            speechAssistantTrace);

        var intentRouter = new FormsCatalogDialogIntentRouter(intentParser, languageChangeHandler.HandleAsync);
        return new FormsCatalogDialogRuntimeRunner(speech, intentParser, intentRouter, speechAssistantTrace);
    }
}
