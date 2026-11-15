using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// <para>DE: Referenz auf das Ziel eines Constraints (Feld/Variable/Formular).</para>
/// <para>EN: Reference to the target of a constraint (field/variable/form).</para>
/// </summary>
public sealed class ConstraintTargetRef
{
    /// <summary>
    /// <para>DE: Zieltyp (z. B. <c>field</c>, <c>variable</c>, <c>form</c>).</para>
    /// <para>EN: Target kind (e.g. <c>field</c>, <c>variable</c>, <c>form</c>).</para>
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// <para>DE: Ziel-Feld-ID, wenn <see cref="Kind" /> <c>field</c> ist.</para>
    /// <para>EN: Target field ID when <see cref="Kind" /> is <c>field</c>.</para>
    /// </summary>
    [JsonPropertyName("fieldId")]
    public FieldId? FieldId { get; set; }

    /// <summary>
    /// <para>DE: Ziel-Variablen-ID, wenn <see cref="Kind" /> <c>variable</c> ist.</para>
    /// <para>EN: Target variable ID when <see cref="Kind" /> is <c>variable</c>.</para>
    /// </summary>
    [JsonPropertyName("variableId")]
    public VariableId? VariableId { get; set; }
}
