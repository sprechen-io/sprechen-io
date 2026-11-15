using System.Globalization;
using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Überführt unterstützte JSON-Werte in die Zeichenfolgenform für Regelaktionen. Bei Listen werden nichtleere Zeichenfolgen getrimmt, ordinal sortiert und mit dem Trennzeichen verbunden.
/// </summary>
internal static class FormActionJsonValueEncoder
{
    internal static bool TryEncodeJsonValue(JsonElement valueElement, out string encoded)
    {
        encoded = string.Empty;

        switch (valueElement.ValueKind)
        {
            case JsonValueKind.String:
                encoded = valueElement.GetString() ?? string.Empty;
                return true;

            case JsonValueKind.Number:
                if (valueElement.TryGetInt32(out var intVal))
                {
                    encoded = intVal.ToString(CultureInfo.InvariantCulture);
                    return true;
                }

                if (valueElement.TryGetDouble(out var doubleVal))
                {
                    encoded = doubleVal.ToString(CultureInfo.InvariantCulture);
                    return true;
                }

                return false;

            case JsonValueKind.True:
                encoded = "true";
                return true;

            case JsonValueKind.False:
                encoded = "false";
                return true;

            case JsonValueKind.Null:
                encoded = string.Empty;
                return true;

            case JsonValueKind.Array:
                encoded = EncodeStringArray(valueElement);
                return true;

            default:
                return false;
        }
    }

    private static string EncodeStringArray(JsonElement valueElement)
    {
        var items = new List<string>();
        foreach (var item in valueElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    items.Add(value.Trim());
                }
            }
        }

        items.Sort(StringComparer.Ordinal);
        return string.Join("|", items);
    }
}
