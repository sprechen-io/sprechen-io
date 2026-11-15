using Sprechen.Speech.WakeWord.Contracts;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Wartet vor einer Aufnahme auf die Freigabe der Mikrofonsperre, damit laufende
/// Sprachausgabe oder Earcons die Aufnahme nicht vorzeitig scheitern lassen.
/// Abbruch und Fehler der Sperrsteuerung werden an den Aufrufer weitergegeben.
/// </summary>
internal sealed class ActiveCaptureMicGateGuard
{
    private readonly IMicGateState _micGateState;

    internal ActiveCaptureMicGateGuard(IMicGateState micGateState)
    {
        _micGateState = micGateState ?? throw new ArgumentNullException(nameof(micGateState));
    }

    internal ValueTask WaitUntilCanStartAsync(CancellationToken cancellationToken)
        => _micGateState.WaitUntilOpenAsync(cancellationToken);
}
