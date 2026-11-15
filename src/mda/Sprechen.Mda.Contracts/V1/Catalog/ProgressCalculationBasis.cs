using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Berechnungsgrundlage für Fortschrittsansagen nach dem Muster x von y.
/// </summary>
[JsonConverter(typeof(ProgressCalculationBasisJsonConverter))]
public enum ProgressCalculationBasis
{
    /// <summary>
    /// Fortschritt über alle Felder des Formulars.
    /// </summary>
    AllFields,

    /// <summary>
    /// Fortschritt ausschließlich über Pflichtfelder.
    /// </summary>
    RequiredFieldsOnly,

    /// <summary>
    /// Fortschritt über abgeschlossene Feldgruppen.
    /// </summary>
    FieldGroupBased
}
