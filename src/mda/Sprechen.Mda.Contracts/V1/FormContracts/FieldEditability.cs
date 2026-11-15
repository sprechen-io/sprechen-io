using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Typisierte Bearbeitbarkeit von Feldern. Die Serialisierung verwendet
/// <see cref="EditabilityModes"/>. Unknown kennzeichnet fehlende oder nicht erkannte
/// Werte und darf nicht serialisiert werden.
/// </summary>
[JsonConverter(typeof(FieldEditabilityJsonConverter))]
public enum FieldEditability
{
    /// <summary>
    /// Fehlende oder unbekannte Bearbeitbarkeit. Wird wie der Standard für bearbeitbare
    /// Felder behandelt und darf nicht serialisiert werden.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Durch Nutzende bearbeitbar. Übertragungswert userEditable.
    /// </summary>
    UserEditable,

    /// <summary>
    /// Schreibgeschützt. Übertragungswert readOnly.
    /// </summary>
    ReadOnly,

    /// <summary>
    /// Berechnet. Übertragungswert computed.
    /// </summary>
    Computed
}
