namespace Sprechen.Speech.State;

/// <summary>
/// Gibt Beobachtern beide Seiten eines Zustandswechsels, ohne den bisherigen Zustand selbst vorhalten zu müssen.
/// </summary>
public readonly record struct StateChanged<TState>(TState Previous, TState Current);


