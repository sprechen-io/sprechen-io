using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Liest die Art eines Werttyps. Fehlende oder leere Angaben ergeben null,
/// damit die weitere Zuordnung das Fehlen erkennen kann.
/// </summary>
internal sealed class FormValueTypeKindReader
{
    public string? TryGetValueTypeKind(JsonElement valueType)
    {
        if (valueType.ValueKind != JsonValueKind.Object
            || !valueType.TryGetProperty(ValueTypePropertyNames.Kind, out var kindElement)
            || kindElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var kind = kindElement.GetString();
        return string.IsNullOrWhiteSpace(kind) ? null : kind.Trim();
    }
}
