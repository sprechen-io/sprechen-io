namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Gemeinsame Basis der vier Aktivierungsentscheidungen: Aktivierung,
/// Verknüpfung, Fehlversuch oder lexikalischer Ersatzpfad. Ableitungen
/// sind auf diese Assembly beschränkt.
/// </summary>
public abstract record WakeArbitrationOutcome
{
    private protected WakeArbitrationOutcome()
    {
    }
}
