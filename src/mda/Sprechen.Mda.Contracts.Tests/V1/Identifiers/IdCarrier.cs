using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests.V1.Identifiers;

/// <summary>
/// Testhülle für typisierte Kennungen als Objekteigenschaften. Der per Attribut
/// registrierte JsonConverter muss sie auch hier als einfache Zeichenketten schreiben.
/// </summary>
internal sealed record IdCarrier(
    [property: JsonPropertyName("FieldId")] FieldId FieldId,
    [property: JsonPropertyName("VariableId")] VariableId VariableId,
    [property: JsonPropertyName("FieldGroupId")] FieldGroupId FieldGroupId,
    [property: JsonPropertyName("CommandId")] CommandId CommandId,
    [property: JsonPropertyName("FormId")] FormId FormId);
