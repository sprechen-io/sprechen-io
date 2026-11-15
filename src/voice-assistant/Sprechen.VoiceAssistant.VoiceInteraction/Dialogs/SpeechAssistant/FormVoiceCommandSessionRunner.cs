using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Koordiniert Formularlauf, Sitzungsabschluss und Fehlerbehandlung einschließlich der abschließenden Telemetriezuordnung.
/// </summary>
internal sealed class FormVoiceCommandSessionRunner : IFormVoiceCommandSessionRunner
{
    private const string FailureMessage = "Voice command dialog failed.";
    private readonly IEvaluationSessionTelemetry _evaluationTelemetry;
    private readonly IEvaluationFormTelemetry _evaluationFormTelemetry;
    private readonly ISpeechCaptureFeedback _speechCaptureFeedback;
    private readonly IFormDialogRuntimeFactory _runtimeFactory;
    private readonly FormVoiceCommandSessionSurfacePublisher _surfacePublisher;
    private readonly FormVoiceCommandTerminalFeedbackOwner _terminalFeedbackOwner;
    private readonly FormVoiceCommandFieldCoordinator _fieldCoordinator;
    private readonly FormVoiceCommandSessionResultFactory _resultFactory;
    private readonly ILogger<FormVoiceCommandSessionRunner> _logger;
    private readonly FormVoiceCommandTaskCompletionResolver _taskCompletionResolver = new();

    public FormVoiceCommandSessionRunner(
        IEvaluationSessionTelemetry evaluationTelemetry,
        IEvaluationFormTelemetry evaluationFormTelemetry,
        ISpeechCaptureFeedback speechCaptureFeedback,
        IFormDialogRuntimeFactory runtimeFactory,
        FormVoiceCommandSessionSurfacePublisher surfacePublisher,
        FormVoiceCommandTerminalFeedbackOwner terminalFeedbackOwner,
        FormVoiceCommandFieldCoordinator fieldCoordinator,
        FormVoiceCommandSessionResultFactory resultFactory,
        ILogger<FormVoiceCommandSessionRunner> logger)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _evaluationFormTelemetry = evaluationFormTelemetry ?? throw new ArgumentNullException(nameof(evaluationFormTelemetry));
        _speechCaptureFeedback = speechCaptureFeedback ?? throw new ArgumentNullException(nameof(speechCaptureFeedback));
        _runtimeFactory = runtimeFactory ?? throw new ArgumentNullException(nameof(runtimeFactory));
        _surfacePublisher = surfacePublisher ?? throw new ArgumentNullException(nameof(surfacePublisher));
        _terminalFeedbackOwner = terminalFeedbackOwner ?? throw new ArgumentNullException(nameof(terminalFeedbackOwner));
        _fieldCoordinator = fieldCoordinator ?? throw new ArgumentNullException(nameof(fieldCoordinator));
        _resultFactory = resultFactory ?? throw new ArgumentNullException(nameof(resultFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VoiceCommandDialogResult> RunAsync(FormVoiceCommandContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var (session, earlyResult) = await StartSessionAsync(context, cancellationToken).ConfigureAwait(false);
        if (earlyResult is not null)
        {
            return earlyResult;
        }

        return await RunSessionDialogAsync(context, session!, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(EvaluationSession? Session, VoiceCommandDialogResult? EarlyResult)> StartSessionAsync(
        FormVoiceCommandContext context, CancellationToken cancellationToken)
    {
        EvaluationSession session;
        try
        {
            session = await _evaluationTelemetry.StartSessionAsync(
                    context.ApplicationId,
                    context.Contract.FormId.Value,
                    context.IsoCode,
                    EvaluationChannel.Voice,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (EvaluationSessionAlreadyActiveException)
        {
            await _terminalFeedbackOwner.ReportOtherTabAsync(
                    context.IsoCode,
                    _terminalFeedbackOwner.RequiredWorkToken)
                .ConfigureAwait(false);
            throw;
        }
        catch (EvaluationTelemetryDeliveryRejectedException)
        {
            _surfacePublisher.Stop();
            await _terminalFeedbackOwner
                .ReportPermanentFailureAsync(context.IsoCode, _terminalFeedbackOwner.RequiredWorkToken)
                .ConfigureAwait(false);
            return (null, VoiceCommandDialogResult.Abandoned(
                context.Contract.ApplicationId,
                context.Contract.FormId,
                context.ValuesByVariableId));
        }

        if (session.RestoredTerminal is null)
        {
            return (session, null);
        }

        var restoredAcceptance = await _terminalFeedbackOwner
            .AwaitRestoredAcceptanceAsync(session.RestoredTerminal.Acceptance, context.IsoCode)
            .ConfigureAwait(false);
        await _terminalFeedbackOwner
            .AwaitLocalCommitAsync(_evaluationTelemetry, restoredAcceptance, context.IsoCode)
            .ConfigureAwait(false);
        _surfacePublisher.Stop();
        return (null, await _terminalFeedbackOwner
            .PublishRestoredAsync(context, session.RestoredTerminal.TerminationClassification)
            .ConfigureAwait(false));
    }

    private async Task<VoiceCommandDialogResult> RunSessionDialogAsync(
        FormVoiceCommandContext context, EvaluationSession session, CancellationToken cancellationToken)
    {
        IFormDialogRuntime? runtime = null;
        FormVoiceCommandContext? sessionContext = null;
        FormSurfaceDefinition? formProjection = null;
        var runResult = FormVoiceCommandSessionRunResult.Abandoned();
        var wasPaused = false;
        var technicalFailure = false;
        var publishTerminal = false;
        FormVoiceCommandStateTracker? stateTracker = null;
        FormSurfacePublicationSession? publication = null;
        EvaluationSessionAcceptance? acceptance = null;
        EvaluationTaskCompletion? taskCompletion = null;

        try
        {
            var loopGuardFeedback = new FormRuleLoopGuardFeedback(_evaluationFormTelemetry, _speechCaptureFeedback, session);
            runtime = _runtimeFactory.Create(context, session.SessionId, loopGuardFeedback);
            formProjection = _surfacePublisher.BuildProjection(context);
            sessionContext = context with { FormProjection = formProjection };
            publication = _surfacePublisher.BeginSession();
            stateTracker = new FormVoiceCommandStateTracker(_evaluationTelemetry, session);
            runResult = await _fieldCoordinator.RunAsync(
                    sessionContext,
                    runtime,
                    session,
                    stateTracker,
                    formProjection,
                    publication,
                    cancellationToken)
                .ConfigureAwait(false);
            publishTerminal = runResult.DialogResult?.IsSilentInputRequested != true;
        }
        catch (FormContractPolicyInvalidException)
        {
            _surfacePublisher.Stop();
            throw;
        }
        catch (VoiceInputPausedException)
        {
            wasPaused = true;
            _surfacePublisher.Stop();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Verbleibende Ausnahmen außer Abbruchausnahmen als technischen Sitzungsfehler
            // behandeln. Die Diagnose hält Ausnahmedaten und letzten Dialogzustand fest.
            // Dieser Fangpfad ist nicht auf zuvor als unbekannt klassifizierte Fehler begrenzt.
            _logger.LogError(
                ex,
                "Voice command form dialog abandoned by the session catch-all. ExceptionType={ExceptionType}, Message={ExceptionMessage}, MicroState={MicroState}, TelemetryState={TelemetryState}.",
                ex.GetType().FullName,
                ex.Message,
                stateTracker?.CurrentMicroState ?? "<none>",
                stateTracker?.CurrentTelemetryState ?? "<none>");
            runResult = FormVoiceCommandSessionRunResult.Abandoned();
            technicalFailure = true;
            if (publication is not null && runtime is not null && sessionContext is not null && formProjection is not null)
            {
                publishTerminal = true;
            }
            else
            {
                _surfacePublisher.Stop();
            }
        }
        finally
        {
            // Den Aufgabenabschluss vor dem Sitzungsende ermitteln, damit der abschließende
            // Bericht den zu diesem Zeitpunkt verfügbaren Formularzustand berücksichtigt.
            taskCompletion = await _taskCompletionResolver.ResolveAsync(runtime, runResult, technicalFailure).ConfigureAwait(false);
            var acceptanceTask = _evaluationTelemetry.EndSessionAsync(
                session,
                runResult.Outcome,
                taskCompletion,
                _terminalFeedbackOwner.RequiredWorkToken);
            acceptance = await _terminalFeedbackOwner
                .AwaitAcceptanceAsync(acceptanceTask, context.IsoCode)
                .ConfigureAwait(false);
        }

        await PublishTerminalOutcomeAsync(
                context, runtime, sessionContext, formProjection, runResult, publication,
                taskCompletion!, acceptance, publishTerminal)
            .ConfigureAwait(false);

        var finalValues = runtime is null
            ? ImmutableDictionary<string, string>.Empty
            : (await runtime.GetValuesSnapshotAsync().ConfigureAwait(false))
                .ToImmutableDictionary(StringComparer.Ordinal);
        return wasPaused
            ? _resultFactory.CreatePaused(context, finalValues)
            : _resultFactory.Create(context, runResult, finalValues);
    }

    private async Task PublishTerminalOutcomeAsync(
        FormVoiceCommandContext context, IFormDialogRuntime? runtime, FormVoiceCommandContext? sessionContext,
        FormSurfaceDefinition? formProjection, FormVoiceCommandSessionRunResult runResult,
        FormSurfacePublicationSession? publication, EvaluationTaskCompletion taskCompletion,
        EvaluationSessionAcceptance? acceptance, bool publishTerminal)
    {
        if (acceptance is not null)
        {
            await _terminalFeedbackOwner
                .AwaitLocalCommitAsync(_evaluationTelemetry, acceptance, context.IsoCode)
                .ConfigureAwait(false);
        }

        await _terminalFeedbackOwner
            .AnnounceAsync(context, taskCompletion.TerminationClassification)
            .ConfigureAwait(false);

        if (publication is not null)
        {
            if (publishTerminal && runtime is not null && sessionContext is not null && formProjection is not null)
            {
                await _terminalFeedbackOwner
                    .PublishSurfaceAsync(
                        runResult,
                        publication,
                        sessionContext,
                        runtime,
                        formProjection)
                    .ConfigureAwait(false);
            }

            _surfacePublisher.EndSession(publication);
        }
    }
}
