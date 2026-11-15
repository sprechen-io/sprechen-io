using Sprechen.Contracts.FormSurfaces;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Koordiniert Rückmeldungen, lokale Abschlussbestätigung und Oberflächenzustände
/// beim Sitzungsabschluss. Die vorgelagerte dauerhafte Annahme wird hier nicht
/// selbst erzwungen. Sie gehört zum aufrufenden Abschlussablauf.
/// </summary>
internal sealed class FormVoiceCommandTerminalFeedbackOwner
{
    private static readonly TimeSpan InitialDeliveryWait = TimeSpan.FromSeconds(5); // arch-allow: owner-bypass-execution-mechanic
    private static readonly TimeSpan DeliveryReannouncementInterval = TimeSpan.FromSeconds(30); // arch-allow: owner-bypass-execution-mechanic
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly IEarconPlayer _earconPlayer;
    private readonly FormVoiceCommandSessionSurfacePublisher _surfacePublisher;
    private readonly FormVoiceCommandTerminalWorkLifetime _terminalWorkLifetime;

    public FormVoiceCommandTerminalFeedbackOwner(
        IFormVoiceCommandSpeechService speechService,
        IEarconPlayer earconPlayer,
        FormVoiceCommandSessionSurfacePublisher surfacePublisher,
        FormVoiceCommandTerminalWorkLifetime terminalWorkLifetime)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _earconPlayer = earconPlayer ?? throw new ArgumentNullException(nameof(earconPlayer));
        _surfacePublisher = surfacePublisher ?? throw new ArgumentNullException(nameof(surfacePublisher));
        _terminalWorkLifetime = terminalWorkLifetime ?? throw new ArgumentNullException(nameof(terminalWorkLifetime));
    }

    internal CancellationToken RequiredWorkToken => _terminalWorkLifetime.RequiredWorkToken;

    internal Task PublishSurfaceAsync(
        FormVoiceCommandSessionRunResult result,
        FormSurfacePublicationSession publication,
        FormVoiceCommandContext context,
        IFormDialogRuntime runtime,
        FormSurfaceDefinition projection)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(publication);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(projection);

        return _surfacePublisher.TryPublishTerminalAsync(
            publication,
            context,
            runtime,
            projection,
            result.SurfaceStage);
    }

    internal Task AnnounceAsync(
        FormVoiceCommandContext context,
        ParticipantTaskTerminationClassification terminationClassification)
        => AnnounceTerminalOutcomeAsync(context, terminationClassification, restored: false);

    internal async Task<VoiceCommandDialogResult> PublishRestoredAsync(
        FormVoiceCommandContext context,
        ParticipantTaskTerminationClassification terminationClassification)
    {
        ArgumentNullException.ThrowIfNull(context);

        await AnnounceTerminalOutcomeAsync(context, terminationClassification, restored: true)
            .ConfigureAwait(false);

        return terminationClassification switch
        {
            ParticipantTaskTerminationClassification.Completed => VoiceCommandDialogResult.Submitted(
                context.Contract.ApplicationId,
                context.Contract.FormId,
                context.ValuesByVariableId),
            ParticipantTaskTerminationClassification.ParticipantCancelled => VoiceCommandDialogResult.Cancelled(
                context.Contract.ApplicationId,
                context.Contract.FormId,
                context.ValuesByVariableId),
            ParticipantTaskTerminationClassification.SessionAbandoned or
            ParticipantTaskTerminationClassification.TechnicalFailure => VoiceCommandDialogResult.Abandoned(
                context.Contract.ApplicationId,
                context.Contract.FormId,
                context.ValuesByVariableId),
            _ => throw new ArgumentOutOfRangeException(
                nameof(terminationClassification),
                terminationClassification,
                "Unsupported restored task termination classification."),
        };
    }

    internal async Task<EvaluationSessionAcceptance?> AwaitAcceptanceAsync(
        Task<EvaluationSessionAcceptance?> acceptanceTask,
        string isoCode)
        => await AwaitProgressAsync(
                acceptanceTask,
                isoCode,
                FormDialogFlowPromptTextResolver.GetEvaluationDeliveryWaitingPrompt)
            .ConfigureAwait(false);

    internal async Task<EvaluationSessionAcceptance> AwaitRestoredAcceptanceAsync(
        Task<EvaluationSessionAcceptance> acceptanceTask,
        string isoCode)
        => await AwaitProgressAsync(
                acceptanceTask,
                isoCode,
                FormDialogFlowPromptTextResolver.GetEvaluationDeliveryWaitingPrompt)
            .ConfigureAwait(false);

    internal async Task AwaitLocalCommitAsync(
        IEvaluationSessionTelemetry telemetry,
        EvaluationSessionAcceptance acceptance,
        string isoCode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(acceptance);

        while (true)
        {
            try
            {
                await AwaitCommitProgressAsync(
                        telemetry.CommitSessionAsync(acceptance, RequiredWorkToken),
                        isoCode,
                        FormDialogFlowPromptTextResolver.GetEvaluationLocalCommitWaitingPrompt)
                    .ConfigureAwait(false);
                return;
            }
            catch (EvaluationLocalCommitUnavailableException)
            {
                await SpeakStatusAsync(
                        FormDialogFlowPromptTextResolver.GetEvaluationLocalCommitWaitingPrompt(isoCode),
                        isoCode,
                        RequiredWorkToken)
                    .ConfigureAwait(false);
                await Task.Delay(DeliveryReannouncementInterval, RequiredWorkToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<TAcceptance> AwaitProgressAsync<TAcceptance>(
        Task<TAcceptance> acceptanceTask,
        string isoCode,
        Func<string, string> waitingPrompt)
    {
        ArgumentNullException.ThrowIfNull(acceptanceTask);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var nextDelay = InitialDeliveryWait;
        while (!acceptanceTask.IsCompleted)
        {
            if (await Task.WhenAny(acceptanceTask, Task.Delay(nextDelay)).ConfigureAwait(false) == acceptanceTask)
            {
                break;
            }

            await SpeakStatusAsync(
                    waitingPrompt(isoCode),
                    isoCode,
                    RequiredWorkToken)
                .ConfigureAwait(false);
            nextDelay = DeliveryReannouncementInterval;
        }

        try
        {
            return await acceptanceTask.ConfigureAwait(false);
        }
        catch (EvaluationTelemetryDeliveryRejectedException)
        {
            await SpeakStatusAsync(
                    FormDialogFlowPromptTextResolver.GetEvaluationPermanentFailurePrompt(isoCode),
                    isoCode,
                    RequiredWorkToken)
                .ConfigureAwait(false);
            throw;
        }
    }

    private async Task AwaitCommitProgressAsync(
        Task commitTask,
        string isoCode,
        Func<string, string> waitingPrompt)
    {
        ArgumentNullException.ThrowIfNull(commitTask);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var nextDelay = InitialDeliveryWait;
        while (!commitTask.IsCompleted)
        {
            if (await Task.WhenAny(commitTask, Task.Delay(nextDelay)).ConfigureAwait(false) == commitTask)
            {
                break;
            }

            await SpeakStatusAsync(waitingPrompt(isoCode), isoCode, RequiredWorkToken).ConfigureAwait(false);
            nextDelay = DeliveryReannouncementInterval;
        }

        await commitTask.ConfigureAwait(false);
    }

    internal Task ReportOtherTabAsync(string isoCode, CancellationToken cancellationToken)
        => SpeakStatusAsync(
            FormDialogFlowPromptTextResolver.GetEvaluationOtherTabPrompt(isoCode),
            isoCode,
            cancellationToken);

    internal Task ReportPermanentFailureAsync(string isoCode, CancellationToken cancellationToken)
        => SpeakStatusAsync(
            FormDialogFlowPromptTextResolver.GetEvaluationPermanentFailurePrompt(isoCode),
            isoCode,
            cancellationToken);

    private async Task AnnounceTerminalOutcomeAsync(
        FormVoiceCommandContext context,
        ParticipantTaskTerminationClassification terminationClassification,
        bool restored)
    {
        var prompt = terminationClassification switch
        {
            ParticipantTaskTerminationClassification.Completed =>
                FormDialogFlowPromptTextResolver.GetFormSubmittedPrompt(context.IsoCode),
            ParticipantTaskTerminationClassification.ParticipantCancelled =>
                FormDialogFlowPromptTextResolver.GetEvaluationTaskCancelledPrompt(context.IsoCode),
            ParticipantTaskTerminationClassification.TechnicalFailure =>
                FormDialogFlowPromptTextResolver.GetEvaluationTechnicalFailurePrompt(context.IsoCode),
            ParticipantTaskTerminationClassification.SessionAbandoned when restored =>
                FormDialogFlowPromptTextResolver.GetEvaluationRestoredAbandonedPrompt(context.IsoCode),
            ParticipantTaskTerminationClassification.SessionAbandoned => null,
            _ => throw new ArgumentOutOfRangeException(
                nameof(terminationClassification), terminationClassification,
                "Unsupported task termination classification.")
        };

        if (terminationClassification == ParticipantTaskTerminationClassification.Completed)
        {
            await PlayCompletionEarconSafeAsync().ConfigureAwait(false);
        }

        if (prompt is null)
        {
            return;
        }

        try
        {
            await _speechService.SpeakSafeAsync(
                prompt, context.IsoCode, context.VoiceSettings, RequiredWorkToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (RequiredWorkToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Nur die optionale Schlussansage darf ausfallen. Annahme, lokale
            // Festschreibung und Veröffentlichung bleiben außerhalb dieser Fangstelle.
        }
    }

    private async Task SpeakStatusAsync(string text, string isoCode, CancellationToken cancellationToken)
    {
        try
        {
            await _speechService.SpeakSafeAsync(text, isoCode, new VoiceDialogSettings(), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Fehler der Statusansage außer dem angeforderten Abbruch abfangen, damit
            // eine optionale Rückmeldung den weiteren Abschlussweg nicht beendet.
        }
    }

    private async Task PlayCompletionEarconSafeAsync()
    {
        try
        {
            await _earconPlayer.PlayAsync(EarconId.FlowDone, RequiredWorkToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Fehler eines optionalen lokalen Hinweistons abfangen. Dieser Aufruf
            // ändert den bereits übergebenen Abschlussstand nicht.
        }
    }
}
