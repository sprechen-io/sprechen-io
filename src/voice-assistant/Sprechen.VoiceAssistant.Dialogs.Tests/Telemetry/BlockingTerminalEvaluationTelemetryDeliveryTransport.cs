using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal sealed class BlockingTerminalEvaluationTelemetryDeliveryTransport : IEvaluationTelemetryDeliveryTransport
{
    private readonly TaskCompletionSource<EvaluationTelemetryDeliveryResult> _terminal =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _terminalObserved =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task TerminalObserved => _terminalObserved.Task;

    public Task<EvaluationTelemetryDeliveryResult> DeliverAsync(string body, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(body);
        if (document.RootElement.GetProperty(TelemetryEnvelopeFieldNames.Event).GetString() != TelemetryEventNames.TaskEnd)
        {
            return Task.FromResult(Accepted());
        }

        _terminalObserved.TrySetResult();
        return _terminal.Task;
    }

    internal void AcceptTerminal() => _terminal.TrySetResult(Accepted());

    private static EvaluationTelemetryDeliveryResult Accepted()
        => new(EvaluationTelemetryDeliveryStatus.Accepted, 204);
}
