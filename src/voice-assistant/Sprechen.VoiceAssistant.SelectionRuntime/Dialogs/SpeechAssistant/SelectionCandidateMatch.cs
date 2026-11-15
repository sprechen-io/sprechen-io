using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die Kandidaten einer deterministischen Trefferstufe samt Revision, damit die Auswahlregel
/// Mehrdeutigkeit und eindeutige Treffer unterscheiden kann.
/// </summary>
public sealed class SelectionCandidateMatch<TSelection>
    where TSelection : class
{
    private SelectionCandidateMatch(
        SelectionRevision revision,
        SelectionCandidateMatchKind kind,
        ImmutableArray<SelectionCandidate<TSelection>> candidates)
    {
        Revision = revision;
        Kind = kind;
        Candidates = candidates;
    }

    /// <summary>
    /// Stellt die Revision bereit, auf die sich das Trefferergebnis bezieht.
    /// </summary>
    public SelectionRevision Revision { get; }

    /// <summary>
    /// Stellt die vom Aufrufer angegebene Trefferstufe bereit.
    /// </summary>
    public SelectionCandidateMatchKind Kind { get; }

    /// <summary>
    /// Stellt die Kandidaten in ihrer übernommenen Reihenfolge bereit.
    /// </summary>
    public ImmutableArray<SelectionCandidate<TSelection>> Candidates { get; }

    /// <summary>
    /// Prüft Kandidatenzuordnung und Ergebnisform, bevor das Trefferergebnis bereitgestellt wird.
    /// </summary>
    public static SelectionCandidateMatch<TSelection> Create(
        SelectionRevision revision,
        SelectionCandidateMatchKind kind,
        IEnumerable<SelectionCandidate<TSelection>> candidates)
    {
        ArgumentNullException.ThrowIfNull(revision);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown candidate match kind.");
        }

        var materialized = SelectionCandidateSnapshot<TSelection>
            .Create(revision, candidates)
            .Candidates;

        if (kind == SelectionCandidateMatchKind.None && materialized.Length != 0)
        {
            throw new ArgumentException("A None match cannot carry candidates.", nameof(candidates));
        }

        if (kind != SelectionCandidateMatchKind.None && materialized.Length == 0)
        {
            throw new ArgumentException("A successful match must carry at least one candidate.", nameof(candidates));
        }

        return new SelectionCandidateMatch<TSelection>(revision, kind, materialized);
    }

    /// <summary>
    /// Erzeugt ein leeres Trefferergebnis für die angegebene Revision.
    /// </summary>
    public static SelectionCandidateMatch<TSelection> None(SelectionRevision revision)
    {
        return Create(revision, SelectionCandidateMatchKind.None, ImmutableArray<SelectionCandidate<TSelection>>.Empty);
    }
}
