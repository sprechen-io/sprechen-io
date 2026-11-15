using Microsoft.JSInterop;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Voice.EvaluationTelemetry.BrowserInfrastructure;

/// <summary>
/// Gibt die Browser-Sperre bei DisposeAsync über JavaScript frei,
/// damit sie anschließend erneut erworben werden kann.
/// </summary>
internal sealed class BrowserEvaluationTelemetryExclusiveLeaseHandle(
    IJSRuntime javascript,
    string leaseId) : IEvaluationTelemetryExclusiveLeaseHandle
{
    private readonly IJSRuntime _javascript = javascript ?? throw new ArgumentNullException(nameof(javascript));
    private readonly string _leaseId = string.IsNullOrWhiteSpace(leaseId)
        ? throw new ArgumentException("A browser evaluation lease identifier is required.", nameof(leaseId))
        : leaseId;
    private bool _released;

    public async ValueTask DisposeAsync()
    {
        if (_released)
        {
            return;
        }

        await _javascript.InvokeVoidAsync("evaluationTelemetryOutbox.releaseLease", _leaseId);
        _released = true;
    }
}
