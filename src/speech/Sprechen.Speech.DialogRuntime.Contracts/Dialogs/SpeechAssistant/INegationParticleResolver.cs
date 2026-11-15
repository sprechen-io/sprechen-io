namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert die modellierten Negationspartikel zum Erkennen einer abgelehnten Auswahl.
/// Die Menge vereint alle modellierten Sprachen, weil die Sprache eines gesprochenen
/// Kandidaten nicht mit dem Sprachcode des Feldes übereinstimmen muss.
/// </summary>
public interface INegationParticleResolver
{
    IReadOnlyCollection<string> ResolveNegationParticles();
}
