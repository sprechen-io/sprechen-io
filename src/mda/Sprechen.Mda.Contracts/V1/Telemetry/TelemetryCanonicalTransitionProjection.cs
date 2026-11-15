using System.Collections.Immutable;

namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Bildet Rohereignisse auf kanonische fachliche Zustandsänderungen ab.
/// Ein Interaktionsschritt zählt eine solche Änderung, keine Äußerung oder Ereignisnamen.
/// Die gemeinsame Abbildung hält die Schrittzählung zwischen beiden Bedingungen vergleichbar.
/// Eine fehlende Projektion muss von einer gültigen leeren Übergangsliste unterschieden werden,
/// damit unvollständig erfasste Aufgaben nicht als Aufgaben mit weniger Schritten ausgewertet werden.
/// </summary>
public static class TelemetryCanonicalTransitionProjection
{
    /// <summary>
    /// Ereignisarten, die einen Zustandswechsel enthalten können. Andere Ereignisarten
    /// enthalten vertragsgemäß eine leere Übergangsliste.
    /// </summary>
    public static IReadOnlySet<string> StepCapableEvents { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        TelemetryEventNames.FocusChange,
        TelemetryEventNames.NavigationChanged,
        TelemetryEventNames.ValueChange,
        TelemetryEventNames.FieldCompletionChanged,
        TelemetryEventNames.FieldGroupCompletionChanged,
        TelemetryEventNames.ManualStep,
    };

    /// <summary>
    /// Übergang zum neu fokussierten Feld.
    /// </summary>
    public static ImmutableArray<TelemetryCanonicalTransition> ForFocusChange(string fieldId)
        => Single(TelemetryCanonicalClasses.FieldTargetChanged, TelemetryTransitionScopes.Field, fieldId, TelemetryResultingStates.Targeted);

    /// <summary>
    /// Übergänge zu den angegebenen Feld- und Feldgruppenzielen. Sind beide Kennungen
    /// vorhanden, werden zwei Übergänge geliefert.
    /// </summary>
    public static ImmutableArray<TelemetryCanonicalTransition> ForNavigationChanged(
        string? fieldId,
        string? fieldGroupId)
    {
        var builder = ImmutableArray.CreateBuilder<TelemetryCanonicalTransition>(2);

        if (!string.IsNullOrWhiteSpace(fieldId))
        {
            builder.Add(new TelemetryCanonicalTransition(
                TelemetryCanonicalClasses.FieldTargetChanged,
                TelemetryTransitionScopes.Field,
                fieldId.Trim(),
                TelemetryResultingStates.Targeted));
        }

        if (!string.IsNullOrWhiteSpace(fieldGroupId))
        {
            builder.Add(new TelemetryCanonicalTransition(
                TelemetryCanonicalClasses.FieldGroupTargetChanged,
                TelemetryTransitionScopes.FieldGroup,
                fieldGroupId.Trim(),
                TelemetryResultingStates.Targeted));
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Übergang eines bestätigten Feldwertwechsels, ohne den Feldwert selbst aufzuzeichnen.
    /// </summary>
    public static ImmutableArray<TelemetryCanonicalTransition> ForValueChange(string fieldId)
        => Single(TelemetryCanonicalClasses.ConfirmedFieldValueChanged, TelemetryTransitionScopes.Field, fieldId, TelemetryResultingStates.ConfirmedChanged);

    /// <summary>
    /// Übergang zum angegebenen Vollständigkeitsstand eines Feldes. Der Aufrufer muss
    /// sicherstellen, dass sich der Stand tatsächlich geändert hat.
    /// </summary>
    public static ImmutableArray<TelemetryCanonicalTransition> ForFieldCompletionChanged(string fieldId, bool completed)
        => Single(
            TelemetryCanonicalClasses.FieldCompletionChanged,
            TelemetryTransitionScopes.Field,
            fieldId,
            completed ? TelemetryResultingStates.Complete : TelemetryResultingStates.Incomplete);

    /// <summary>
    /// Übergang zum angegebenen Vollständigkeitsstand einer Feldgruppe.
    /// </summary>
    public static ImmutableArray<TelemetryCanonicalTransition> ForFieldGroupCompletionChanged(string fieldGroupId, bool completed)
        => Single(
            TelemetryCanonicalClasses.FieldGroupCompletionChanged,
            TelemetryTransitionScopes.FieldGroup,
            fieldGroupId,
            completed ? TelemetryResultingStates.Complete : TelemetryResultingStates.Incomplete);

    /// <summary>
    /// Prüft die Kombination aus Änderungsklasse, Geltungsbereich und Ergebniszustand
    /// eines manuell beobachteten Übergangs sowie dessen Objektkennung.
    /// Das Ereignis ist für manuelle Beobachtungen bestimmt. Anwendungen dürfen es nicht
    /// aus Navigation oder Dialogzustand ableiten.
    /// </summary>
    public static ImmutableArray<TelemetryCanonicalTransition> ForManualStep(TelemetryCanonicalTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);

        var validCombination = transition.CanonicalClass switch
        {
            TelemetryCanonicalClasses.FieldTargetChanged =>
                transition.Scope == TelemetryTransitionScopes.Field
                && transition.ResultingState == TelemetryResultingStates.Targeted,
            TelemetryCanonicalClasses.FieldGroupTargetChanged =>
                transition.Scope == TelemetryTransitionScopes.FieldGroup
                && transition.ResultingState == TelemetryResultingStates.Targeted,
            TelemetryCanonicalClasses.ConfirmedFieldValueChanged =>
                transition.Scope == TelemetryTransitionScopes.Field
                && transition.ResultingState == TelemetryResultingStates.ConfirmedChanged,
            TelemetryCanonicalClasses.FieldCompletionChanged =>
                transition.Scope == TelemetryTransitionScopes.Field
                && IsCompletionState(transition.ResultingState),
            TelemetryCanonicalClasses.FieldGroupCompletionChanged =>
                transition.Scope == TelemetryTransitionScopes.FieldGroup
                && IsCompletionState(transition.ResultingState),
            _ => false,
        };

        if (!validCombination)
        {
            throw new ArgumentException(
                "A manual transition must use one modeled class, scope, and resulting-state combination.",
                nameof(transition));
        }

        return Single(
            transition.CanonicalClass,
            transition.Scope,
            transition.ObjectId,
            transition.ResultingState);
    }

    /// <summary>
    /// Explizit leere Übergangsliste für Ereignisse ohne Zustandswechsel.
    /// </summary>
    public static ImmutableArray<TelemetryCanonicalTransition> None => [];

    /// <summary>
    /// Prüft, ob ein Ereignis aus <see cref="StepCapableEvents"/> eine leere Projektion liefert.
    /// Fehler bei der Projektion werden weitergegeben. Bei der Auswertung muss eine fehlende
    /// oder fehlerhafte Projektion die Schrittkennzahl der betroffenen Aufgabe als unbeurteilt markieren.
    /// </summary>
    public static bool IsProjectionMissing(string eventName, IReadOnlyDictionary<string, object?> payload)
        => StepCapableEvents.Contains(eventName) && ForEvent(eventName, payload).IsEmpty;

    /// <summary>
    /// Bildet Ereignisname und aufgezeichnete Nutzlast auf kanonische Übergänge ab.
    /// Die entsprechende TypeScript-Abbildung verwendet dieselbe Zuordnungstabelle.
    /// </summary>
    /// <param name="eventName">Aufgezeichneter Ereignisname.</param>
    /// <param name="payload">Aufgezeichnete Nutzlast mit ihren übertragenen Feldnamen.</param>
    public static ImmutableArray<TelemetryCanonicalTransition> ForEvent(
        string eventName,
        IReadOnlyDictionary<string, object?> payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        // Ein Platzhalter bezeichnet kein identifiziertes Objekt und wird deshalb wie eine fehlende Kennung behandelt.
        string? Text(string name)
            => payload.TryGetValue(name, out var value)
                && value is string text
                && !string.Equals(text.Trim(), TelemetryPayloadPlaceholders.UnidentifiedObject, StringComparison.Ordinal)
                    ? text
                    : null;

        bool Flag(string name)
            => payload.TryGetValue(name, out var value) && value is true;

        return eventName switch
        {
            TelemetryEventNames.FocusChange when Text(TelemetryPayloadProperties.FieldId) is { Length: > 0 } fieldId =>
                ForFocusChange(fieldId),
            TelemetryEventNames.NavigationChanged =>
                ForNavigationChanged(
                    Text(TelemetryPayloadProperties.CurrentFieldId),
                    Text(TelemetryPayloadProperties.CurrentFieldGroupId)),
            TelemetryEventNames.ValueChange when Text(TelemetryPayloadProperties.FieldId) is { Length: > 0 } fieldId =>
                ForValueChange(fieldId),
            TelemetryEventNames.FieldCompletionChanged when Text(TelemetryPayloadProperties.FieldId) is { Length: > 0 } fieldId =>
                ForFieldCompletionChanged(fieldId, Flag(TelemetryPayloadProperties.Completed)),
            TelemetryEventNames.FieldGroupCompletionChanged when Text(TelemetryPayloadProperties.FieldGroupId) is { Length: > 0 } fieldGroupId =>
                ForFieldGroupCompletionChanged(fieldGroupId, Flag(TelemetryPayloadProperties.Completed)),
            TelemetryEventNames.ManualStep => ProjectManualStep(payload),
            _ => None,
        };
    }

    private static ImmutableArray<TelemetryCanonicalTransition> ProjectManualStep(
        IReadOnlyDictionary<string, object?> payload)
    {
        if (!payload.TryGetValue(TelemetryPayloadProperties.CanonicalTransitions, out var raw)
            || raw is not IEnumerable<object?> values)
        {
            throw new ArgumentException(
                "A manual step must carry exactly one complete canonical transition.",
                nameof(payload));
        }

        var items = values.ToArray();
        if (items.Length != 1)
        {
            throw new ArgumentException(
                "A manual step must carry exactly one complete canonical transition.",
                nameof(payload));
        }

        var transition = items[0] switch
        {
            TelemetryCanonicalTransition typed => typed,
            IReadOnlyDictionary<string, object?> properties => new TelemetryCanonicalTransition(
                ReadRequiredString(properties, TelemetryPayloadProperties.CanonicalClass),
                ReadRequiredString(properties, TelemetryPayloadProperties.TransitionScope),
                ReadRequiredString(properties, TelemetryPayloadProperties.ObjectId),
                ReadRequiredString(properties, TelemetryPayloadProperties.ResultingState)),
            _ => throw new ArgumentException(
                "A manual step transition must be a readable object.",
                nameof(payload)),
        };

        return ForManualStep(transition);
    }

    private static string ReadRequiredString(IReadOnlyDictionary<string, object?> properties, string name)
        => properties.Count == 4
            && properties.TryGetValue(name, out var value)
            && value is string text
                ? text
                : throw new ArgumentException($"A manual step transition requires '{name}'.", nameof(properties));

    /// <summary>
    /// Zählt unterschiedliche Deduplizierungsschlüssel mit ordinalem Zeichenvergleich.
    /// Die im Schlüssel enthaltene Zustandsversion unterscheidet wiederkehrende Übergänge.
    /// </summary>
    public static int CountDeduplicated(IEnumerable<string> deduplicationKeys)
    {
        ArgumentNullException.ThrowIfNull(deduplicationKeys);
        return deduplicationKeys.ToHashSet(StringComparer.Ordinal).Count;
    }

    private static ImmutableArray<TelemetryCanonicalTransition> Single(
        string canonicalClass,
        string scope,
        string objectId,
        string resultingState)
    {
        if (string.IsNullOrWhiteSpace(objectId) || objectId.Trim().Length > 128)
        {
            // Eine fehlende oder zu lange Objektkennung ist ungültig. Eine leere Übergangsliste
            // würde dagegen behaupten, das Ereignis habe keinen Zustandswechsel enthalten.
            throw new ArgumentException("A canonical transition needs the identifier of its object.", nameof(objectId));
        }

        return [new TelemetryCanonicalTransition(canonicalClass, scope, objectId.Trim(), resultingState)];
    }

    private static bool IsCompletionState(string resultingState)
        => resultingState is TelemetryResultingStates.Complete or TelemetryResultingStates.Incomplete;
}
