using System.Collections.Immutable;
using System.Globalization;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Liest Rohwerte entsprechend ihrer Modellwertart, damit bereits
/// zusammengesetzte Werte auf Typverträglichkeit geprüft werden können.
/// </summary>
internal sealed class RawFormValueParser
{
    public static RawFormValueParser Default { get; } = new();

    public bool TryParse(string raw, string valueTypeKind, out FormValue value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (string.Equals(valueTypeKind, ValueTypeKinds.Boolean, StringComparison.OrdinalIgnoreCase))
        {
            return TryParseBoolean(raw, out value);
        }

        if (string.Equals(valueTypeKind, ValueTypeKinds.Integer, StringComparison.OrdinalIgnoreCase))
        {
            return TryParseInteger(raw, out value);
        }

        if (string.Equals(valueTypeKind, ValueTypeKinds.Date, StringComparison.OrdinalIgnoreCase))
        {
            if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return false;
            }

            value = FormValue.FromString(raw);
            return true;
        }

        if (string.Equals(valueTypeKind, ValueTypeKinds.Time, StringComparison.OrdinalIgnoreCase))
        {
            if (!TimeOnly.TryParseExact(raw, ["HH:mm", "HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return false;
            }

            value = FormValue.FromString(raw);
            return true;
        }

        if (string.Equals(valueTypeKind, ValueTypeKinds.Set, StringComparison.OrdinalIgnoreCase))
        {
            var items = raw
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToImmutableArray();

            value = FormValue.FromSet(items);
            return true;
        }

        value = FormValue.FromString(raw);
        return true;
    }

    private static bool TryParseBoolean(string raw, out FormValue value)
    {
        value = default;

        if (!bool.TryParse(raw, out var boolean))
        {
            return false;
        }

        value = FormValue.FromBoolean(boolean);
        return true;
    }

    private static bool TryParseInteger(string raw, out FormValue value)
    {
        value = default;

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
        {
            return false;
        }

        value = FormValue.FromInteger(integer);
        return true;
    }
}
