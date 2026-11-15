using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt den aktuellen lexikalischen Aktivierungsersatz bereit und verteilt Änderungen seines Zustands.
/// </summary>
public interface ISpeechWakeListeningFallbackSignals
{
    SpeechWakeListeningFallbackState WakeListeningFallbackState { get; }

    ValueTask SetWakeListeningFallbackStateAsync(SpeechWakeListeningFallbackState state);

    IDisposable SubscribeToWakeListeningFallbackStateChanged(
        Func<SpeechWakeListeningFallbackStateChanged, ValueTask> handler);
}
