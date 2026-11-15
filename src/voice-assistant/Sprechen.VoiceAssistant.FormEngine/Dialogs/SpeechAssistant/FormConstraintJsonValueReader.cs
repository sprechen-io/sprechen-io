using System.Globalization;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Liest Zusatzparameter von Bedingungen und wandelt unterstützte JSON- beziehungsweise Formularwerte in Zeichenfolgen, Ganzzahlen und Datumswerte um.
/// </summary>
internal static class FormConstraintJsonValueReader
{
    private const string IsoDateFormat = "yyyy-MM-dd";

    internal static bool TryGetAdditionalProperty(ConstraintDefinition constraint, string propertyName, out JsonElement element)
    {
        element = default;

        return constraint.AdditionalProperties is not null
            && constraint.AdditionalProperties.TryGetValue(propertyName, out element);
    }

    internal static bool TryGetString(JsonElement element, out string value)
    {
        value = string.Empty;

        if (element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rawValue = element.GetString();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        value = rawValue.Trim();
        return true;
    }

    internal static bool TryGetInt32(JsonElement element, out int value)
    {
        value = default;

        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetInt32(out value);
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rawValue = element.GetString();
        return !string.IsNullOrWhiteSpace(rawValue)
            && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    internal static bool TryGetDateOnly(JsonElement element, out DateOnly value)
    {
        value = default;

        if (!TryGetString(element, out var rawValue))
        {
            return false;
        }

        return DateOnly.TryParseExact(rawValue, IsoDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    internal static bool TryGetDateOnly(FormValue value, out DateOnly date)
    {
        date = default;

        return value.Kind == FormValueKind.String
            && !string.IsNullOrWhiteSpace(value.String)
            && DateOnly.TryParseExact(value.String, IsoDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
