namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Liefert den wirksamen Aktivierungsbestand je Sprache, damit Verbraucher Modellvorgaben und lokale Überschreibungen einheitlich verwenden.
/// </summary>
public interface IWakeWordProvider
{
    /// <summary>
    /// Liefert den nach Überschreibungen und Ersatzregeln wirksamen Aktivierungsbestand für die angeforderte Sprache.
    /// </summary>
    /// <param name="isoCode">Normalisierter oder noch unverarbeiteter Sprachcode nach ISO 639-1.</param>
    /// <param name="cancellationToken">Abbruchsignal des aufrufenden Ablaufs.</param>
    /// <returns>Wirksamer Aktivierungsbestand nach Anwendung lokaler Überschreibungen und Ersatzregeln.</returns>
    Task<WakeWordInventory> GetWakeWordInventoryAsync(string isoCode, CancellationToken cancellationToken);
}

