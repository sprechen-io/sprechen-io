using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Gibt vorbereitete Spur- und Logeinträge für Wake-Word-Ereignisse aus,
/// damit die Ausgabetechnik von deren phasenabhängiger Auswahl getrennt bleibt.
/// </summary>
internal sealed class WakeWordMissTelemetryForwarder
{
    private readonly ISpeechAssistantTrace _trace;
    private readonly ILogger _logger;
    private readonly WakeWordMissTelemetryActionFactory _actionFactory;

    public WakeWordMissTelemetryForwarder(
        ISpeechAssistantTrace trace,
        ILogger logger,
        WakeWordMissTelemetryActionFactory actionFactory)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _actionFactory = actionFactory ?? throw new ArgumentNullException(nameof(actionFactory));
    }

    public ValueTask RecordAsync(WakeWordMissEvent telemetryEvent, CancellationToken cancellationToken)
    {
        var action = _actionFactory.Create(telemetryEvent);
        if (action.LogRecord is { } logRecord)
        {
            _logger.LogDebug(logRecord.Exception, logRecord.Message);
        }

        if (action.TraceRecord is null)
        {
            return ValueTask.CompletedTask;
        }

        var traceRecord = action.TraceRecord.Value;
        return _trace.WriteIfConfiguredAsync(
            telemetryEvent.CorrelationId,
            traceRecord.Scope,
            traceRecord.Level,
            traceRecord.Message,
            cancellationToken);
    }
}
