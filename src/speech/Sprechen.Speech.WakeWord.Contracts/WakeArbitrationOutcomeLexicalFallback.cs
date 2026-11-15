namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Fordert den lexikalischen Aktivierungspfad mit einem angegebenen Ersatzgrund an.
/// <see cref="LexicalWakeFallbackReason.None"/> bezeichnet keinen Ersatzgrund
/// und ist für dieses Ergebnis nicht vorgesehen.
/// </summary>
public sealed record WakeArbitrationOutcomeLexicalFallback : WakeArbitrationOutcome
{
    public WakeArbitrationOutcomeLexicalFallback(LexicalWakeFallbackReason fallbackReason)
    {
        FallbackReason = fallbackReason;
    }

    public LexicalWakeFallbackReason FallbackReason { get; }
}
