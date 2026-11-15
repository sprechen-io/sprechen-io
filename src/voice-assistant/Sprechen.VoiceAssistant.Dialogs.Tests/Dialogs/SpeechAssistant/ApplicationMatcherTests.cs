using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class ApplicationMatcherTests
{
    private readonly ApplicationMatcher _matcher = new();

    #region Search Tests

    [Fact]
    public void Search_EmptyQuery_ReturnsAll()
    {
        var apps = CreateTestApplications();

        var result = _matcher.Search(apps, null);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Search_WhitespaceQuery_ReturnsAll()
    {
        var apps = CreateTestApplications();

        var result = _matcher.Search(apps, "   ");

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Search_ByDisplayName_FindsMatch()
    {
        var apps = CreateTestApplications();

        var result = _matcher.Search(apps, "Short");

        Assert.Single(result);
        Assert.Equal("short-form", result[0].Id);
    }

    [Fact]
    public void Search_ByDescription_FindsMatch()
    {
        var apps = CreateTestApplications();

        var result = _matcher.Search(apps, "address");

        Assert.Single(result);
        Assert.Equal("address-form", result[0].Id);
    }

    [Fact]
    public void Search_ByTag_FindsMatch()
    {
        var apps = CreateTestApplications();

        var result = _matcher.Search(apps, "demo");

        Assert.Single(result);
        Assert.Equal("short-form", result[0].Id);
    }

    [Fact]
    public void Search_ById_FindsMatch()
    {
        var apps = CreateTestApplications();

        var result = _matcher.Search(apps, "contact");

        Assert.Single(result);
        Assert.Equal("contact-form", result[0].Id);
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        var apps = CreateTestApplications();

        var result = _matcher.Search(apps, "nonexistent");

        Assert.Empty(result);
    }

    [Fact]
    public void Search_PunctuationOnlyQuery_ReturnsEmpty()
    {
        var apps = CreateTestApplications();

        var result = _matcher.Search(apps, "...");

        Assert.Empty(result);
    }

    [Fact]
    public void Search_CaseInsensitive()
    {
        var apps = CreateTestApplications();

        var result = _matcher.Search(apps, "SHORT");

        Assert.Single(result);
        Assert.Equal("short-form", result[0].Id);
    }

    #endregion

    #region Candidate matching tests

    [Fact]
    public void Match_EmptyName_ReturnsNoMatch()
    {
        var apps = CreateTestApplications();

        var result = Match(apps, null);

        Assert.Equal(SelectionCandidateMatchKind.None, result.Kind);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void Match_ExactMatch_ReturnsCompleteExactTier()
    {
        var apps = CreateTestApplications();

        var result = Match(apps, "Short Form");

        Assert.Equal(SelectionCandidateMatchKind.Exact, result.Kind);
        var candidate = Assert.Single(result.Candidates);
        Assert.Equal("short-form", candidate.Identifier);
        Assert.Equal("Short Form", candidate.DisplayName);
        Assert.Equal(1, candidate.StableNumber);
    }

    [Fact]
    public void Match_SubstringMatch_ReturnsEveryPartialCandidateInSourceOrder()
    {
        var apps = CreateTestApplications();

        var result = Match(apps, "Form");

        Assert.Equal(SelectionCandidateMatchKind.Partial, result.Kind);
        Assert.Equal(
            ["short-form", "address-form", "contact-form"],
            result.Candidates.Select(candidate => candidate.Identifier));
        Assert.Equal([1, 2, 3], result.Candidates.Select(candidate => candidate.StableNumber));
    }

    [Fact]
    public void Match_IdMatch_WhenNoDisplayNameMatch_ReturnsIdentifierTier()
    {
        var apps = new List<ApplicationInfo>
        {
            new("test-id-123", "Some App", "Description", [])
        };

        var result = Match(apps, "test-id-123");

        Assert.Equal(SelectionCandidateMatchKind.Identifier, result.Kind);
        Assert.Equal("test-id-123", Assert.Single(result.Candidates).Identifier);
    }

    [Fact]
    public void Match_NoMatch_ReturnsEmptyNoneTier()
    {
        var apps = CreateTestApplications();

        var result = Match(apps, "nonexistent");

        Assert.Equal(SelectionCandidateMatchKind.None, result.Kind);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void Match_PunctuationOnlyName_ReturnsNoMatch()
    {
        var apps = CreateTestApplications();

        var result = Match(apps, "...");

        Assert.Equal(SelectionCandidateMatchKind.None, result.Kind);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void CreateSnapshot_PunctuationOnlyCandidateName_FailsFast()
    {
        var apps = new[]
        {
            new ApplicationInfo("candidate", "...", null, [])
        };

        Assert.Throws<ArgumentException>(() => _matcher.CandidateMatcher.CreateSnapshot(
            apps,
            SelectionRevision.Initial));
    }

    [Fact]
    public void Match_CaseInsensitive()
    {
        var apps = CreateTestApplications();

        var result = Match(apps, "short form");

        Assert.Equal(SelectionCandidateMatchKind.Exact, result.Kind);
        Assert.Equal("short-form", Assert.Single(result.Candidates).Identifier);
    }

    [Fact]
    public void Match_MultipleExactMatches_ReturnsEveryExactCandidate()
    {
        var apps = new List<ApplicationInfo>
        {
            new("app1", "Test", "Desc 1", ["tag1"]),
            new("app2", "Test", "Desc 2", ["tag2"])
        };

        var result = Match(apps, "Test");

        Assert.Equal(SelectionCandidateMatchKind.Exact, result.Kind);
        Assert.Equal(["app1", "app2"], result.Candidates.Select(candidate => candidate.Identifier));
    }

    #endregion

    #region Localized example-app name resolution (regression)

    // Die Beispieltitel teilen Wörter wie „Beispiel“. Vollständige Namen müssen genau eine
    // Anwendung treffen, das gemeinsame Wort muss beide Kandidaten liefern. Geprüft werden
    // die hier hinterlegten Namen in fünf Sprachen, keine Spracherkennung oder Bestätigung.
    [Theory]
    [InlineData("Einfaches Beispiel", "Komplexes Beispiel", "Beispiel")]  // de
    [InlineData("Simple example", "Complex example", "example")]          // en
    [InlineData("Exemple simple", "Exemple complexe", "Exemple")]         // fr
    [InlineData("Ejemplo sencillo", "Ejemplo complejo", "Ejemplo")]       // es
    [InlineData("Esempio semplice", "Esempio complesso", "Esempio")]      // it
    public void Match_LocalizedExampleNames_ResolveToCorrectApp_NeverTheConfusableSibling(
        string simpleName, string complexName, string sharedToken)
    {
        var apps = new List<ApplicationInfo>
        {
            new("sample.simple", simpleName, null, []),
            new("sample.complex", complexName, null, [])
        };


        var simple = Match(apps, simpleName);
        Assert.Equal(SelectionCandidateMatchKind.Exact, simple.Kind);
        Assert.Equal("sample.simple", Assert.Single(simple.Candidates).Identifier);


        var complex = Match(apps, complexName);
        Assert.Equal(SelectionCandidateMatchKind.Exact, complex.Kind);
        Assert.Equal("sample.complex", Assert.Single(complex.Candidates).Identifier);

        // Das gemeinsame Wort liefert beide Kandidaten; die spätere Bestätigung wird hier nicht geprüft.
        var shared = Match(apps, sharedToken);
        Assert.Equal(SelectionCandidateMatchKind.Partial, shared.Kind);
        Assert.Equal(
            ["sample.simple", "sample.complex"],
            shared.Candidates.Select(candidate => candidate.Identifier));
    }

    #endregion

    #region Helper Methods

    private static List<ApplicationInfo> CreateTestApplications()
    {
        return
        [
            new("short-form", "Short Form", "A simple test form", ["demo", "test"]),
            new("address-form", "Address Form", "Collect address information", ["production"]),
            new("contact-form", "Contact Form", "Contact details form", ["production"])
        ];
    }

    private SelectionCandidateMatch<ApplicationInfo> Match(
        IReadOnlyList<ApplicationInfo> applications,
        string? target)
    {
        var snapshot = _matcher.CandidateMatcher.CreateSnapshot(
            applications,
            SelectionRevision.Initial);
        return _matcher.CandidateMatcher.Match(snapshot, target);
    }

    #endregion
}
