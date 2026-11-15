using Microsoft.JSInterop;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Voice.EvaluationTelemetry.BrowserInfrastructure;

/// <summary>
/// Erwirbt über JavaScript eine exklusive Browser-Sperre, damit zwei Tabs dieselben Telemetriedaten nicht gleichzeitig versenden.
/// </summary>
internal sealed class BrowserEvaluationTelemetryExclusiveLease(IJSRuntime javascript)
    : IEvaluationTelemetryExclusiveLease
{
    private readonly IJSRuntime _javascript = javascript ?? throw new ArgumentNullException(nameof(javascript));

    public async ValueTask<IEvaluationTelemetryExclusiveLeaseHandle?> TryAcquireAsync(
        string leaseKey,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseKey);
        var leaseId = await _javascript.InvokeAsync<string?>(
            "evaluationTelemetryOutbox.tryAcquireLease",
            cancellationToken,
            [leaseKey]);
        return leaseId is null
            ? null
            : new BrowserEvaluationTelemetryExclusiveLeaseHandle(_javascript, leaseId);
    }
}
