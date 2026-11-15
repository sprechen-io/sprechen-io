using System.Collections;
using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält einen unveränderlichen Kandidatenbestand mit gemeinsamer Revision. Die enthaltenen
/// Fachobjekte werden dabei nicht tief kopiert oder unveränderlich gemacht.
/// </summary>
public sealed class SelectionCandidateSnapshot<TSelection> : IReadOnlyList<SelectionCandidate<TSelection>>
    where TSelection : class
{
    private SelectionCandidateSnapshot(
        SelectionRevision revision,
        ImmutableArray<SelectionCandidate<TSelection>> candidates)
    {
        Revision = revision;
        Candidates = candidates;
    }

    /// <summary>
    /// Stellt die gemeinsame Revision des Kandidatenbestands bereit.
    /// </summary>
    public SelectionRevision Revision { get; }

    /// <summary>
    /// Stellt die Kandidaten in der übernommenen Reihenfolge bereit.
    /// </summary>
    public ImmutableArray<SelectionCandidate<TSelection>> Candidates { get; }

    /// <inheritdoc />
    public int Count => Candidates.Length;

    /// <inheritdoc />
    public SelectionCandidate<TSelection> this[int index] => Candidates[index];

    /// <summary>
    /// Erzeugt einen geprüften Kandidatenbestand. Auch ein leerer Ausgangskatalog ist gültig.
    /// </summary>
    public static SelectionCandidateSnapshot<TSelection> Create(
        SelectionRevision revision,
        IEnumerable<SelectionCandidate<TSelection>> candidates)
    {
        ArgumentNullException.ThrowIfNull(revision);
        var materialized = MaterializeAndValidate(revision, candidates);
        return new SelectionCandidateSnapshot<TSelection>(revision, materialized);
    }

    /// <summary>
    /// Behält eine Teilmenge derselben Revision unter Erhaltung der bisherigen Reihenfolge und
    /// Kandidateninstanzen.
    /// </summary>
    public SelectionCandidateSnapshot<TSelection> Retain(
        IEnumerable<SelectionCandidate<TSelection>> retainedCandidates)
    {
        var retained = SelectRetainedCandidates(retainedCandidates);
        return new SelectionCandidateSnapshot<TSelection>(Revision, retained);
    }

    /// <summary>
    /// Überführt eine beibehaltene Teilmenge in eine höhere Revision. Die neuen
    /// Kandidateninstanzen verweisen weiterhin auf dieselben Fachobjekte.
    /// </summary>
    public SelectionCandidateSnapshot<TSelection> Revise(
        SelectionRevision newRevision,
        IEnumerable<SelectionCandidate<TSelection>> retainedCandidates)
    {
        ArgumentNullException.ThrowIfNull(newRevision);

        if (newRevision.Value <= Revision.Value)
        {
            throw new ArgumentException(
                "A revised candidate snapshot must use a strictly higher revision.",
                nameof(newRevision));
        }

        var retained = SelectRetainedCandidates(retainedCandidates);
        var revised = retained
            .Select(candidate => SelectionCandidate<TSelection>.Create(
                candidate.Item,
                candidate.Identifier,
                candidate.DisplayName,
                candidate.StableNumber,
                newRevision))
            .ToImmutableArray();

        return new SelectionCandidateSnapshot<TSelection>(newRevision, revised);
    }

    /// <inheritdoc />
    public IEnumerator<SelectionCandidate<TSelection>> GetEnumerator()
    {
        return ((IEnumerable<SelectionCandidate<TSelection>>)Candidates).GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private static ImmutableArray<SelectionCandidate<TSelection>> MaterializeAndValidate(
        SelectionRevision revision,
        IEnumerable<SelectionCandidate<TSelection>> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        RejectDefaultImmutableArray(candidates);

        var builder = ImmutableArray.CreateBuilder<SelectionCandidate<TSelection>>();
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        var stableNumbers = new HashSet<int>();

        foreach (var candidate in candidates)
        {
            if (candidate is null)
            {
                throw new ArgumentException("A candidate snapshot cannot contain null elements.", nameof(candidates));
            }

            if (candidate.Revision != revision)
            {
                throw new ArgumentException(
                    "Every candidate must belong to the snapshot revision.",
                    nameof(candidates));
            }

            if (!identifiers.Add(candidate.Identifier))
            {
                throw new ArgumentException(
                    "A candidate snapshot cannot contain duplicate ordinal identifiers.",
                    nameof(candidates));
            }

            if (!stableNumbers.Add(candidate.StableNumber))
            {
                throw new ArgumentException(
                    "A candidate snapshot cannot contain duplicate stable numbers.",
                    nameof(candidates));
            }

            builder.Add(candidate);
        }

        return builder.ToImmutable();
    }

    private ImmutableArray<SelectionCandidate<TSelection>> SelectRetainedCandidates(
        IEnumerable<SelectionCandidate<TSelection>> retainedCandidates)
    {
        ArgumentNullException.ThrowIfNull(retainedCandidates);
        RejectDefaultImmutableArray(retainedCandidates);

        var retainedNumbers = new HashSet<int>();
        foreach (var candidate in retainedCandidates)
        {
            if (candidate is null)
            {
                throw new ArgumentException("A retained candidate set cannot contain null elements.", nameof(retainedCandidates));
            }

            if (candidate.Revision != Revision)
            {
                throw new ArgumentException(
                    "A retained candidate must belong to the active revision.",
                    nameof(retainedCandidates));
            }

            var original = Candidates.FirstOrDefault(
                current => current.StableNumber == candidate.StableNumber);
            if (original is null || !ReferenceEquals(original, candidate))
            {
                throw new ArgumentException(
                    "A retained candidate must come from the active snapshot.",
                    nameof(retainedCandidates));
            }

            if (!retainedNumbers.Add(candidate.StableNumber))
            {
                throw new ArgumentException(
                    "A retained candidate cannot be supplied more than once.",
                    nameof(retainedCandidates));
            }
        }

        return Candidates
            .Where(candidate => retainedNumbers.Contains(candidate.StableNumber))
            .ToImmutableArray();
    }

    private static void RejectDefaultImmutableArray(
        IEnumerable<SelectionCandidate<TSelection>> candidates)
    {
        if (candidates is ImmutableArray<SelectionCandidate<TSelection>> immutableCandidates &&
            immutableCandidates.IsDefault)
        {
            throw new ArgumentException("A default immutable candidate array is not a candidate set.", nameof(candidates));
        }
    }
}
