using System.Collections.Immutable;
using Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionCandidateMatcherTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SelectionRevision_NonPositiveValue_Throws(long value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SelectionRevision(value));
    }

    [Fact]
    public void SelectionRevision_InitialAndNext_AreImmutableSuccessors()
    {
        var initial = SelectionRevision.Initial;

        var next = initial.Next();

        Assert.Equal(1, initial.Value);
        Assert.Equal(2, next.Value);
        Assert.NotSame(initial, next);
    }

    [Fact]
    public void SelectionRevision_NextAtMaximum_ThrowsOverflow()
    {
        var maximum = new SelectionRevision(long.MaxValue);

        Assert.Throws<OverflowException>(() => maximum.Next());
    }

    [Fact]
    public void SelectionCandidate_Create_PreservesValidatedIdentity()
    {
        var item = Item("alpha", "Alpha");
        var revision = SelectionRevision.Initial;

        var candidate = SelectionCandidate<Tuple<string, string>>.Create(
            item,
            "alpha",
            "Alpha",
            3,
            revision);

        Assert.Same(item, candidate.Item);
        Assert.Equal("alpha", candidate.Identifier);
        Assert.Equal("Alpha", candidate.DisplayName);
        Assert.Equal(3, candidate.StableNumber);
        Assert.Same(revision, candidate.Revision);
    }

    [Fact]
    public void SelectionCandidate_Create_NullItemOrRevision_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SelectionCandidate<Tuple<string, string>>.Create(
            null!,
            "alpha",
            "Alpha",
            1,
            SelectionRevision.Initial));
        Assert.Throws<ArgumentNullException>(() => SelectionCandidate<Tuple<string, string>>.Create(
            Item("alpha", "Alpha"),
            "alpha",
            "Alpha",
            1,
            null!));
    }

    [Theory]
    [InlineData(null, "Alpha")]
    [InlineData("", "Alpha")]
    [InlineData("  ", "Alpha")]
    [InlineData("alpha", null)]
    [InlineData("alpha", "")]
    [InlineData("alpha", "  ")]
    public void SelectionCandidate_Create_BlankIdentityOrDisplayName_Throws(
        string? identifier,
        string? displayName)
    {
        Assert.ThrowsAny<ArgumentException>(() => SelectionCandidate<Tuple<string, string>>.Create(
            Item("alpha", "Alpha"),
            identifier!,
            displayName!,
            1,
            SelectionRevision.Initial));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SelectionCandidate_Create_NonPositiveStableNumber_Throws(int stableNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SelectionCandidate<Tuple<string, string>>.Create(
            Item("alpha", "Alpha"),
            "alpha",
            "Alpha",
            stableNumber,
            SelectionRevision.Initial));
    }

    [Fact]
    public void Snapshot_Create_AllowsEmptyInitialCatalog()
    {
        var snapshot = SelectionCandidateSnapshot<Tuple<string, string>>.Create(
            SelectionRevision.Initial,
            []);

        Assert.Empty(snapshot);
        Assert.Equal(SelectionRevision.Initial, snapshot.Revision);
        Assert.False(snapshot.Candidates.IsDefault);
    }

    [Fact]
    public void Snapshot_Create_NullDefaultOrNullElement_Throws()
    {
        ImmutableArray<SelectionCandidate<Tuple<string, string>>> defaultCandidates = default;

        Assert.Throws<ArgumentNullException>(() => SelectionCandidateSnapshot<Tuple<string, string>>.Create(
            SelectionRevision.Initial,
            null!));
        Assert.Throws<ArgumentException>(() => SelectionCandidateSnapshot<Tuple<string, string>>.Create(
            SelectionRevision.Initial,
            defaultCandidates));
        Assert.Throws<ArgumentException>(() => SelectionCandidateSnapshot<Tuple<string, string>>.Create(
            SelectionRevision.Initial,
            new SelectionCandidate<Tuple<string, string>>[] { null! }));
    }

    [Fact]
    public void Snapshot_Create_MismatchedRevision_Throws()
    {
        var candidate = Candidate("alpha", "Alpha", 1, new SelectionRevision(2));

        Assert.Throws<ArgumentException>(() => SelectionCandidateSnapshot<Tuple<string, string>>.Create(
            SelectionRevision.Initial,
            [candidate]));
    }

    [Fact]
    public void Snapshot_Create_DuplicateOrdinalIdentifierOrStableNumber_Throws()
    {
        var revision = SelectionRevision.Initial;
        var first = Candidate("alpha", "Alpha", 1, revision);
        var duplicateIdentifier = Candidate("alpha", "Other", 2, revision);
        var duplicateNumber = Candidate("beta", "Beta", 1, revision);

        Assert.Throws<ArgumentException>(() => SelectionCandidateSnapshot<Tuple<string, string>>.Create(
            revision,
            [first, duplicateIdentifier]));
        Assert.Throws<ArgumentException>(() => SelectionCandidateSnapshot<Tuple<string, string>>.Create(
            revision,
            [first, duplicateNumber]));
    }

    [Fact]
    public void Snapshot_Create_OrdinalIdentifierValidation_RemainsCaseSensitive()
    {
        var revision = SelectionRevision.Initial;

        var snapshot = SelectionCandidateSnapshot<Tuple<string, string>>.Create(
            revision,
            [
                Candidate("alpha", "Alpha", 1, revision),
                Candidate("ALPHA", "Other", 2, revision)
            ]);

        Assert.Equal(2, snapshot.Count);
    }

    [Fact]
    public void Snapshot_Create_CopiesMutableInputCollection()
    {
        var revision = SelectionRevision.Initial;
        var first = Candidate("alpha", "Alpha", 1, revision);
        var source = new List<SelectionCandidate<Tuple<string, string>>> { first };

        var snapshot = SelectionCandidateSnapshot<Tuple<string, string>>.Create(revision, source);
        source.Clear();

        Assert.Single(snapshot);
        Assert.Same(first, snapshot[0]);
    }

    [Fact]
    public void Snapshot_Retain_PreservesRevisionSourceOrderNumbersAndInstances()
    {
        var revision = SelectionRevision.Initial;
        var snapshot = SelectionCandidateSnapshot<Tuple<string, string>>.Create(
            revision,
            [
                Candidate("one", "One", 1, revision),
                Candidate("two", "Two", 2, revision),
                Candidate("three", "Three", 3, revision)
            ]);

        var retained = snapshot.Retain([snapshot[2], snapshot[0]]);

        Assert.Same(revision, retained.Revision);
        Assert.Equal([1, 3], retained.Candidates.Select(candidate => candidate.StableNumber));
        Assert.Same(snapshot[0], retained[0]);
        Assert.Same(snapshot[2], retained[1]);
    }

    [Fact]
    public void Snapshot_Retain_RejectsDuplicateForeignOrStaleCandidate()
    {
        var revision = SelectionRevision.Initial;
        var candidate = Candidate("one", "One", 1, revision);
        var snapshot = SelectionCandidateSnapshot<Tuple<string, string>>.Create(revision, [candidate]);
        var foreign = Candidate("one", "One", 1, revision);
        var stale = Candidate("one", "One", 1, revision.Next());

        Assert.Throws<ArgumentException>(() => snapshot.Retain([candidate, candidate]));
        Assert.Throws<ArgumentException>(() => snapshot.Retain([foreign]));
        Assert.Throws<ArgumentException>(() => snapshot.Retain([stale]));
    }

    [Fact]
    public void Snapshot_Revise_ClonesRetainedCandidatesOntoHigherRevision()
    {
        var revision = SelectionRevision.Initial;
        var snapshot = SelectionCandidateSnapshot<Tuple<string, string>>.Create(
            revision,
            [
                Candidate("one", "One", 1, revision),
                Candidate("two", "Two", 2, revision),
                Candidate("three", "Three", 3, revision)
            ]);
        var higherRevision = new SelectionRevision(5);

        var revised = snapshot.Revise(higherRevision, [snapshot[2], snapshot[0]]);

        Assert.Same(higherRevision, revised.Revision);
        Assert.Equal([1, 3], revised.Candidates.Select(candidate => candidate.StableNumber));
        Assert.All(revised, candidate => Assert.Same(higherRevision, candidate.Revision));
        Assert.Same(snapshot[0].Item, revised[0].Item);
        Assert.Same(snapshot[2].Item, revised[1].Item);
        Assert.Equal(snapshot[0].Identifier, revised[0].Identifier);
        Assert.Equal(snapshot[2].DisplayName, revised[1].DisplayName);
        Assert.NotSame(snapshot[0], revised[0]);
        Assert.NotSame(snapshot[2], revised[1]);
        Assert.Same(revision, snapshot.Revision);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public void Snapshot_Revise_EqualOrLowerRevision_Throws(long value)
    {
        var revision = new SelectionRevision(2);
        var candidate = Candidate("one", "One", 1, revision);
        var snapshot = SelectionCandidateSnapshot<Tuple<string, string>>.Create(revision, [candidate]);
        var requested = value == 0 ? SelectionRevision.Initial : new SelectionRevision(2);

        Assert.Throws<ArgumentException>(() => snapshot.Revise(requested, [candidate]));
    }

    [Fact]
    public void Match_Create_EnforcesKindCardinalityAndRevision()
    {
        var revision = SelectionRevision.Initial;
        var candidate = Candidate("one", "One", 1, revision);
        var otherRevision = revision.Next();

        Assert.Throws<ArgumentException>(() => SelectionCandidateMatch<Tuple<string, string>>.Create(
            revision,
            SelectionCandidateMatchKind.None,
            [candidate]));
        Assert.Throws<ArgumentException>(() => SelectionCandidateMatch<Tuple<string, string>>.Create(
            revision,
            SelectionCandidateMatchKind.Exact,
            []));
        Assert.Throws<ArgumentException>(() => SelectionCandidateMatch<Tuple<string, string>>.Create(
            otherRevision,
            SelectionCandidateMatchKind.Exact,
            [candidate]));
        Assert.Throws<ArgumentOutOfRangeException>(() => SelectionCandidateMatch<Tuple<string, string>>.Create(
            revision,
            (SelectionCandidateMatchKind)999,
            []));
    }

    [Fact]
    public void Match_Create_CopiesMutableInputCollection()
    {
        var revision = SelectionRevision.Initial;
        var candidate = Candidate("one", "One", 1, revision);
        var source = new List<SelectionCandidate<Tuple<string, string>>> { candidate };

        var match = SelectionCandidateMatch<Tuple<string, string>>.Create(
            revision,
            SelectionCandidateMatchKind.Exact,
            source);
        source.Clear();

        Assert.Single(match.Candidates);
        Assert.Same(candidate, match.Candidates[0]);
        Assert.Equal(revision, match.Revision);
    }

    [Fact]
    public void Matcher_Constructor_NullDependency_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SelectionCandidateMatcher<Tuple<string, string>>(
            null!,
            static item => item.Item2,
            TextNormalizer.NormalizeForCompactMatching));
        Assert.Throws<ArgumentNullException>(() => new SelectionCandidateMatcher<Tuple<string, string>>(
            static item => item.Item1,
            null!,
            TextNormalizer.NormalizeForCompactMatching));
        Assert.Throws<ArgumentNullException>(() => new SelectionCandidateMatcher<Tuple<string, string>>(
            static item => item.Item1,
            static item => item.Item2,
            null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void Matcher_CreateSnapshot_AssignsStableSourceNumbers_ForZeroOneAndMany(int count)
    {
        var items = Enumerable.Range(1, count)
            .Select(number => Item($"id-{number}", $"Item {number}"))
            .ToArray();

        var snapshot = Matcher().CreateSnapshot(items, SelectionRevision.Initial);

        Assert.Equal(count, snapshot.Count);
        Assert.Equal(
            Enumerable.Range(1, count),
            snapshot.Candidates.Select(candidate => candidate.StableNumber));
        Assert.Equal(items, snapshot.Candidates.Select(candidate => candidate.Item));
    }

    [Fact]
    public void Matcher_CreateSnapshot_NullDefaultOrNullItem_Throws()
    {
        ImmutableArray<Tuple<string, string>> defaultItems = default;

        Assert.Throws<ArgumentNullException>(() => Matcher().CreateSnapshot(null!, SelectionRevision.Initial));
        Assert.Throws<ArgumentException>(() => Matcher().CreateSnapshot(defaultItems, SelectionRevision.Initial));
        Assert.Throws<ArgumentException>(() => Matcher().CreateSnapshot(
            new Tuple<string, string>[] { null! },
            SelectionRevision.Initial));
    }

    [Theory]
    [InlineData(null, "Alpha")]
    [InlineData("", "Alpha")]
    [InlineData("  ", "Alpha")]
    [InlineData("...", "Alpha")]
    [InlineData("alpha", null)]
    [InlineData("alpha", "")]
    [InlineData("alpha", "  ")]
    [InlineData("alpha", "...")]
    public void Matcher_CreateSnapshot_BlankOrCompactEmptyProjection_Throws(
        string? identifier,
        string? displayName)
    {
        var item = Tuple.Create(identifier!, displayName!);

        Assert.Throws<ArgumentException>(() => Matcher().CreateSnapshot(
            [item],
            SelectionRevision.Initial));
    }

    [Fact]
    public void Matcher_CreateSnapshot_DuplicateIdentifierIsOrdinalAndFailsFast()
    {
        var matcher = Matcher();

        Assert.Throws<ArgumentException>(() => matcher.CreateSnapshot(
            [Item("alpha", "One"), Item("alpha", "Two")],
            SelectionRevision.Initial));

        var caseDistinct = matcher.CreateSnapshot(
            [Item("alpha", "One"), Item("ALPHA", "Two")],
            SelectionRevision.Initial);
        Assert.Equal(2, caseDistinct.Count);
    }

    [Fact]
    public void Matcher_CreateSnapshot_CopiesMutableCatalogCollection()
    {
        var first = Item("one", "One");
        var source = new List<Tuple<string, string>> { first };

        var snapshot = Matcher().CreateSnapshot(source, SelectionRevision.Initial);
        source.Clear();

        Assert.Single(snapshot);
        Assert.Same(first, snapshot[0].Item);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("?!...")]
    public void Matcher_Match_BlankOrCompactEmptyTarget_ReturnsNone(string? target)
    {
        var snapshot = Matcher().CreateSnapshot(
            [Item("alpha", "Alpha")],
            SelectionRevision.Initial);

        var match = Matcher().Match(snapshot, target);

        Assert.Equal(SelectionCandidateMatchKind.None, match.Kind);
        Assert.Empty(match.Candidates);
        Assert.Equal(snapshot.Revision, match.Revision);
    }

    [Fact]
    public void Matcher_Match_ExactDisplayName_ReturnsUniqueExactCandidate()
    {
        var snapshot = Matcher().CreateSnapshot(
            [Item("alpha", "Alpha"), Item("beta", "Beta")],
            SelectionRevision.Initial);

        var match = Matcher().Match(snapshot, "  ÁLPHA! ");

        Assert.Equal(SelectionCandidateMatchKind.Exact, match.Kind);
        Assert.Single(match.Candidates);
        Assert.Same(snapshot[0], match.Candidates[0]);
    }

    [Fact]
    public void Matcher_Match_DuplicateExactDisplayNames_PreservesEveryCandidate()
    {
        var snapshot = Matcher().CreateSnapshot(
            [
                Item("alpha-one", "Alpha"),
                Item("beta", "Beta"),
                Item("alpha-two", "Alpha")
            ],
            SelectionRevision.Initial);

        var match = Matcher().Match(snapshot, "alpha");

        Assert.Equal(SelectionCandidateMatchKind.Exact, match.Kind);
        Assert.Equal(["alpha-one", "alpha-two"], match.Candidates.Select(candidate => candidate.Identifier));
        Assert.Equal([1, 3], match.Candidates.Select(candidate => candidate.StableNumber));
    }

    [Fact]
    public void Matcher_Match_ExactTier_PrecedesPartialAndIdentifierTiers()
    {
        var snapshot = Matcher().CreateSnapshot(
            [
                Item("partial", "Alpha Extended"),
                Item("alpha-identifier", "Other"),
                Item("exact", "Alpha")
            ],
            SelectionRevision.Initial);

        var match = Matcher().Match(snapshot, "alpha");

        Assert.Equal(SelectionCandidateMatchKind.Exact, match.Kind);
        Assert.Single(match.Candidates);
        Assert.Equal("exact", match.Candidates[0].Identifier);
    }

    [Theory]
    [InlineData("alpha", "Alpha Extended")]
    [InlineData("alpha extended", "Alpha")]
    public void Matcher_Match_DisplayNameContainment_IsBidirectionalPartial(
        string target,
        string displayName)
    {
        var snapshot = Matcher().CreateSnapshot(
            [Item("candidate", displayName)],
            SelectionRevision.Initial);

        var match = Matcher().Match(snapshot, target);

        Assert.Equal(SelectionCandidateMatchKind.Partial, match.Kind);
        Assert.Single(match.Candidates);
    }

    [Fact]
    public void Matcher_Match_PartialTier_PrecedesIdentifierTier()
    {
        var snapshot = Matcher().CreateSnapshot(
            [
                Item("identifier-alpha", "Other"),
                Item("partial", "Alpha Extended")
            ],
            SelectionRevision.Initial);

        var match = Matcher().Match(snapshot, "alpha");

        Assert.Equal(SelectionCandidateMatchKind.Partial, match.Kind);
        Assert.Single(match.Candidates);
        Assert.Equal("partial", match.Candidates[0].Identifier);
    }

    [Fact]
    public void Matcher_Match_IdentifierContainsTarget_ReturnsEveryIdentifierCandidate()
    {
        var snapshot = Matcher().CreateSnapshot(
            [
                Item("catalog-alpha-one", "One"),
                Item("other", "Other"),
                Item("catalog-alpha-two", "Two")
            ],
            SelectionRevision.Initial);

        var match = Matcher().Match(snapshot, "alpha");

        Assert.Equal(SelectionCandidateMatchKind.Identifier, match.Kind);
        Assert.Equal(["catalog-alpha-one", "catalog-alpha-two"], match.Candidates.Select(candidate => candidate.Identifier));
        Assert.Equal([1, 3], match.Candidates.Select(candidate => candidate.StableNumber));
    }

    [Fact]
    public void Matcher_Match_NoCandidate_ReturnsNoneAtSnapshotRevision()
    {
        var revision = new SelectionRevision(7);
        var snapshot = Matcher().CreateSnapshot([Item("alpha", "Alpha")], revision);

        var match = Matcher().Match(snapshot, "gamma");

        Assert.Equal(SelectionCandidateMatchKind.None, match.Kind);
        Assert.Empty(match.Candidates);
        Assert.Same(revision, match.Revision);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void Matcher_Match_PreservesAllPartialCandidates_ForZeroOneAndMany(int count)
    {
        var snapshot = Matcher().CreateSnapshot(
            Enumerable.Range(1, count)
                .Select(number => Item($"id-{number}", $"Group {number}")),
            SelectionRevision.Initial);

        var match = Matcher().Match(snapshot, "group");

        Assert.Equal(
            count == 0 ? SelectionCandidateMatchKind.None : SelectionCandidateMatchKind.Partial,
            match.Kind);
        Assert.Equal(count, match.Candidates.Length);
        Assert.Equal(
            Enumerable.Range(1, count),
            match.Candidates.Select(candidate => candidate.StableNumber));
    }

    [Fact]
    public void Matcher_Match_ManualNonCompactCandidateProjection_FailsFast()
    {
        var revision = SelectionRevision.Initial;
        var snapshot = SelectionCandidateSnapshot<Tuple<string, string>>.Create(
            revision,
            [SelectionCandidate<Tuple<string, string>>.Create(
                Item("candidate", "..."),
                "candidate",
                "...",
                1,
                revision)]);

        Assert.Throws<ArgumentException>(() => Matcher().Match(snapshot, "candidate"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Matcher_Refine_WithoutEstablishedAmbiguity_Throws(int count)
    {
        var snapshot = Matcher().CreateSnapshot(
            Enumerable.Range(1, count)
                .Select(number => Item($"id-{number}", $"Item {number}")),
            SelectionRevision.Initial);

        Assert.Throws<ArgumentException>(() => Matcher().Refine(snapshot, "item"));
    }

    [Fact]
    public void Matcher_Refine_PreservesRetainedRevisionOrderAndGappedSourceNumbers()
    {
        var revision = new SelectionRevision(4);
        var initial = Matcher().CreateSnapshot(
            [
                Item("one", "Other One"),
                Item("two", "Alpha One"),
                Item("three", "Other Three"),
                Item("four", "Alpha Two"),
                Item("five", "Gamma")
            ],
            revision);
        var active = initial.Retain([initial[4], initial[3], initial[1]]);

        var match = Matcher().Refine(active, "alpha");

        Assert.Equal(SelectionCandidateMatchKind.Partial, match.Kind);
        Assert.Same(revision, match.Revision);
        Assert.Equal([2, 4], match.Candidates.Select(candidate => candidate.StableNumber));
        Assert.Same(initial[1], match.Candidates[0]);
        Assert.Same(initial[3], match.Candidates[1]);
    }

    private static SelectionCandidateMatcher<Tuple<string, string>> Matcher()
    {
        return new SelectionCandidateMatcher<Tuple<string, string>>(
            static item => item.Item1,
            static item => item.Item2,
            TextNormalizer.NormalizeForCompactMatching);
    }

    private static Tuple<string, string> Item(string identifier, string displayName)
    {
        return Tuple.Create(identifier, displayName);
    }

    private static SelectionCandidate<Tuple<string, string>> Candidate(
        string identifier,
        string displayName,
        int stableNumber,
        SelectionRevision revision)
    {
        return SelectionCandidate<Tuple<string, string>>.Create(
            Item(identifier, displayName),
            identifier,
            displayName,
            stableNumber,
            revision);
    }
}
