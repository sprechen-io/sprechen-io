using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Liest die Namen erforderlicher Eigenschaften als Menge für die Zuordnung ihres Pflichtstatus.
/// </summary>
internal sealed class SchemaRequiredPropertySetReader
{
    public HashSet<string> Read(JsonElement element)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (!element.TryGetProperty("required", out var requiredElement))
        {
            return result;
        }

        foreach (var item in requiredElement.EnumerateArray())
        {
            if (item.GetString() is string propertyName)
            {
                result.Add(propertyName);
            }
        }

        return result;
    }
}
