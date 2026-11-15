using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Leitet aus projizierten Beobachtungen den übernommenen Aufgabenstand und
/// die auszugebenden Übergänge ab. Der anfängliche Abschlussstatus von Feldern
/// und Gruppen wird ohne neues Ereignis übernommen. Wiederholte Wertänderungen
/// bleiben eigene Vorkommnisse.
/// </summary>
internal static class EvaluationTelemetryTransitionAdoptionReducer
{
    private const char MemberSeparator = '\u001F';

    internal static EvaluationTelemetryTransitionAdoptionResult Advance(
        int stateVersion,
        IReadOnlyList<string>? adoptedTransitionIdentities,
        IReadOnlyList<TelemetryCanonicalTransition> projectedTransitions)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stateVersion);
        ArgumentNullException.ThrowIfNull(projectedTransitions);

        var incoming = projectedTransitions.Select(BuildIdentity).ToArray();
        if (incoming.Length == 0)
        {
            return new(stateVersion, adoptedTransitionIdentities, []);
        }

        if (adoptedTransitionIdentities is null)
        {
            return new(stateVersion, incoming, projectedTransitions);
        }

        var adoptedByDimension = adoptedTransitionIdentities.ToDictionary(
            BuildDimension,
            static identity => identity,
            StringComparer.Ordinal);
        if (projectedTransitions.Any(IsOccurrenceTransition))
        {
            foreach (var identity in incoming)
            {
                adoptedByDimension[BuildDimension(identity)] = identity;
            }

            return new(
                checked(stateVersion + 1),
                [.. adoptedByDimension.Values],
                projectedTransitions);
        }

        var hasConflictingDimension = incoming.Any(candidate => adoptedTransitionIdentities.Any(current =>
            string.Equals(BuildDimension(candidate), BuildDimension(current), StringComparison.Ordinal)
            && !string.Equals(candidate, current, StringComparison.Ordinal)));
        var hasExactOverlap = incoming.Any(candidate =>
            adoptedTransitionIdentities.Contains(candidate, StringComparer.Ordinal));
        if (!hasConflictingDimension && hasExactOverlap)
        {
            var newlyEmitted = projectedTransitions
                .Where(transition => !adoptedTransitionIdentities.Contains(
                    BuildIdentity(transition),
                    StringComparer.Ordinal))
                .ToArray();
            return new(
                stateVersion,
                [.. adoptedTransitionIdentities.Concat(incoming).Distinct(StringComparer.Ordinal)],
                newlyEmitted);
        }

        var emitted = projectedTransitions
            .Where(transition =>
            {
                var identity = BuildIdentity(transition);
                return !adoptedByDimension.TryGetValue(BuildDimension(identity), out var current)
                    || !string.Equals(current, identity, StringComparison.Ordinal);
            })
            .ToArray();
        foreach (var identity in incoming)
        {
            adoptedByDimension[BuildDimension(identity)] = identity;
        }

        return new(
            checked(stateVersion + 1),
            [.. adoptedByDimension.Values],
            emitted);
    }

    internal static EvaluationTelemetryTransitionAdoptionResult PrimeCompletionBaseline(
        int stateVersion,
        IReadOnlyList<string>? adoptedTransitionIdentities,
        IReadOnlyList<TelemetryCanonicalTransition> completionTransitions)
    {
        ArgumentNullException.ThrowIfNull(completionTransitions);
        if (completionTransitions.Any(static transition => !IsCompletionTransition(transition)))
        {
            throw new InvalidDataException("Only completion transitions may be adopted as a baseline.");
        }

        return PrimeCompletionBaselineIdentities(
            stateVersion,
            adoptedTransitionIdentities,
            completionTransitions.Select(BuildIdentity).ToArray());
    }

    internal static EvaluationTelemetryTransitionAdoptionResult PrimeCompletionBaselineIdentities(
        int stateVersion,
        IReadOnlyList<string>? adoptedTransitionIdentities,
        IReadOnlyList<string> completionTransitionIdentities)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stateVersion);
        ArgumentNullException.ThrowIfNull(completionTransitionIdentities);
        if (completionTransitionIdentities.Count == 0)
        {
            return new(stateVersion, adoptedTransitionIdentities, []);
        }

        var adopted = adoptedTransitionIdentities?.ToList() ?? [];
        var adoptedByDimension = adopted.ToDictionary(
            BuildDimension,
            static identity => identity,
            StringComparer.Ordinal);
        var incomingDimensions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var identity in completionTransitionIdentities)
        {
            var members = ParseIdentity(identity);
            ValidateCompletionIdentityMembers(members);

            var dimension = BuildDimension(identity);
            if (!incomingDimensions.Add(dimension))
            {
                throw new InvalidDataException("A completion baseline contains a duplicate transition dimension.");
            }

            if (adoptedByDimension.TryGetValue(dimension, out var current))
            {
                if (!string.Equals(current, identity, StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        "A completion baseline conflicts with the durable transition adoption state.");
                }

                continue;
            }

            adopted.Add(identity);
            adoptedByDimension.Add(dimension, identity);
        }

        return new(stateVersion, adopted, []);
    }

    private static bool IsOccurrenceTransition(TelemetryCanonicalTransition transition)
        => string.Equals(
            transition.CanonicalClass,
            TelemetryCanonicalClasses.ConfirmedFieldValueChanged,
            StringComparison.Ordinal);

    private static bool IsCompletionTransition(TelemetryCanonicalTransition transition)
        => IsCompletionClass(transition.CanonicalClass);

    private static bool IsCompletionClass(string canonicalClass)
        => canonicalClass is TelemetryCanonicalClasses.FieldCompletionChanged
            or TelemetryCanonicalClasses.FieldGroupCompletionChanged;

    private static void ValidateCompletionIdentityMembers(IReadOnlyList<string> members)
    {
        var hasSupportedResult = members[3] is "complete" or "incomplete";
        var hasMatchingClassAndScope =
            members[0] == TelemetryCanonicalClasses.FieldCompletionChanged && members[1] == "field"
            || members[0] == TelemetryCanonicalClasses.FieldGroupCompletionChanged && members[1] == "field_group";
        if (!hasSupportedResult || !hasMatchingClassAndScope)
        {
            throw new InvalidDataException(
                "A completion transition identity has an unsupported class, scope, or resulting state.");
        }

        if (!string.Equals(members[2], members[2].Trim(), StringComparison.Ordinal)
            || members[2].Length > 128)
        {
            throw new InvalidDataException("A completion transition identity has an invalid object id.");
        }
    }

    internal static string BuildIdentity(TelemetryCanonicalTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        return string.Join(
            MemberSeparator,
            transition.CanonicalClass,
            transition.Scope,
            transition.ObjectId,
            transition.ResultingState);
    }

    internal static string BuildDimension(string identity)
    {
        var members = ParseIdentity(identity);

        return members[0] switch
        {
            TelemetryCanonicalClasses.FieldTargetChanged or
                TelemetryCanonicalClasses.FieldGroupTargetChanged => string.Join(MemberSeparator, members[..2]),
            TelemetryCanonicalClasses.FieldCompletionChanged or
                TelemetryCanonicalClasses.FieldGroupCompletionChanged or
                TelemetryCanonicalClasses.ConfirmedFieldValueChanged => string.Join(MemberSeparator, members[..3]),
            _ => throw new InvalidDataException(
                $"Canonical transition adoption class '{members[0]}' is not supported."),
        };
    }

    private static string[] ParseIdentity(string identity)
    {
        var members = identity.Split(MemberSeparator);
        if (members.Length != 4 || members.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("A canonical transition adoption identity is malformed.");
        }

        return members;
    }
}
