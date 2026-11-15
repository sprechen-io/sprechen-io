using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Kandidatentreffer nach exaktem Namen, teilweisem Namen und Kennung. Diese gemeinsame
/// Reihenfolge hält vollständige Auswahl und Eingrenzung einer Mehrdeutigkeit konsistent.
/// </summary>
public sealed class SelectionCandidateMatcher<TSelection>
    where TSelection : class
{
    private readonly Func<TSelection, string?> _identifierSelector;
    private readonly Func<TSelection, string?> _displayNameSelector;
    private readonly Func<string, string> _compactNormalizer;

    /// <summary>
    /// Übernimmt Funktionen zum Ermitteln von Kennung und Anzeigename sowie die
    /// gemeinsame Normalisierung für den Kandidatenvergleich.
    /// </summary>
    public SelectionCandidateMatcher(
        Func<TSelection, string?> identifierSelector,
        Func<TSelection, string?> displayNameSelector,
        Func<string, string> compactNormalizer)
    {
        _identifierSelector = identifierSelector ?? throw new ArgumentNullException(nameof(identifierSelector));
        _displayNameSelector = displayNameSelector ?? throw new ArgumentNullException(nameof(displayNameSelector));
        _compactNormalizer = compactNormalizer ?? throw new ArgumentNullException(nameof(compactNormalizer));
    }

    /// <summary>
    /// Projiziert einen Katalog in einen geprüften Kandidatenbestand mit stabilen, bei eins
    /// beginnenden Nummern in Quellreihenfolge.
    /// </summary>
    public SelectionCandidateSnapshot<TSelection> CreateSnapshot(
        IEnumerable<TSelection> items,
        SelectionRevision revision)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(revision);
        RejectDefaultImmutableArray(items);

        var candidates = ImmutableArray.CreateBuilder<SelectionCandidate<TSelection>>();
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        var stableNumber = 0;

        foreach (var item in items)
        {
            if (item is null)
            {
                throw new ArgumentException("A selection catalog cannot contain null items.", nameof(items));
            }

            var identifier = ValidateProjection(
                _identifierSelector(item),
                "identifier",
                nameof(items));
            var displayName = ValidateProjection(
                _displayNameSelector(item),
                "display name",
                nameof(items));

            if (!identifiers.Add(identifier))
            {
                throw new ArgumentException(
                    "A selection catalog cannot contain duplicate ordinal identifiers.",
                    nameof(items));
            }

            stableNumber = checked(stableNumber + 1);
            candidates.Add(SelectionCandidate<TSelection>.Create(
                item,
                identifier,
                displayName,
                stableNumber,
                revision));
        }

        return SelectionCandidateSnapshot<TSelection>.Create(revision, candidates.ToImmutable());
    }

    /// <summary>
    /// Prüft einen vollständigen oder eingegrenzten Bestand in fester Trefferstufenfolge und liefert
    /// die Kandidaten der zuerst erfolgreichen Stufe.
    /// </summary>
    public SelectionCandidateMatch<TSelection> Match(
        SelectionCandidateSnapshot<TSelection> snapshot,
        string? target)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var normalizedTarget = NormalizeOptionalTarget(target);
        if (normalizedTarget.Length == 0)
        {
            return SelectionCandidateMatch<TSelection>.None(snapshot.Revision);
        }

        var normalizedCandidates = snapshot.Candidates
            .Select(candidate => (
                Candidate: candidate,
                Identifier: NormalizeCandidateProjection(candidate.Identifier, nameof(candidate.Identifier)),
                DisplayName: NormalizeCandidateProjection(candidate.DisplayName, nameof(candidate.DisplayName))))
            .ToImmutableArray();

        var exact = normalizedCandidates
            .Where(candidate => candidate.DisplayName.Equals(normalizedTarget, StringComparison.Ordinal))
            .Select(candidate => candidate.Candidate)
            .ToImmutableArray();
        if (exact.Length != 0)
        {
            return SelectionCandidateMatch<TSelection>.Create(
                snapshot.Revision,
                SelectionCandidateMatchKind.Exact,
                exact);
        }

        var partial = normalizedCandidates
            .Where(candidate =>
                candidate.DisplayName.Contains(normalizedTarget, StringComparison.Ordinal) ||
                normalizedTarget.Contains(candidate.DisplayName, StringComparison.Ordinal))
            .Select(candidate => candidate.Candidate)
            .ToImmutableArray();
        if (partial.Length != 0)
        {
            return SelectionCandidateMatch<TSelection>.Create(
                snapshot.Revision,
                SelectionCandidateMatchKind.Partial,
                partial);
        }

        var identifier = normalizedCandidates
            .Where(candidate => candidate.Identifier.Contains(normalizedTarget, StringComparison.Ordinal))
            .Select(candidate => candidate.Candidate)
            .ToImmutableArray();
        if (identifier.Length != 0)
        {
            return SelectionCandidateMatch<TSelection>.Create(
                snapshot.Revision,
                SelectionCandidateMatchKind.Identifier,
                identifier);
        }

        return SelectionCandidateMatch<TSelection>.None(snapshot.Revision);
    }

    /// <summary>
    /// Grenzt einen Kandidatenbestand mit mindestens zwei Einträgen durch einen
    /// weiteren Namens- oder Kennungsvergleich ein.
    /// </summary>
    public SelectionCandidateMatch<TSelection> Refine(
        SelectionCandidateSnapshot<TSelection> activeSnapshot,
        string? target)
    {
        ArgumentNullException.ThrowIfNull(activeSnapshot);
        if (activeSnapshot.Count < 2)
        {
            throw new ArgumentException(
                "Candidate refinement requires an active snapshot with at least two candidates.",
                nameof(activeSnapshot));
        }

        return Match(activeSnapshot, target);
    }

    private string ValidateProjection(string? raw, string projectionName, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ArgumentException(
                $"A selection item must project a non-blank {projectionName}.",
                parameterName);
        }

        var normalized = _compactNormalizer(raw);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException(
                $"A selection item must project a compact-matchable {projectionName}.",
                parameterName);
        }

        return raw;
    }

    private string NormalizeOptionalTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return string.Empty;
        }

        return _compactNormalizer(target) ?? string.Empty;
    }

    private string NormalizeCandidateProjection(string value, string projectionName)
    {
        var normalized = _compactNormalizer(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException(
                $"The active candidate has a non-matchable {projectionName}.",
                projectionName);
        }

        return normalized;
    }

    private static void RejectDefaultImmutableArray(IEnumerable<TSelection> items)
    {
        if (items is ImmutableArray<TSelection> immutableItems && immutableItems.IsDefault)
        {
            throw new ArgumentException("A default immutable item array is not a catalog.", nameof(items));
        }
    }
}
