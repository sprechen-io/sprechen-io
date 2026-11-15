namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bietet Korrekturen für einen wegen zu niedrigem Vertrauen abgelehnten
/// Buchstabierwert an. Fordert einen Hinweiston und die erlaubte
/// Wertwiedergabe an. Angeboten werden Korrektur oder erneutes Buchstabieren,
/// keine Übernahme des abgelehnten Werts.
/// </summary>
internal sealed class FormVoiceCommandSpellingEscalationFlow
{
    // Den Hinweiston vor der Wertwiedergabe anfordern, um die Korrekturfrage
    // von der gewöhnlichen Bestätigung zu unterscheiden.
    private const EarconId EscalationCue = EarconId.ValidationAmbiguous;

    private readonly SpellingConfidenceEscalationPolicy _escalationPolicy;
    private readonly FormVoiceCommandConfirmationValuePresenter _valuePresenter;
    private readonly FormVoiceCommandConfirmationDecisionService _decisionService;
    private readonly FormVoiceCommandConfirmationRepairActionFlow _repairActionFlow;
    private readonly IEarconPlayer _earconPlayer;

    public FormVoiceCommandSpellingEscalationFlow(
        SpellingConfidenceEscalationPolicy escalationPolicy,
        FormVoiceCommandConfirmationValuePresenter valuePresenter,
        FormVoiceCommandConfirmationDecisionService decisionService,
        FormVoiceCommandConfirmationRepairActionFlow repairActionFlow,
        IEarconPlayer earconPlayer)
    {
        _escalationPolicy = escalationPolicy ?? throw new ArgumentNullException(nameof(escalationPolicy));
        _valuePresenter = valuePresenter ?? throw new ArgumentNullException(nameof(valuePresenter));
        _decisionService = decisionService ?? throw new ArgumentNullException(nameof(decisionService));
        _repairActionFlow = repairActionFlow ?? throw new ArgumentNullException(nameof(repairActionFlow));
        _earconPlayer = earconPlayer ?? throw new ArgumentNullException(nameof(earconPlayer));
    }

    /// <summary>
    /// Versucht die Reparaturwahl für einen abgelehnten Buchstabierwert. Ohne Eskalation
    /// oder ohne hier behandelbare Antwort liefert der Ablauf null für den weiteren Pfad des Aufrufers.
    /// </summary>
    public async Task<FormVoiceCommandFieldHandlingResult?> TryEscalateAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandFieldApplyRequest request,
        SttConfidenceLevel confidenceLevel,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(request);

        if (_escalationPolicy.Decide(request.ExpectedInputMode, confidenceLevel, request.SttConfidencePercent)
            != SpellingEscalationDecision.Escalate)
        {
            return null;
        }

        await PlayEscalationCueAsync(cancellationToken).ConfigureAwait(false);

        var readback = await _valuePresenter
            .BuildAsync(
                turnContext,
                new FormVoiceCommandConfirmationRequest(request.TranscriptForApply),
                cancellationToken)
            .ConfigureAwait(false);

        // Die Reparaturansage verwenden, die Korrektur und erneutes Buchstabieren anbietet.
        // Eine Übernahme wird in diesem Ablehnungsweg nicht als Wahl ausgeführt.
        var repairPrompt = FormSpellingWorkflowPromptBuilder.GetSpellingLowConfidenceRepairPrompt(
            turnContext.Context.Contract,
            turnContext.Context.IsoCode,
            turnContext.Field.Label,
            readback.Value,
            readback.IncludesValue,
            turnContext.Context.VoiceSettings.GetPolitenessLevel());

        var decision = await _decisionService
            .CaptureDecisionAsync(
                turnContext.Context,
                turnContext.Session,
                turnContext.StateTracker,
                repairPrompt,
                cancellationToken)
            .ConfigureAwait(false);

        // Reparaturaktionen und interpretierte globale Befehle über die gemeinsamen
        // Ausführungswege behandeln. Bei anderen Antworten null an den Aufrufer zurückgeben.
        return await _repairActionFlow.TryHandleAsync(turnContext, decision, cancellationToken).ConfigureAwait(false);
    }

    private async Task PlayEscalationCueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _earconPlayer.PlayAsync(EscalationCue, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Fehler des Hinweistons außer Abbruchausnahmen abfangen, damit die
            // anschließende Ansage trotzdem versucht werden kann.
        }
    }
}
