using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Liest Enum-Werte als Zeichenketten, damit der Composer zulässige Auswahlwerte darstellen kann.
/// </summary>
internal sealed class SchemaElementEnumValueReader
{
    public IReadOnlyList<string>? Read(JsonElement element)
    {
        return element.TryGetProperty("enum", out var enumElement)
            ? enumElement.EnumerateArray().Select(value => value.ToString()).ToList()
            : null;
    }
}
