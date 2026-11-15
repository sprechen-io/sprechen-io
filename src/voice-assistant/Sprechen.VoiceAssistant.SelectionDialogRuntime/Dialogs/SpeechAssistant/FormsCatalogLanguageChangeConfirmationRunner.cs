using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Führt die Bestätigung eines Sprachwechsels mit einer Frage in der Zielsprache aus. Antworten
/// werden gegen Ziel- und Sitzungssprache geprüft, damit der Wechsel nicht allein an der
/// bisherigen Dialogsprache hängt.
/// </summary>
internal sealed class FormsCatalogLanguageChangeConfirmationRunner
{
    private readonly SelectionDialogSpeechTurnRunner _speech;
    private readonly IFormsCatalogIntentParser _intentParser;
    private readonly FormsCatalogLanguageChangeWorkflow _languageChangeWorkflow;
    private readonly ISpeechAssistantTrace _speechAssistantTrace;

    public FormsCatalogLanguageChangeConfirmationRunner(
        SelectionDialogSpeechTurnRunner speech,
        IFormsCatalogIntentParser intentParser,
        FormsCatalogLanguageChangeWorkflow languageChangeWorkflow,
        ISpeechAssistantTrace speechAssistantTrace)
    {
        _speech = speech ?? throw new ArgumentNullException(nameof(speech));
        _intentParser = intentParser ?? throw new ArgumentNullException(nameof(intentParser));
        _languageChangeWorkflow = languageChangeWorkflow ?? throw new ArgumentNullException(nameof(languageChangeWorkflow));
        _speechAssistantTrace = speechAssistantTrace ?? throw new ArgumentNullException(nameof(speechAssistantTrace));
    }

    public async Task<FormsCatalogLanguageChangeDecision> ConfirmAsync(
        FormsCatalogDialogTurnContext turnContext,
        string targetIsoCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetIsoCode);

        // Die Frage verwendet die Zielsprache und deren Eigenbezeichnung, damit sie auch ohne
        // Kenntnis der Sitzungssprache verständlich sein kann. Antworten werden anschließend
        // gegen beide Sprachen geprüft.
        var confirmationPrompt = SelectionDialogPrompt.FromText(
            _languageChangeWorkflow.GetConfirmationPrompt(
                targetIsoCode,
                _languageChangeWorkflow.GetEndonym(targetIsoCode)));
        // Die Bestätigungsfrage stammt aus dem gemeinsamen Sprachkatalog. Hilfe und Liste
        // verwenden die Katalogvorgaben der Sitzung. Ihre Verfügbarkeit in der Zielsprache ist
        // nicht für jeden Anwendungskatalog vorausgesetzt.
        var confirmationHelpPrompt = SelectionDialogPrompt.FromSequence(
            confirmationPrompt.Text,
            [],
            FormsCatalogPromptBuilder.GetConfirmationRepeatPrompt(
                turnContext.PromptContext,
                turnContext.IsoCode,
                turnContext.PolitenessLevel).Text);
        // Die erste Aufnahme wird für die Zielsprache angefordert. Ja-/Nein-Formen der
        // Sitzungssprache werden als zusätzliche Erkennungshinweise übergeben. Die Anbieteranbindung
        // ergänzt die allgemeinen serverseitigen Hinweise. Erkennungsentscheidung und Mengenbegrenzung bleiben getrennt.
        var sessionConfirmationKeyterms = GetSessionConfirmationKeyterms(turnContext);
        var prompt = confirmationPrompt;
        var promptIsoCode = targetIsoCode;
        var maximumRecognizedFailures = Math.Max(turnContext.MaxRetries, 1);
        var captureFailurePolicy = new SelectionCaptureFailureTransitionPolicy(turnContext.MaxRetries);
        var recognizedFailureCount = 0;
        var captureFailureCount = 0;

        while (true)
        {
            // Kurze Bestätigungsantworten erhalten den dafür vorgesehenen Erfassungsmodus.
            // Dessen gelockerter Abstand zur letzten Wiedergabe soll einen frühen Antwortbeginn
            // unterstützen.
            var captureResult = await _speech.SpeakAndListenAsync(
                    prompt,
                    promptIsoCode,
                    turnContext.InteractionSettings,
                    turnContext.NoInputTimeoutMs,
                    cancellationToken,
                    new SpeechKeyterms(sessionConfirmationKeyterms, KeytermComposition.WithServerBase),
                    SpeechCaptureMode.ShortConfirmation)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(captureResult.FailureCode))
            {
                var captureFailureDecision = captureFailurePolicy.Evaluate(
                    captureResult.FailureCode,
                    captureFailureCount,
                    SelectionDialogPhase.Confirming);
                await TraceRecoverableFailureAsync(turnContext, captureResult, cancellationToken).ConfigureAwait(false);
                captureFailureCount = checked(captureFailureCount + 1);
                if (captureFailureDecision.Kind == SelectionCaptureFailureTransitionKind.Terminal)
                {
                    return FormsCatalogLanguageChangeDecision.CaptureFaultBudgetExhausted;
                }

                promptIsoCode = turnContext.IsoCode;
                prompt = SelectionDialogPrompt.FromSequence(
                    RecoverableCapturePromptTextResolver.GetRecoverableFailurePrompt(
                        turnContext.IsoCode,
                        turnContext.PolitenessLevel),
                    [],
                    confirmationPrompt.Text)
                    .AsAnnouncement(SpokenAnnouncementKind.RecoverableFailure);
                continue;
            }

            if (string.IsNullOrWhiteSpace(captureResult.Transcript))
            {
                // Ohne Transkript wird dieselbe Frage wiederholt, ohne das Budget erkannter
                // Fehlantworten zu belasten. So entsteht auch bei null Wiederholungen keine
                // Ablehnung allein aus einem leeren Ergebnis. Eine zusätzliche Katalogvorlage
                // wird dafür nicht benötigt.
                promptIsoCode = targetIsoCode;
                prompt = confirmationPrompt;
                continue;
            }

            captureFailureCount = 0;

            var confirmation = TryResolveConfirmation(turnContext, targetIsoCode, captureResult.Transcript);
            if (confirmation.HasValue)
            {
                return confirmation.Value;
            }

            var parsedIntent = ParseAgainstBothLanguages(turnContext, targetIsoCode, captureResult.Transcript);
            if (parsedIntent == FormsCatalogIntent.Help)
            {
                promptIsoCode = turnContext.IsoCode;
                prompt = confirmationHelpPrompt;
                continue;
            }

            if (parsedIntent == FormsCatalogIntent.Repeat)
            {
                promptIsoCode = targetIsoCode;
                prompt = confirmationPrompt;
                continue;
            }

            if (parsedIntent == FormsCatalogIntent.List)
            {
                promptIsoCode = turnContext.IsoCode;
                prompt = FormsCatalogPromptBuilder.GetConfirmationListPrompt(
                    turnContext.PromptContext,
                    turnContext.InitialCandidates,
                    confirmationPrompt,
                    turnContext.IsoCode,
                    turnContext.PolitenessLevel);
                continue;
            }

            recognizedFailureCount++;
            if (recognizedFailureCount >= maximumRecognizedFailures)
            {
                return FormsCatalogLanguageChangeDecision.Declined;
            }

            promptIsoCode = targetIsoCode;
            prompt = confirmationPrompt;
        }
    }

    private static IReadOnlyList<string> GetSessionConfirmationKeyterms(
        FormsCatalogDialogTurnContext turnContext)
    {
        var resolver = new FormsCatalogIntentUtteranceResolver();
        return resolver
            .GetUtterances(
                turnContext.Catalog,
                CatalogVoiceIntentId.ConfirmYes,
                CatalogVoiceIntentMatchMode.Phrase,
                turnContext.IsoCode,
                turnContext.PolitenessLevel)
            .Concat(resolver.GetUtterances(
                turnContext.Catalog,
                CatalogVoiceIntentId.ConfirmNo,
                CatalogVoiceIntentMatchMode.Phrase,
                turnContext.IsoCode,
                turnContext.PolitenessLevel))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private FormsCatalogIntent ParseAgainstBothLanguages(
        FormsCatalogDialogTurnContext turnContext,
        string targetIsoCode,
        string transcript)
    {
        var targetIntent = _intentParser.Parse(
            turnContext.Catalog,
            transcript,
            targetIsoCode,
            turnContext.PolitenessLevel).Intent;
        if (targetIntent != FormsCatalogIntent.Unknown)
        {
            return targetIntent;
        }

        return _intentParser.Parse(
            turnContext.Catalog,
            transcript,
            turnContext.IsoCode,
            turnContext.PolitenessLevel).Intent;
    }

    private FormsCatalogLanguageChangeDecision? TryResolveConfirmation(
        FormsCatalogDialogTurnContext turnContext,
        string targetIsoCode,
        string? transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return null;
        }

        // Die Antwort wird zuerst für die Zielsprache und danach für die bisherige
        // Sitzungssprache geprüft. Bei einer erkannten Zustimmung, Ablehnung oder einem
        // Abbruch in der Zielsprache endet die Prüfung.
        return ResolveAgainstLanguage(turnContext, targetIsoCode, transcript)
            ?? ResolveAgainstLanguage(turnContext, turnContext.IsoCode, transcript);
    }

    private FormsCatalogLanguageChangeDecision? ResolveAgainstLanguage(
        FormsCatalogDialogTurnContext turnContext,
        string isoCode,
        string transcript)
    {
        var confirmationIntent = _intentParser.Parse(
            turnContext.Catalog,
            transcript,
            isoCode,
            turnContext.PolitenessLevel).Intent;
        return confirmationIntent switch
        {
            FormsCatalogIntent.ConfirmYes => FormsCatalogLanguageChangeDecision.Confirmed,
            FormsCatalogIntent.ConfirmNo => FormsCatalogLanguageChangeDecision.Declined,
            // Abbrechen fordert den gesprochenen Dialogausstieg an. Eine bloße Ablehnung behält
            // dagegen Sprache und Katalogdialog bei.
            FormsCatalogIntent.Cancel => FormsCatalogLanguageChangeDecision.Cancelled,
            _ => null
        };
    }

    private async ValueTask TraceRecoverableFailureAsync(
        FormsCatalogDialogTurnContext turnContext,
        SpeechCaptureResult captureResult,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(captureResult.FailureCode))
        {
            return;
        }

        await _speechAssistantTrace
            .WriteIfConfiguredAsync(
                turnContext.CorrelationId,
                SpeechAssistantTraceScope.SelectionDialogRuntime,
                SpeechAssistantTraceLevel.Warning,
                $"Forms catalog language-change confirmation observed recoverable speech capture failure. code={captureResult.FailureCode}",
                cancellationToken)
            .ConfigureAwait(false);
    }
}
