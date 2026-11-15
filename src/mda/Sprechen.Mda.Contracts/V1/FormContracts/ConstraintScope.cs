using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Typisierte Geltungsbereiche von Validierungsregeln. Die Serialisierung nutzt
/// <see cref="ConstraintScopes"/>. Unknown bedeutet: angegeben, aber nicht erkannt.
/// Ein fehlender Geltungsbereich wird dagegen als null dargestellt.
/// </summary>
[JsonConverter(typeof(ConstraintScopeJsonConverter))]
public enum ConstraintScope
{
    /// <summary>
    /// Angegebener, aber nicht erkannter Geltungsbereich. Die Vorprüfung meldet ihn.
    /// Er darf nicht serialisiert werden.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Feldbezogen. Übertragungswert field.
    /// </summary>
    Field,

    /// <summary>
    /// Feldgruppenbezogen. Übertragungswert fieldGroup.
    /// </summary>
    FieldGroup,

    /// <summary>
    /// Feldgruppenübergreifend. Übertragungswert crossGroup.
    /// </summary>
    CrossGroup,

    /// <summary>
    /// Formularbezogen. Übertragungswert form.
    /// </summary>
    Form
}
