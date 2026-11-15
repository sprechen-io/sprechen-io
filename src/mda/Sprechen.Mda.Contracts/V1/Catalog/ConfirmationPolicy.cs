using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Legt fest, wann eine Benutzerauswahl ausdrücklich bestätigt werden muss.
/// </summary>
[JsonConverter(typeof(ConfirmationPolicyJsonConverter))]
public enum ConfirmationPolicy
{
    /// <summary>
    /// Jede Auswahl muss ausdrücklich bestätigt werden.
    /// </summary>
    Always,

    /// <summary>
    /// Bestätigung bei unsicherer Erkennung, beispielsweise niedriger STT-Konfidenz.
    /// </summary>
    Ambiguous,

    /// <summary>
    /// Keine Bestätigung für eindeutige Kandidaten mit hinreichender STT-Konfidenz.
    /// Mehrdeutigkeit, Konfidenz unterhalb der Akzeptanzschwelle und fehlende Konfidenz
    /// müssen vor dem Start weiterhin aufgelöst werden.
    /// </summary>
    Never
}
