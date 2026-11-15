using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Typisierte Eingabemodi gemäß field-definitions.schema.json. Die Serialisierung
/// verwendet die zugehörigen Zeichenketten aus <see cref="InputModes"/>.
/// </summary>
[JsonConverter(typeof(InputModeJsonConverter))]
public enum InputMode
{
    /// <summary>
    /// Normale Spracheingabe.
    /// </summary>
    Voice,

    Keyboard,

    /// <summary>
    /// Buchstabierte Eingabe, Buchstabe für Buchstabe.
    /// </summary>
    Spelling,

    /// <summary>
    /// Ziffernweise Eingabe.
    /// </summary>
    Digits,

    /// <summary>
    /// Wörtliche Transkription.
    /// </summary>
    Literal
}
