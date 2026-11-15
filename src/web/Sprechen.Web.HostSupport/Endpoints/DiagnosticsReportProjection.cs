using System.Text.Json;

namespace Sprechen.Web.HostSupport.Endpoints;

/// <summary>
/// Beschränkt einen Diagnosebericht auf die vom Endpunkt freigegebenen Feldnamen.
/// Fehlende Felder bleiben im Ergebnis aus, ausdrücklich übergebene Nullwerte bleiben erhalten.
/// Werte freigegebener Felder werden übernommen. Eine inhaltliche oder rekursive Prüfung erfolgt hier nicht.
/// </summary>
internal static class DiagnosticsReportProjection
{
    internal static string Project(JsonElement report, IReadOnlyList<string> loggableFields)
    {
        ArgumentNullException.ThrowIfNull(loggableFields);

        var projected = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in loggableFields)
        {
            if (!report.TryGetProperty(field, out var value))
            {
                continue;
            }

            projected[field] = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.TryGetInt64(out var number) ? number : value.GetDouble(),
                JsonValueKind.True or JsonValueKind.False => value.GetBoolean(),
                JsonValueKind.Null => null,
                // Arrays und Objekte werden für freigegebene Feldnamen als JSON-Text übernommen, ohne ihre Unterfelder zu prüfen.
                _ => value.GetRawText(),
            };
        }

        return JsonSerializer.Serialize(projected);
    }
}
