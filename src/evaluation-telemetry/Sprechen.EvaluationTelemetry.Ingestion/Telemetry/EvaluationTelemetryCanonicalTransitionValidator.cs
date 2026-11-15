using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Prüft die aufgezeichneten Übergänge gegen die Projektion der Ereignisdaten.
/// </summary>
internal sealed class EvaluationTelemetryCanonicalTransitionValidator
{
    private static readonly IReadOnlySet<string> TransitionPropertyNames = new HashSet<string>(StringComparer.Ordinal)
    {
        TelemetryPayloadProperties.CanonicalClass,
        TelemetryPayloadProperties.TransitionScope,
        TelemetryPayloadProperties.ObjectId,
        TelemetryPayloadProperties.ResultingState,
    };

    /// <summary>
    /// Prüft, ob die aufgezeichneten Übergänge eine geordnete Teilmenge
    /// der aus den Ereignisdaten abgeleiteten Übergänge sind.
    /// </summary>
    public bool Validate(string eventName, JsonElement? data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        if (data is not { ValueKind: JsonValueKind.Object } dataElement)
        {

            return false;
        }

        if (!TryReadRecordedTransitions(dataElement, out var recordedTransitions))
        {
            return false;
        }

        try
        {
            // Bereits übernommene Übergänge können in der Aufzeichnung fehlen.
            // Jeder verbleibende Eintrag muss in derselben Reihenfolge aus dem Ereignis ableitbar sein.
            var projectedTransitions = ProjectExpectedTransitions(eventName, dataElement, recordedTransitions);
            return IsOrderedSubset(recordedTransitions, projectedTransitions);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static ImmutableArray<TelemetryCanonicalTransition> ProjectExpectedTransitions(
        string eventName,
        JsonElement data,
        IReadOnlyList<TelemetryCanonicalTransition> recordedTransitions)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in data.EnumerateObject())
        {
            payload[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when property.Value.TryGetInt32(out var integer) => integer,
                JsonValueKind.Null => null,
                _ => property.Value,
            };
        }

        payload[TelemetryPayloadProperties.CanonicalTransitions] =
            recordedTransitions.Cast<object?>().ToArray();
        return TelemetryCanonicalTransitionProjection.ForEvent(eventName, payload);
    }

    private static bool IsOrderedSubset(
        IReadOnlyList<TelemetryCanonicalTransition> recorded,
        IReadOnlyList<TelemetryCanonicalTransition> projected)
    {
        var projectedIndex = 0;
        foreach (var transition in recorded)
        {
            while (projectedIndex < projected.Count
                && !Equals(projected[projectedIndex], transition))
            {
                projectedIndex++;
            }

            if (projectedIndex == projected.Count)
            {
                return false;
            }

            projectedIndex++;
        }

        return true;
    }

    private static bool TryReadRecordedTransitions(
        JsonElement data,
        out ImmutableArray<TelemetryCanonicalTransition> transitions)
    {
        transitions = [];
        if (!data.TryGetProperty(TelemetryPayloadProperties.CanonicalTransitions, out var transitionsElement)
            || transitionsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var builder = ImmutableArray.CreateBuilder<TelemetryCanonicalTransition>(transitionsElement.GetArrayLength());
        foreach (var transitionElement in transitionsElement.EnumerateArray())
        {
            if (!TryReadTransition(transitionElement, out var transition))
            {
                return false;
            }

            builder.Add(transition);
        }

        transitions = builder.ToImmutable();
        return true;
    }

    private static bool TryReadTransition(JsonElement element, out TelemetryCanonicalTransition transition)
    {
        transition = null!;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var propertyNames = element.EnumerateObject().Select(property => property.Name).ToArray();
        if (propertyNames.Length != TransitionPropertyNames.Count
            || propertyNames.Distinct(StringComparer.Ordinal).Count() != TransitionPropertyNames.Count
            || propertyNames.Any(propertyName => !TransitionPropertyNames.Contains(propertyName))
            || !TryReadString(element, TelemetryPayloadProperties.CanonicalClass, out var canonicalClass)
            || !TryReadString(element, TelemetryPayloadProperties.TransitionScope, out var scope)
            || !TryReadString(element, TelemetryPayloadProperties.ObjectId, out var objectId)
            || !TryReadString(element, TelemetryPayloadProperties.ResultingState, out var resultingState)
            || objectId.Length > 128)
        {
            return false;
        }

        transition = new TelemetryCanonicalTransition(canonicalClass, scope, objectId, resultingState);
        return true;
    }

    private static bool TryReadString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString()!;
        return !string.IsNullOrWhiteSpace(value);
    }
}
