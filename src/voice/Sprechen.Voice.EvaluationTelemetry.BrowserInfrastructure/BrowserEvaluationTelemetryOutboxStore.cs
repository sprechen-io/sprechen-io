using Microsoft.JSInterop;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Voice.EvaluationTelemetry.BrowserInfrastructure;

/// <summary>
/// Speichert ausstehende Telemetrie synchron im Browser, damit der Schreibaufruf
/// vor dem anschließenden Versand abgeschlossen ist.
/// </summary>
internal sealed class BrowserEvaluationTelemetryOutboxStore : IEvaluationTelemetryOutboxStore
{
    private readonly IJSInProcessRuntime _javascript;

    public BrowserEvaluationTelemetryOutboxStore(IJSRuntime javascript)
    {
        _javascript = javascript as IJSInProcessRuntime
            ?? throw new InvalidOperationException(
                "Evaluation telemetry requires the synchronous WebAssembly JavaScript runtime so persistence precedes delivery.");
    }

    public IReadOnlyList<string> ListKeys()
        => _javascript.Invoke<string[]>(
            "evaluationTelemetryOutbox.keys",
            EvaluationTelemetryOutboxStorageContract.StorageKeyPrefix);

    public string? Read(string key)
        => _javascript.Invoke<string?>("evaluationTelemetryOutbox.read", key);

    public void Write(string key, string checkpoint)
        => _javascript.InvokeVoid("evaluationTelemetryOutbox.write", key, checkpoint);

    public void Delete(string key)
        => _javascript.InvokeVoid("evaluationTelemetryOutbox.delete", key);
}
