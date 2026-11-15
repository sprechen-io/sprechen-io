using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Ergänzt Ereignisdaten um die gemeinsam definierten kanonischen Übergänge. Auch eine leere Übergangsliste wird ausdrücklich geschrieben, damit sie von einem fehlenden Merkmal unterscheidbar bleibt.
/// </summary>
internal static class EvaluationTelemetryTransitionAttacher
{
    internal static (object? Payload, IReadOnlyList<TelemetryCanonicalTransition> Transitions) Attach(
        string eventName,
        object? data)
    {
        var payload = data as IDictionary<string, object?>;
        if (payload is null && data is not null)
        {
            // Eine unlesbare Nutzlast darf nicht ohne Übergangsmerkmal weiterlaufen:
            // sonst könnte die Auswertung eine fehlende Projektion als leere lesen.
            throw new InvalidOperationException(
                $"Telemetry payload of '{eventName}' is not a readable dictionary, so its canonical transitions cannot be attached.");
        }

        var attached = payload is null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(payload, StringComparer.Ordinal);

        var transitions = Project(eventName, attached);
        attached[TelemetryPayloadProperties.CanonicalTransitions] = ToWire(transitions);

        return (attached, transitions);
    }

    private static IReadOnlyList<object> ToWire(IReadOnlyList<TelemetryCanonicalTransition> transitions)
    {
        return
        [
            .. transitions.Select(transition => (object)new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [TelemetryPayloadProperties.CanonicalClass] = transition.CanonicalClass,
                [TelemetryPayloadProperties.TransitionScope] = transition.Scope,
                [TelemetryPayloadProperties.ObjectId] = transition.ObjectId,
                [TelemetryPayloadProperties.ResultingState] = transition.ResultingState,
            }),
        ];
    }

    internal static object ReplaceTransitions(
        object? payload,
        IReadOnlyList<TelemetryCanonicalTransition> transitions)
    {
        ArgumentNullException.ThrowIfNull(transitions);
        if (payload is not IDictionary<string, object?> readable)
        {
            throw new InvalidOperationException("An attached telemetry payload must remain a readable dictionary.");
        }

        var replaced = new Dictionary<string, object?>(readable, StringComparer.Ordinal)
        {
            [TelemetryPayloadProperties.CanonicalTransitions] = ToWire(transitions),
        };
        return replaced;
    }

    private static IReadOnlyList<TelemetryCanonicalTransition> Project(string eventName, IReadOnlyDictionary<string, object?> payload)
        => TelemetryCanonicalTransitionProjection.ForEvent(eventName, payload);
}
