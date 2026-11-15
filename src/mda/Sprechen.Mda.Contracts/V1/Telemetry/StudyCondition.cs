using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Kennzeichnet die Studienbedingung eines Telemetrie-Ereignisses.
/// </summary>
/// <remarks>
/// Der Converter schreibt <c>"A"</c> und <c>"B"</c> statt der Ordinalzahlen. Das Modell deklariert
/// dieses Mitglied als String.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<StudyCondition>))]
public enum StudyCondition
{
    /// <summary>
    /// Bedingung A (Referenzablauf).
    /// </summary>
    A,

    /// <summary>
    /// Bedingung B (Sprachprototyp).
    /// </summary>
    B,
}
