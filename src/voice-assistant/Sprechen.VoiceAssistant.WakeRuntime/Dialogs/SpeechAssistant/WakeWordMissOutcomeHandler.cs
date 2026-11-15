using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Behandelt ausbleibende Aktivierung anhand von Text mit Diagnose, optionaler
/// Sprachhilfe und Hinweiston. Eine begonnene Segmentzusammenführung erhöht
/// den Fehlerzähler für Hilfe noch nicht.
/// </summary>
internal sealed class WakeWordMissOutcomeHandler
{
    private readonly WakeRecoveryGuidanceGate _guidanceGate;

    public WakeWordMissOutcomeHandler(WakeRecoveryGuidanceGate guidanceGate)
    {
        _guidanceGate = guidanceGate ?? throw new ArgumentNullException(nameof(guidanceGate));
    }

    public async ValueTask<bool> HandleAsync(
        WakeRuntimeContext context,
        IWakeRuntimeOutcomeSink outcomeSink,
        IWakeWordTelemetrySink telemetry,
        string correlationId,
        WakeWordInventory wakeWordInventory,
        WakeArbitrationOutcomeMiss miss,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(outcomeSink);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(wakeWordInventory);
        ArgumentNullException.ThrowIfNull(miss);

        if (miss.StitchPhase is { } stitchPhase)
        {
            await telemetry
                .RecordStitchAsync(
                    new WakeWordStitchEvent(
                        correlationId,
                        stitchPhase,
                        miss.StitchTranscriptLength,
                        StitchedTranscriptLength: 0,
                        CommandTextLength: 0),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await RecordMissTraceAsync(telemetry, correlationId, wakeWordInventory, miss, cancellationToken)
            .ConfigureAwait(false);

        await ReportMissDiagnosticAsync(context, wakeWordInventory, miss, cancellationToken)
            .ConfigureAwait(false);

        if (miss.StitchStarted)
        {
            // Bei begonnener Segmentzusammenführung noch keinen Hilfefehler zählen,
            // weil weitere Teile der Äußerung ausstehen können.
            return true;
        }

        _guidanceGate.RecordMiss();

        var guidanceSpoken = false;
        if (context.RecoveryGuidance is { } guidance && _guidanceGate.ShouldSpeakGuidance(guidance))
        {
            // Zuerst Ersthinweis oder Folgetext wählen. Den Versuch vor der Wiedergabe
            // vermerken, damit Mindestabstand und Zähler auch bei Wiedergabefehlern fortschreiten.
            var guidanceText = _guidanceGate.SelectGuidanceText(guidance);
            _guidanceGate.RecordGuidanceSpoken();
            guidanceSpoken = await TrySpeakGuidanceAsync(context, telemetry, correlationId, guidance, guidanceText, miss, cancellationToken)
                .ConfigureAwait(false);
        }

        if (!guidanceSpoken)
        {
            // Nach erfolgreicher Rückkehr der Sprachhilfe keinen zusätzlichen Fehlerton
            // anfordern. Andernfalls den konfigurierten Hinweiston versuchen.
            await PlayNotRecognizedEarconAsync(context, telemetry, correlationId, wakeWordInventory, miss, cancellationToken)
                .ConfigureAwait(false);
        }

        await outcomeSink
            .PublishAsync(new WakeRuntimeMissedOutcome(miss.Evaluation, correlationId), WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
            .ConfigureAwait(false);
        return true;
    }

    private async ValueTask<bool> TrySpeakGuidanceAsync(
        WakeRuntimeContext context,
        IWakeWordTelemetrySink telemetry,
        string correlationId,
        WakeRecoveryGuidance guidance,
        string guidanceText,
        WakeArbitrationOutcomeMiss miss,
        CancellationToken cancellationToken)
    {
        try
        {
            // Die Auswertung führte nicht zur Aktivierung. Die folgende Erkennungshilfe
            // gehört deshalb zum nachvollziehbaren Ergebnisweg.
            await context.TextToSpeechPlayer
                .SpeakAsync(guidanceText, guidance.IsoCode, speechRate: 1.0d, SpokenAnnouncementKind.RecoverableFailure, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await telemetry
                .RecordMissAsync(
                    new WakeWordMissEvent(
                        correlationId,
                        WakeWordMissPhase.EarconFailed,
                        miss.Evaluation.Status,
                        ConfidencePercent: null,
                        TranscriptLength: null,
                        ActivationFormCount: null,
                        RecognitionOnlyActivationFormCount: null,
                        ex,
                        Diagnostic: null),
                    WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            return false;
        }

        await telemetry
            .RecordMissAsync(
                new WakeWordMissEvent(
                    correlationId,
                    WakeWordMissPhase.GuidanceSpoken,
                    miss.Evaluation.Status,
                    miss.Evaluation.ConfidencePercent,
                    guidanceText.Length,
                    ActivationFormCount: null,
                    RecognitionOnlyActivationFormCount: null,
                    Exception: null,
                    Diagnostic: null),
                cancellationToken)
            .ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Meldet zusammengefasste Diagnosen nicht erkannter Aktivierungen,
    /// ohne den erkannten Äußerungstext in den Bericht aufzunehmen.
    /// </summary>
    private static async ValueTask ReportMissDiagnosticAsync(
        WakeRuntimeContext context,
        WakeWordInventory wakeWordInventory,
        WakeArbitrationOutcomeMiss miss,
        CancellationToken cancellationToken)
    {
        if (context.WakeMissDiagnosticsReporter is not { } reporter)
        {
            return;
        }

        var transcriptText = miss.Evaluation.TranscriptText ?? string.Empty;
        var transcriptTokens = TokenizeTranscript(transcriptText);

        await reporter
            .ReportAsync(
                new WakeMissDiagnosticsReport(
                    transcriptText.Length,
                    string.Join(' ', transcriptTokens).Length,
                    transcriptTokens.Length,
                    miss.Evaluation.Status.ToString(),
                    miss.Evaluation.ConfidencePercent,
                    wakeWordInventory.ActivationForms,
                    wakeWordInventory.RecognitionOnlyActivationForms),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask RecordMissTraceAsync(
        IWakeWordTelemetrySink telemetry,
        string correlationId,
        WakeWordInventory wakeWordInventory,
        WakeArbitrationOutcomeMiss miss,
        CancellationToken cancellationToken)
    {
        await telemetry
            .RecordMissAsync(
                new WakeWordMissEvent(
                    correlationId,
                    WakeWordMissPhase.MissTrace,
                    miss.Evaluation.Status,
                    miss.Evaluation.ConfidencePercent,
                    TranscriptLength: null,
                    ActivationFormCount: null,
                    RecognitionOnlyActivationFormCount: null,
                    Exception: null,
                    Diagnostic: null),
                cancellationToken)
            .ConfigureAwait(false);

        var transcriptText = miss.Evaluation.TranscriptText ?? string.Empty;
        var transcriptTokens = TokenizeTranscript(transcriptText);
        await telemetry
            .RecordMissAsync(
                new WakeWordMissEvent(
                    correlationId,
                    WakeWordMissPhase.MissDiagnostic,
                    miss.Evaluation.Status,
                    miss.Evaluation.ConfidencePercent,
                    transcriptText.Length,
                    wakeWordInventory.ActivationForms.Count,
                    wakeWordInventory.RecognitionOnlyActivationForms.Count,
                    Exception: null,
                    new WakeWordMissDiagnosticDetail(
                        TokenCount: transcriptTokens.Length,
                        wakeWordInventory.ActivationForms,
                        wakeWordInventory.RecognitionOnlyActivationForms)),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static string[] TokenizeTranscript(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return [];
        }

        var normalized = TextNormalizer.NormalizeForTokenComparison(transcript);
        return TextNormalizer.TokenizeLettersOrDigits(normalized).ToArray();
    }

    private async ValueTask PlayNotRecognizedEarconAsync(
        WakeRuntimeContext context,
        IWakeWordTelemetrySink telemetry,
        string correlationId,
        WakeWordInventory wakeWordInventory,
        WakeArbitrationOutcomeMiss miss,
        CancellationToken cancellationToken)
    {
        if (wakeWordInventory.NotRecognizedEarconId is not { } notRecognizedEarcon)
        {
            return;
        }

        try
        {
            await context.EarconPlayer
                .PlayAsync(notRecognizedEarcon, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await telemetry
                .RecordMissAsync(
                    new WakeWordMissEvent(
                        correlationId,
                        WakeWordMissPhase.EarconFailed,
                        miss.Evaluation.Status,
                    ConfidencePercent: null,
                    TranscriptLength: null,
                    ActivationFormCount: null,
                    RecognitionOnlyActivationFormCount: null,
                    ex,
                    Diagnostic: null),
                    WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
        }
    }
}
