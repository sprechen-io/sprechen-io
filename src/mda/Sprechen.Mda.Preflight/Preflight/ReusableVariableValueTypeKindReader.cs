using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Liest das Zeichenkettenmitglied <c>kind</c> aus einem JSON-Werttypobjekt
/// oder liefert null, wenn es fehlt oder nicht als Zeichenkette vorliegt.
/// </summary>
internal sealed class ReusableVariableValueTypeKindReader
{
    public string? TryRead(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(ValueTypePropertyNames.Kind, out var kindElement)
            && kindElement.ValueKind == JsonValueKind.String
            ? kindElement.GetString()
            : null;
    }
}
