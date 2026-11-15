using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet die sitzungsbezogene Sperrfrist mit der Abbruchbestätigung und bildet nur nach bestätigtem Abbruch ein Endergebnis mit dem aktuellen Wertbestand.
/// </summary>
internal sealed class FormVoiceCommandCancelHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly ISystemClock _clock;
    private readonly IEvaluationCommandTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly Dictionary<string, DateTimeOffset> _lastCancelRequestedAtBySessionId = new(StringComparer.Ordinal);

    public FormVoiceCommandCancelHandler(
        ISystemClock clock,
        IEvaluationCommandTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public bool CanHandle(CommandId commandId) => commandId == GlobalCommandIds.Cancel;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();

        var sessionId = context.Session.SessionId;
        var now = _clock.UtcNow;
        var cancelRefractoryPeriod = context.VoiceSettings.GetCancelRefractoryDuration();

        if (_lastCancelRequestedAtBySessionId.TryGetValue(sessionId, out var lastRequestedAt)
            && now - lastRequestedAt < cancelRefractoryPeriod)
        {
            _evaluationTelemetry.TrackDestructiveCommandResolved(
                context.Session,
                GlobalCommandIds.Cancel,
                confirmed: false,
                refractoryBlocked: true);
            context.StateTracker.BeginErrorPrompt();
            await _speechService.SpeakSafeAsync(
                    FormConfirmationPromptBuilder.GetCancelRefractoryPrompt(context.Contract, context.IsoCode, politenessLevel),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        _lastCancelRequestedAtBySessionId[sessionId] = now;
        context.StateTracker.BeginCancelConfirmation();

        var confirm = await _speechService.ConfirmYesNoAsync(
                context.CaptureSettings,
                FormConfirmationPromptBuilder.GetCancelConfirmationPrompt(
                    context.Contract,
                    context.IsoCode,
                    politenessLevel),
                context.IsoCode,
                new SpeechCaptureTelemetryContext(context.Session),
                cancellationToken)
            .ConfigureAwait(false);

        if (confirm.HasValue)
        {
            _evaluationTelemetry.TrackDestructiveCommandResolved(
                context.Session,
                GlobalCommandIds.Cancel,
                confirm.Value);
        }

        if (confirm is true)
        {
            context.StateTracker.RecordCancelled();
            return VoiceCommandDialogResult.Cancelled(
                context.Contract.ApplicationId,
                context.Contract.FormId,
                (await context.Interpreter.GetValuesSnapshotAsync().ConfigureAwait(false))
                    .ToImmutableDictionary(StringComparer.Ordinal));
        }

        return null;
    }
}
