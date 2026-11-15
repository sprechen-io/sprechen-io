using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormsCatalogInitialTranscriptResolverTests
{
    public static IEnumerable<object[]> InitialPhaseIntentCases()
    {
        yield return [FormsCatalogIntent.Unknown, false];
        yield return [FormsCatalogIntent.List, true];
        yield return [FormsCatalogIntent.Search, true];
        yield return [FormsCatalogIntent.SelectByName, false];
        yield return [FormsCatalogIntent.SelectByNumber, true];
        yield return [FormsCatalogIntent.Help, true];
        yield return [FormsCatalogIntent.Describe, true];
        yield return [FormsCatalogIntent.Repeat, false];
        yield return [FormsCatalogIntent.Cancel, true];
        yield return [FormsCatalogIntent.ChangeLanguage, true];
        yield return [FormsCatalogIntent.ConfirmYes, false];
        yield return [FormsCatalogIntent.ConfirmNo, false];
        yield return [FormsCatalogIntent.OpenCandidate, false];
    }

    public static IEnumerable<object[]> PhaseInvalidLocalizedTranscripts()
    {
        yield return ["de", "okay"];
        yield return ["de", "nein"];
        yield return ["de", "wiederholen"];
        yield return ["en", "okay"];
        yield return ["en", "no"];
        yield return ["en", "repeat"];
        yield return ["fr", "oui"];
        yield return ["fr", "non"];
        yield return ["fr", "repeter"];
        yield return ["es", "si"];
        yield return ["es", "no"];
        yield return ["es", "repetir"];
        yield return ["it", "okay"];
        yield return ["it", "no"];
        yield return ["it", "ripeti"];
    }

    public static IEnumerable<object[]> EntryExecutableLocalizedTranscripts()
    {
        yield return ["de", "alle anwendungen"];
        yield return ["de", "suche nach katalog"];
        yield return ["de", "hilfe"];
        yield return ["de", "beschreibe katalog"];
        yield return ["de", "abbrechen"];
        yield return ["de", "sprache wechseln"];
        yield return ["de", "eins"];
        yield return ["en", "all applications"];
        yield return ["en", "search for catalog"];
        yield return ["en", "help"];
        yield return ["en", "describe catalog"];
        yield return ["en", "cancel"];
        yield return ["en", "change language"];
        yield return ["en", "one"];
        yield return ["fr", "toutes les applications"];
        yield return ["fr", "rechercher catalogue"];
        yield return ["fr", "aide"];
        yield return ["fr", "decris catalogue"];
        yield return ["fr", "annuler"];
        yield return ["fr", "changer de langue"];
        yield return ["fr", "un"];
        yield return ["es", "todas las aplicaciones"];
        yield return ["es", "buscar catalogo"];
        yield return ["es", "ayuda"];
        yield return ["es", "describe catalogo"];
        yield return ["es", "cancelar"];
        yield return ["es", "cambiar idioma"];
        yield return ["es", "uno"];
        yield return ["it", "tutte le applicazioni"];
        yield return ["it", "cerca catalogo"];
        yield return ["it", "aiuto"];
        yield return ["it", "descrivi catalogo"];
        yield return ["it", "annulla"];
        yield return ["it", "cambia lingua"];
        yield return ["it", "uno"];
    }

    [Fact]
    public void Resolve_WithRecognizedCatalogIntent_ReturnsTrimmedTranscript()
    {
        var catalog = FormsCatalogIntentTestCatalog.Create();
        var resolver = CreateResolver();

        var result = resolver.Resolve(
            " eins ",
            catalog,
            [new ApplicationInfo("sample.catalog", "Katalog", "Katalog", [], DefaultFormId: "sample-form")],
            "de",
            PolitenessLevel.Formal);

        Assert.Equal("eins", result);
    }

    [Theory]
    [InlineData("de", "Katalog")]
    [InlineData("en", "Catalog")]
    [InlineData("fr", "Catalogue")]
    [InlineData("es", "Catálogo")]
    [InlineData("it", "Catalogo")]
    public void Resolve_WithLocalizedVisibleApplicationName_ReturnsTrimmedTranscript(
        string isoCode,
        string applicationName)
    {
        var catalog = FormsCatalogIntentTestCatalog.Create();
        var resolver = CreateResolver();

        var result = resolver.Resolve(
            $" {applicationName} ",
            catalog,
            [new ApplicationInfo("sample.catalog", applicationName, applicationName, [], DefaultFormId: "sample-form")],
            isoCode,
            PolitenessLevel.Formal);

        Assert.Equal(applicationName, result);
    }

    [Theory]
    [MemberData(nameof(InitialPhaseIntentCases))]
    public void Resolve_WithEveryDefinedIntent_FollowsExplicitInitialPhasePolicy(
        FormsCatalogIntent intent,
        bool expectedActionable)
    {
        var catalog = FormsCatalogIntentTestCatalog.Create();
        var resolver = CreateResolver(CreateIntentResult(intent));

        var result = resolver.Resolve(
            " phase input ",
            catalog,
            [new ApplicationInfo("sample.catalog", "Katalog", "Katalog", [], DefaultFormId: "sample-form")],
            "de",
            PolitenessLevel.Formal);

        if (expectedActionable)
        {
            Assert.Equal("phase input", result);
        }
        else
        {
            Assert.Null(result);
        }
    }

    [Fact]
    public void InitialPhaseIntentCases_CoverEveryDefinedIntentExactlyOnce()
    {
        var expected = Enum.GetValues<FormsCatalogIntent>().OrderBy(static intent => intent).ToArray();

        var actual = InitialPhaseIntentCases()
            .Select(static values => (FormsCatalogIntent)values[0])
            .OrderBy(static intent => intent)
            .ToArray();

        Assert.Equal(expected, actual);
        Assert.Equal(actual.Length, actual.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(PhaseInvalidLocalizedTranscripts))]
    public void Resolve_WithPhaseInvalidLocalizedIntent_ReturnsNull(string isoCode, string transcript)
    {
        var catalog = FormsCatalogIntentTestCatalog.Create();
        var resolver = CreateResolver();

        var result = resolver.Resolve(
            transcript,
            catalog,
            [new ApplicationInfo("sample.catalog", "Katalog", "Katalog", [], DefaultFormId: "sample-form")],
            isoCode,
            PolitenessLevel.Formal);

        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(EntryExecutableLocalizedTranscripts))]
    public void Resolve_WithEntryExecutableLocalizedIntent_ReturnsTranscript(string isoCode, string transcript)
    {
        var catalog = FormsCatalogIntentTestCatalog.Create();
        var resolver = CreateResolver();

        var result = resolver.Resolve(
            transcript,
            catalog,
            [new ApplicationInfo("sample.catalog", "Katalog", "Katalog", [], DefaultFormId: "sample-form")],
            isoCode,
            PolitenessLevel.Formal);

        Assert.Equal(transcript, result);
    }

    [Fact]
    public void Resolve_WithAmbiguousApplicationCandidate_ReturnsTranscriptForSelectionRuntime()
    {
        var catalog = FormsCatalogIntentTestCatalog.Create();
        var resolver = CreateResolver();

        var result = resolver.Resolve(
            "Alpha",
            catalog,
            [
                new ApplicationInfo("alpha.catalog", "Alpha Catalog", "Alpha Catalog", [], DefaultFormId: "catalog-form"),
                new ApplicationInfo("alpha.forms", "Alpha Forms", "Alpha Forms", [], DefaultFormId: "forms-form")
            ],
            "de",
            PolitenessLevel.Formal);

        Assert.Equal("Alpha", result);
    }

    [Fact]
    public void Resolve_WithSelectByNameIntentAndVisibleCandidate_ReturnsTranscript()
    {
        var catalog = FormsCatalogIntentTestCatalog.Create();
        var resolver = CreateResolver(new FormsCatalogIntentResult(FormsCatalogIntent.SelectByName, TargetName: "Katalog"));

        var result = resolver.Resolve(
            " Katalog ",
            catalog,
            [new ApplicationInfo("sample.catalog", "Katalog", "Katalog", [], DefaultFormId: "sample-form")],
            "de",
            PolitenessLevel.Formal);

        Assert.Equal("Katalog", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("...")]
    [InlineData("bitte Anwendungskatalog oeffnen")]
    public void Resolve_WithBlankOrNonActionableTranscript_ReturnsNull(string? transcript)
    {
        var catalog = FormsCatalogIntentTestCatalog.Create();
        var resolver = CreateResolver();

        var result = resolver.Resolve(
            transcript,
            catalog,
            [
                new ApplicationInfo("alpha", "Alpha", "Alpha", [], DefaultFormId: "alpha-form"),
                new ApplicationInfo("beta", "Beta", "Beta", [], DefaultFormId: "beta-form")
            ],
            "de",
            PolitenessLevel.Formal);

        Assert.Null(result);
    }

    private static FormsCatalogInitialTranscriptResolver CreateResolver()
        => new(new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog), new ApplicationMatcher());

    private static FormsCatalogIntentResult CreateIntentResult(FormsCatalogIntent intent)
        => intent switch
        {
            FormsCatalogIntent.Unknown => new FormsCatalogIntentResult(intent, TargetName: "missing"),
            FormsCatalogIntent.Search => new FormsCatalogIntentResult(intent, Query: "katalog"),
            FormsCatalogIntent.SelectByName => new FormsCatalogIntentResult(intent, TargetName: "missing"),
            FormsCatalogIntent.OpenCandidate => new FormsCatalogIntentResult(intent, TargetName: "missing"),
            FormsCatalogIntent.SelectByNumber => new FormsCatalogIntentResult(intent, Number: 1),
            FormsCatalogIntent.Describe => new FormsCatalogIntentResult(intent, TargetName: "Katalog"),
            FormsCatalogIntent.ChangeLanguage => new FormsCatalogIntentResult(intent, LanguageArgument: "de"),
            _ => new FormsCatalogIntentResult(intent)
        };

    private static FormsCatalogInitialTranscriptResolver CreateResolver(FormsCatalogIntentResult intentResult)
    {
        var parser = new Mock<IFormsCatalogIntentParser>(MockBehavior.Strict);
        parser
            .Setup(candidate => candidate.Parse(
                It.IsAny<FormsCatalog>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<PolitenessLevel>()))
            .Returns(intentResult);

        return new FormsCatalogInitialTranscriptResolver(parser.Object, new ApplicationMatcher());
    }
}
