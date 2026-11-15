using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Typisierte Schweregrade von Validierungsregeln gemäß constraint-language.schema.json.
/// Die Serialisierung nutzt <see cref="SeverityLevels"/>. Unknown kennzeichnet
/// nicht erkannte Werte und darf nicht serialisiert werden.
/// </summary>
[JsonConverter(typeof(ConstraintSeverityJsonConverter))]
public enum ConstraintSeverity
{
    /// <summary>
    /// Standardwert für einen fehlenden oder unbekannten Schweregrad. Die Vorprüfung
    /// meldet ihn. Er darf nicht serialisiert werden.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Fehler. Übertragungswert error.
    /// </summary>
    Error,

    /// <summary>
    /// Warnung. Übertragungswert warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Information. Übertragungswert info.
    /// </summary>
    Info
}
