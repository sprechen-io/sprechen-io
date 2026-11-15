using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, ob ein Wert zu mindestens einem zulässigen Eintrag passt. Bei Mengen beschreibt jeder Eintrag eine vollständige zulässige Menge.
/// </summary>
internal sealed class FormOneOfConstraintEvaluator
{
    public bool IsSatisfied(ConstraintDefinition constraint, FormValue value)
    {
        if (!FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, ConstraintPropertyNames.Values, out var valuesElement)
            || valuesElement.ValueKind != JsonValueKind.Array)
        {
            return true;
        }

        foreach (var element in valuesElement.EnumerateArray())
        {
            // Jeden erwarteten Eintrag als vollständige zulässige Menge normalisieren. Ein
            // skalarer Eintrag wird zur einelementigen Menge. Mindestens ein gleicher Eintrag
            // genügt. Der tatsächliche Mengenwert muss bereits in kanonischer Reihenfolge vorliegen.
            if (value.Kind == FormValueKind.Set)
            {
                if (TryReadSetEntry(element, out var expectedSet) && value.Set.SequenceEqual(expectedSet))
                {
                    return true;
                }

                continue;
            }

            if (!FormRuleEngine.TryParseConstValue(element, out var expected))
            {
                continue;
            }

            if (Equals(value, expected))
            {
                return true;
            }
        }

        return false;
    }

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
            if (item.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var text = item.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            items.Add(text);
        }

        if (items.Count == 0)
        {
            return false;
        }

        entry = FormValue.FromSet([.. items]).Set;
        return true;
    }
}
