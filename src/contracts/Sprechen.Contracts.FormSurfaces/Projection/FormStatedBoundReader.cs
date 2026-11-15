using System.Globalization;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Liest numerische und Datumsgrenzen und weist ungültige Angaben zurück, damit eine vorhandene Regelgrenze nicht unbemerkt entfällt.
/// </summary>
internal sealed class FormStatedBoundReader
{
    // Zahlengrenzen bleiben im vorgegebenen 32-Bit-Bereich,
    // damit die Oberfläche denselben Wertebereich wie das Modell verwendet.
    private const long SmallestWholeNumber = -2147483648;
    private const long LargestWholeNumber = 2147483647;

    private const string CalendarDateFormat = "yyyy-MM-dd";

    private static readonly IReadOnlyDictionary<ConstraintKind, FormBoundedRuleReading> ReadingsByKind =
        new Dictionary<ConstraintKind, FormBoundedRuleReading>
        {
            [ConstraintKind.Min] = new(ConstraintPropertyNames.Min, null, BoundsAreCalendarDates: false),
            [ConstraintKind.Max] = new(null, ConstraintPropertyNames.Max, BoundsAreCalendarDates: false),
            [ConstraintKind.Range] = new(ConstraintPropertyNames.Min, ConstraintPropertyNames.Max, BoundsAreCalendarDates: false),
            [ConstraintKind.DateMin] = new(ConstraintPropertyNames.MinDate, null, BoundsAreCalendarDates: true),
            [ConstraintKind.DateMax] = new(null, ConstraintPropertyNames.MaxDate, BoundsAreCalendarDates: true),
            [ConstraintKind.DateRange] = new(ConstraintPropertyNames.MinDate, ConstraintPropertyNames.MaxDate, BoundsAreCalendarDates: true)
        };

    public FormBoundedRuleReading? TryGetReading(ConstraintKind kind)
        => ReadingsByKind.TryGetValue(kind, out var reading) ? reading : null;

    public FormStatedBound? Read(ConstraintDefinition rule, string boundName, bool boundsAreCalendarDates)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentException.ThrowIfNullOrWhiteSpace(boundName);

        if (rule.AdditionalProperties is null
            || !rule.AdditionalProperties.TryGetValue(boundName, out var authored))
        {
            return null;
        }

        var bound = boundsAreCalendarDates ? ReadCalendarDate(authored) : ReadWholeNumber(authored);
        return bound ?? throw new InvalidOperationException(
            $"Form contract rule '{rule.ConstraintId}' states a {boundName} bound nobody can read.");
    }

    // Ganzzahlige Grenzen dürfen als JSON-Zahl oder Zeichenkette vorliegen.
    // Beide Formen erhalten dieselbe numerische Auswertung.
    private static FormStatedBound? ReadWholeNumber(JsonElement authored)
    {
        var candidate = authored.ValueKind switch
        {
            JsonValueKind.Number => authored.TryGetInt64(out var number) ? number : null,
            JsonValueKind.String => long.TryParse(
                (authored.GetString() ?? string.Empty).Trim(),
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var parsed)
                ? parsed
                : null,
            _ => (long?)null
        };

        return candidate is null || candidate < SmallestWholeNumber || candidate > LargestWholeNumber
            ? null
            : new FormStatedBound(candidate.Value, candidate.Value.ToString(CultureInfo.InvariantCulture));
    }

    // Neben dem Format yyyy-MM-dd wird das Kalenderdatum geprüft,
    // damit ungültige Tages- und Monatsangaben keine Grenze bilden.
    private static FormStatedBound? ReadCalendarDate(JsonElement authored)
    {
        if (authored.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var markupText = (authored.GetString() ?? string.Empty).Trim();
        return DateOnly.TryParseExact(markupText, CalendarDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? new FormStatedBound(date.DayNumber, markupText)
            : null;
    }
}
