using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Führt den Formulardialog mit Sitzungssprache, Anwendung, Formular und Anwendungssitzung aus.
/// </summary>
internal sealed class RunVoiceCommandDialogEffect : IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>
{
    private const string FailureMessage = "Voice command dialog failed.";
    private readonly string? _selectedIsoCode;
    private readonly string? _selectedApplicationId;
    private readonly string? _selectedFormId;
    private readonly string? _correlationId;
    private readonly SpeechAssistantApplicationSessionState _applicationSession;
    private readonly ImmutableDictionary<string, string> _initialValuesByVariableId;

    internal string? SelectedApplicationId => _selectedApplicationId;

    internal string? SelectedFormId => _selectedFormId;

    /// <summary>
    /// Anwendungssitzung, deren Zustand beim Start des Formulardialogs übernommen wird.
    /// </summary>
    internal SpeechAssistantApplicationSessionState ApplicationSession => _applicationSession;

    public RunVoiceCommandDialogEffect(
        string? selectedIsoCode,
        string? selectedApplicationId,
        SpeechAssistantApplicationSessionState applicationSession)
        : this(
            selectedIsoCode,
            selectedApplicationId,
            selectedFormId: null,
            correlationId: null,
            applicationSession,
            ImmutableDictionary.Create<string, string>(StringComparer.Ordinal))
    {
    }

    public RunVoiceCommandDialogEffect(
        string? selectedIsoCode,
        string? selectedApplicationId,
        string? selectedFormId,
        string? correlationId,
        SpeechAssistantApplicationSessionState applicationSession)
        : this(
            selectedIsoCode,
            selectedApplicationId,
            selectedFormId,
            correlationId,
            applicationSession,
            ImmutableDictionary.Create<string, string>(StringComparer.Ordinal))
    {
    }

    public RunVoiceCommandDialogEffect(
        string? selectedIsoCode,
        string? selectedApplicationId,
        string? selectedFormId,
        string? correlationId,
        SpeechAssistantApplicationSessionState applicationSession,
        ImmutableDictionary<string, string> initialValuesByVariableId)
    {
        _selectedIsoCode = IsoCode.TryNormalizeIso6391(selectedIsoCode);
        _selectedApplicationId = string.IsNullOrWhiteSpace(selectedApplicationId) ? null : selectedApplicationId.Trim();
        _selectedFormId = string.IsNullOrWhiteSpace(selectedFormId) ? null : selectedFormId.Trim();
        _correlationId = NormalizeOptional(correlationId);
        _applicationSession = applicationSession ?? throw new ArgumentNullException(nameof(applicationSession));
        _initialValuesByVariableId = initialValuesByVariableId ?? throw new ArgumentNullException(nameof(initialValuesByVariableId));
    }

    public async ValueTask ExecuteAsync(
        SpeechAssistantContext context,
        IEventDispatcher<SpeechAssistantEvent> dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatcher);

        var correlationId = ResolveCorrelationId();
        await context.InteractionDependencies.SpeechAssistantTrace
            .WriteIfConfiguredAsync(
                correlationId,
                SpeechAssistantTraceScope.VoiceCommandEffect,
                SpeechAssistantTraceLevel.Information,
                $"Starting voice command dialog for applicationId={_selectedApplicationId ?? "<missing>"} formId={_selectedFormId ?? "<auto>"}",
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await SpeechAssistantVoiceInputPauseTerminal.CompleteExpectedAsync(async _ =>
            {
                if (string.IsNullOrWhiteSpace(_selectedIsoCode))
                {
                    throw new InvalidOperationException("Voice command dialog requires a valid selected ISO code.");
                }

                if (string.IsNullOrWhiteSpace(_selectedApplicationId))
                {
                    throw new InvalidOperationException("Voice command dialog requires a selected application identifier.");
                }

                var result = await context.DialogDependencies.VoiceCommandDialog
                    .ExecuteAsync(
                        _selectedIsoCode!,
                        _selectedApplicationId!,
                        _selectedFormId,
                        _applicationSession,
                        _initialValuesByVariableId,
                        cancellationToken)
                    .ConfigureAwait(false);

                // Das Ergebnis mit den beim Pausieren erhaltenen Werten soll auch dann eingereiht
                // werden können, wenn das Unterbrechen den Effekt-Token bereits abgebrochen hat.
                var dispatchToken = result.WasPaused
                    ? SpeechAssistantTerminalCancellation.ForRequiredTerminalWork()
                    : cancellationToken;

                await context.InteractionDependencies.SpeechAssistantTrace
                    .WriteIfConfiguredAsync(
                        correlationId,
                        SpeechAssistantTraceScope.VoiceCommandEffect,
                        SpeechAssistantTraceLevel.Information,
                        $"Voice command dialog {(result.WasPaused ? "paused" : "completed")}. outcome={result.Outcome} navigation={result.Navigation}",
                        dispatchToken)
                    .ConfigureAwait(false);

                SpeechAssistantEvent terminalEvent = result.WasPaused
                    ? new SpeechAssistantVoiceCommandPausedEvent(result)
                    : new SpeechAssistantVoiceCommandCompletedEvent(result);

                await dispatcher.DispatchAsync(
                        terminalEvent,
                        DispatchMode.Enqueue,
                        dispatchToken)
                    .ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            await context.InteractionDependencies.SpeechAssistantTrace
                .WriteIfConfiguredAsync(
                    correlationId,
                    SpeechAssistantTraceScope.VoiceCommandEffect,
                    SpeechAssistantTraceLevel.Warning,
                    "Voice command dialog was interrupted before completion.",
                    SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            await dispatcher.DispatchAsync(
                    new SpeechAssistantVoiceCommandCompletedEvent(
                        CreateRecoveryResult()),
                    DispatchMode.Enqueue,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            context.InteractionDependencies.Logger.LogDebug(ex, FailureMessage);
            await context.InteractionDependencies.SpeechAssistantTrace
                .WriteIfConfiguredAsync(
                    correlationId,
                    SpeechAssistantTraceScope.VoiceCommandEffect,
                    SpeechAssistantTraceLevel.Error,
                    FailureMessage,
                    SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            await context.InteractionDependencies.SpeechInteractionSignals
                .ReportFailureAsync(SpeechErrorCode.VoiceCommandDialogFailed, FailureMessage)
                .ConfigureAwait(false);

            await dispatcher.DispatchAsync(
                    new SpeechAssistantVoiceCommandCompletedEvent(
                        CreateRecoveryResult()),
                    DispatchMode.Enqueue,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private VoiceCommandDialogResult CreateRecoveryResult()
    {
        if (string.IsNullOrWhiteSpace(_selectedApplicationId))
        {
            return VoiceCommandDialogResult.AbandonedWithoutContext(_initialValuesByVariableId);
        }

        return VoiceCommandDialogResult.Abandoned(_selectedApplicationId, _selectedFormId, _initialValuesByVariableId);
    }

    private string ResolveCorrelationId()
        => string.IsNullOrWhiteSpace(_correlationId)
            ? Guid.NewGuid().ToString("N")
            : _correlationId;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
