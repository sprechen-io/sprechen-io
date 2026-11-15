using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft skalare und mengenbezogene Gleichheitsbedingungen. Erwartete Mengen werden normalisiert. Tatsächliche Mengenwerte müssen bereits kanonisch vorliegen.
/// </summary>
internal sealed class FormEqualitySetValueConstraintEvaluator
{
    internal bool Supports(string kind)
    {
        return string.Equals(kind, ConstraintKinds.Equals, StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, ConstraintKinds.NotEquals, StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsSatisfied(string kind, ConstraintDefinition constraint, FormValue value)
    {
        if (string.Equals(kind, ConstraintKinds.Equals, StringComparison.OrdinalIgnoreCase))
        {
            return IsEqualsSatisfied(constraint, value);
        }

        if (string.Equals(kind, ConstraintKinds.NotEquals, StringComparison.OrdinalIgnoreCase))
        {
            return IsNotEqualsSatisfied(constraint, value);
        }

        throw new InvalidOperationException($"Unsupported equality constraint kind '{kind}'.");
    }

    private static bool IsEqualsSatisfied(ConstraintDefinition constraint, FormValue value)
    {
        if (!FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, ConstraintPropertyNames.Value, out var expectedElement))
        {
            return true;
        }

        if (value.Kind == FormValueKind.Set)
        {
            return TryReadSetEntry(expectedElement, out var expectedSet)
                && value.Set.SequenceEqual(expectedSet);
        }

        if (!FormRuleEngine.TryParseConstValue(expectedElement, out var expectedValue))
        {
            return false;
        }

        return Equals(value, expectedValue);
    }

    private static bool IsNotEqualsSatisfied(ConstraintDefinition constraint, FormValue value)
    {
        if (!FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, ConstraintPropertyNames.Value, out var expectedElement))
        {
            return true;
        }

        if (value.Kind == FormValueKind.Set)
        {
            return TryReadSetEntry(expectedElement, out var expectedSet)
                && !value.Set.SequenceEqual(expectedSet);
        }

        if (!FormRuleEngine.TryParseConstValue(expectedElement, out var expectedValue))
        {
            return false;
        }

        return !Equals(value, expectedValue);
    }

    // Den erwarteten Eintrag als vollständige zulässige Menge normalisieren. Ein
    // skalarer Eintrag wird zur einelementigen Menge. Der anschließende Folgenvergleich
    // setzt auch beim tatsächlichen Wert die kanonische Mengenreihenfolge voraus.
    private static bool TryReadSetEntry(JsonElement element, out ImmutableArray<string> entry)
    {
        entry = [];

        if (element.ValueKind == JsonValueKind.String)
        {
            var single = element.GetString();
            if (string.IsNullOrWhiteSpace(single))
            {
                return false;
            }

            entry = FormValue.FromSet([single]).Set;
            return true;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var items = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
            {
                return false;
            }

            items.Add(item.GetString()!);
        }

        if (items.Count == 0)
        {
            return false;
        }

        entry = FormValue.FromSet([.. items]).Set;
        return true;
    }
}
