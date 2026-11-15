using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// <para>DE: Referenz auf ein Feld oder eine untergeordnete Feldgruppe.</para>
/// <para>EN: Reference to a member within a field group (either a field or a nested field group).</para>
/// </summary>
public sealed class FieldGroupMember
{
    /// <summary>
    /// <para>DE: Art des Eintrags (<c>field</c> oder <c>fieldGroup</c>).</para>
    /// <para>EN: Member kind (<c>field</c> or <c>fieldGroup</c>).</para>
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// <para>DE: Feld-ID, wenn <see cref="Kind" /> <c>field</c> ist.</para>
    /// <para>EN: Field ID when <see cref="Kind" /> is <c>field</c>.</para>
    /// </summary>
    [JsonPropertyName("fieldId")]
    public FieldId? FieldId { get; set; }

    /// <summary>
    /// <para>DE: Feldgruppen-ID, wenn <see cref="Kind" /> <c>fieldGroup</c> ist.</para>
    /// <para>EN: Field-group ID when <see cref="Kind" /> is <c>fieldGroup</c>.</para>
    /// </summary>
    [JsonPropertyName("fieldGroupId")]
    public FieldGroupId? FieldGroupId { get; set; }
}


