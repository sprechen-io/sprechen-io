using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Liest boolesche Werte, Ganzzahlen und Texte aus skalaren JSON-Konstanten als Formularwerte.
/// </summary>
internal static class FormRuleConstValueParser
{
    public static bool TryParse(JsonElement raw, out FormValue value)
    {
        value = default;

        switch (raw.ValueKind)
        {
            case JsonValueKind.True:
                value = FormValue.FromBoolean(true);
                return true;

            case JsonValueKind.False:
                value = FormValue.FromBoolean(false);
                return true;

            case JsonValueKind.Number:
                if (raw.TryGetInt32(out var integer))
                {
                    value = FormValue.FromInteger(integer);
                    return true;
                }

                return false;

            case JsonValueKind.String:
                var str = raw.GetString();
                if (str is not null)
                {
                    value = FormValue.FromString(str);
                    return true;
                }

                return false;

            default:
                return false;
        }
    }
}
