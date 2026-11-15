namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Kennzeichnet, ob eine direkte Aktivierung aus akustischer Detektion
/// oder aus der lexikalischen Auswertung des Transkripts stammt.
/// </summary>
public enum WakeWordActivationSource
{
    Acoustic = 0,
    Lexical = 1,
}
