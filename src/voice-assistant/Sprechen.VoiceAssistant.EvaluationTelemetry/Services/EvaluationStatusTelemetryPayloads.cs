using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Erstellt Ereignisdaten aus Formularständen. Fehlende Navigationsziele und Fortschrittsnenner mit dem Wert 0 werden ausdrücklich gekennzeichnet.
/// </summary>
internal sealed class EvaluationStatusTelemetryPayloads
{
    internal object CreateNavigation(EvaluationFormStatusSnapshot snapshot)
    {
        return new Dictionary<string, object?>
        {
            // Ohne aktuelles Feld oder Gruppe steht der modellierte Platzhalter
            // statt null. So bleibt die Navigation ein gültiges Ereignis, ohne
            // einen Zielgegenstand zu erfinden: die Übergangsprojektion ignoriert
            // diesen Platzhalter bei der Ableitung eines Zielwechsels.
            [TelemetryPayloadProperties.CurrentFieldId] = OrPlaceholder(snapshot.Navigation.CurrentFieldId),
            [TelemetryPayloadProperties.CurrentFieldGroupId] = OrPlaceholder(snapshot.Navigation.CurrentFieldGroupId),
            [TelemetryPayloadProperties.NavigationIndex] = snapshot.Navigation.Index,
            [TelemetryPayloadProperties.NavigationTotal] = snapshot.Navigation.Total,
        };
    }

    internal object CreateStatusSnapshot(EvaluationFormStatusSnapshot snapshot)
    {
        var data = new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.Navigation] = CreateNavigation(snapshot),
            [TelemetryPayloadProperties.Progress] = new Dictionary<string, object?>
            {
                [TelemetryPayloadProperties.AllFields] = CreateProgressCounter(
                    snapshot.Progress.AllFields.Completed,
                    snapshot.Progress.AllFields.Total),
                [TelemetryPayloadProperties.RequiredFieldsOnly] = CreateProgressCounter(
                    snapshot.Progress.RequiredFieldsOnly.Completed,
                    snapshot.Progress.RequiredFieldsOnly.Total),
                [TelemetryPayloadProperties.FieldGroupBased] = CreateProgressCounter(
                    snapshot.Progress.FieldGroupBased.Completed,
                    snapshot.Progress.FieldGroupBased.Total),
            },
        };

        var zeroDenominatorMode = DetermineZeroDenominatorMode(snapshot);
        if (zeroDenominatorMode is not null)
        {
            data[TelemetryPayloadProperties.ZeroDenominatorMode] = zeroDenominatorMode;
        }

        return data;
    }

    internal object CreateFieldRelevanceChanged(EvaluationFieldStatus field)
    {
        return new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.FieldId] = field.FieldId,
            [TelemetryPayloadProperties.FieldGroupId] = field.FieldGroupId,
            [TelemetryPayloadProperties.Relevant] = field.Relevant,
        };
    }

    internal object CreateFieldCompletionChanged(EvaluationFieldStatus field)
    {
        var data = new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.FieldId] = field.FieldId,
            [TelemetryPayloadProperties.FieldGroupId] = field.FieldGroupId,
            [TelemetryPayloadProperties.Relevant] = field.Relevant,
            [TelemetryPayloadProperties.Required] = field.Required,
            [TelemetryPayloadProperties.Completed] = field.Completed,
        };

        if (!field.BlockingErrorCodes.IsDefaultOrEmpty)
        {
            data[TelemetryPayloadProperties.BlockingErrorCodes] = field.BlockingErrorCodes.ToArray();
        }

        return data;
    }

    internal object CreateFieldGroupRelevanceChanged(EvaluationFieldGroupStatus fieldGroup)
    {
        return new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.FieldGroupId] = fieldGroup.FieldGroupId,
            [TelemetryPayloadProperties.Relevant] = fieldGroup.Relevant,
        };
    }

    internal object CreateFieldGroupCompletionChanged(EvaluationFieldGroupStatus fieldGroup)
    {
        var data = new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.FieldGroupId] = fieldGroup.FieldGroupId,
            [TelemetryPayloadProperties.Relevant] = fieldGroup.Relevant,
            [TelemetryPayloadProperties.Completed] = fieldGroup.Completed,
        };

        if (!fieldGroup.OpenGroupErrorCodes.IsDefaultOrEmpty)
        {
            data[TelemetryPayloadProperties.OpenGroupErrorCodes] = fieldGroup.OpenGroupErrorCodes.ToArray();
        }

        return data;
    }

    private static string OrPlaceholder(string? identifier)
        => string.IsNullOrWhiteSpace(identifier)
            ? TelemetryPayloadPlaceholders.UnidentifiedObject
            : identifier;

    private static object CreateProgressCounter(int completed, int total)
    {
        return new Dictionary<string, object?>
        {
            [TelemetryPayloadProperties.Completed] = Math.Max(0, completed),
            [TelemetryPayloadProperties.Total] = Math.Max(0, total),
        };
    }

    private static string? DetermineZeroDenominatorMode(EvaluationFormStatusSnapshot snapshot)
    {
        if (snapshot.Progress.AllFields.Total == 0)
        {
            return TelemetryZeroDenominatorModes.AllFields;
        }

        if (snapshot.Progress.RequiredFieldsOnly.Total == 0)
        {
            return TelemetryZeroDenominatorModes.RequiredFieldsOnly;
        }

        if (snapshot.Progress.FieldGroupBased.Total == 0)
        {
            return TelemetryZeroDenominatorModes.FieldGroupBased;
        }

        return null;
    }
}
