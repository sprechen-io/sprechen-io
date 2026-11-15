
namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormSelectionMatcherTests
{
    private readonly IFormSelectionMatcher _matcher = new FormSelectionMatcher();

    [Fact]
    public void Match_ExactUnique_ReturnsExactCandidate()
    {
        var forms = new[]
        {
            new FormInfo("a", "Alpha", []),
            new FormInfo("b", "Beta", [])
        };

        var result = Match(forms, "Alpha");

        Assert.Equal(SelectionCandidateMatchKind.Exact, result.Kind);
        var candidate = Assert.Single(result.Candidates);
        Assert.Equal("a", candidate.Identifier);
        Assert.Equal(1, candidate.StableNumber);
    }

    [Fact]
    public void Match_ExactAmbiguous_ReturnsEveryExactCandidate()
    {
        var forms = new[]
        {
            new FormInfo("a", "Alpha", []),
            new FormInfo("b", "Alpha", [])
        };

        var result = Match(forms, "Alpha");

        Assert.Equal(SelectionCandidateMatchKind.Exact, result.Kind);
        Assert.Equal(["a", "b"], result.Candidates.Select(candidate => candidate.Identifier));
    }

    [Fact]
    public void Match_Substring_ReturnsPartialCandidate()
    {
        var forms = new[]
        {
            new FormInfo("addr", "Address", []),
            new FormInfo("sum", "Summary", [])
        };

        var result = Match(forms, "add");

        Assert.Equal(SelectionCandidateMatchKind.Partial, result.Kind);
        Assert.Equal("addr", Assert.Single(result.Candidates).Identifier);
    }

    [Fact]
    public void Match_IdMatch_ReturnsIdentifierCandidate()
    {
        var forms = new[]
        {
            new FormInfo("form-1", "First", [])
        };

        var result = Match(forms, "form-1");

        Assert.Equal(SelectionCandidateMatchKind.Identifier, result.Kind);
        Assert.Equal("form-1", Assert.Single(result.Candidates).Identifier);
    }

    [Fact]
    public void Match_NoMatch_ReturnsEmptyNoneTier()
    {
        var forms = new[]
        {
            new FormInfo("a", "Alpha", [])
        };

        var result = Match(forms, "Gamma");

        Assert.Equal(SelectionCandidateMatchKind.None, result.Kind);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void Match_PunctuationOnlyName_ReturnsNoMatch()
    {
        var forms = new[]
        {
            new FormInfo("a", "Alpha", [])
        };

        var result = Match(forms, "...");

        Assert.Equal(SelectionCandidateMatchKind.None, result.Kind);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void CreateSnapshot_PunctuationOnlyCandidateName_FailsFast()
    {
        var forms = new[]
        {
            new FormInfo("candidate", "...", [])
        };

        Assert.Throws<ArgumentException>(() => _matcher.CandidateMatcher.CreateSnapshot(
            forms,
            SelectionRevision.Initial));
    }

    private SelectionCandidateMatch<FormInfo> Match(
        IReadOnlyList<FormInfo> forms,
        string? target)
    {
        var snapshot = _matcher.CandidateMatcher.CreateSnapshot(forms, SelectionRevision.Initial);
        return _matcher.CandidateMatcher.Match(snapshot, target);
    }
}
