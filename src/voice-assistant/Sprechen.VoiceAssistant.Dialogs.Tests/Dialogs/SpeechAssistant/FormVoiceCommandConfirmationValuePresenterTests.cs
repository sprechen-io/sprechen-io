using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandConfirmationValuePresenterTests
{
    [Fact]
    public async Task BuildAsync_WhenSpellingReadbackIsAllowed_PresentsCanonicalSpelledValue()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateField(InputMode.Spelling, speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "ab-1"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            });

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("fallback"),
            CancellationToken.None);

        Assert.Equal("A wie Anton. B wie Berta. Bindestrich. 1 wie eins", result.Value);
        Assert.True(result.IncludesValue);
    }

    [Theory]
    [InlineData("de", "Berta", "Ludwig", "Anton", "Ulrich")]
    [InlineData("en", "Bravo", "Lima", "Alpha", "Uniform")]
    [InlineData("fr", "Bravo", "Lima", "Alpha", "Uniform")]
    [InlineData("es", "Bravo", "Lima", "Alpha", "Uniform")]
    [InlineData("it", "Bravo", "Lima", "Alpha", "Uniform")]
    public async Task BuildAsync_WhenSpellingModeAndRendererInjected_ReadsValueBackPhonetically(
        string isoCode,
        string firstWord,
        string secondWord,
        string thirdWord,
        string fourthWord)
    {
        // Buchstabierte Werte mit den modellierten Buchstabierwörtern der
        // Sitzungssprache aufbereiten. Die tatsächliche Aussprache wird hier nicht geprüft.
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(
            speechService.Object,
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateField(InputMode.Spelling, speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "blau"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            },
            isoCode: isoCode);

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("fallback"),
            CancellationToken.None);

        Assert.Contains(firstWord, result.Value);
        Assert.Contains(secondWord, result.Value);
        Assert.Contains(thirdWord, result.Value);
        Assert.Contains(fourthWord, result.Value);
        Assert.DoesNotContain("B L A U", result.Value);
        Assert.True(result.IncludesValue);
    }

    [Fact]
    public async Task BuildAsync_WhenNotSpellingMode_RendererDoesNotSpellOutWholeWordValue()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(
            speechService.Object,
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateField(InputMode.Voice, speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "Markus"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            });

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("fallback"),
            CancellationToken.None);

        Assert.Equal("Markus", result.Value);
        Assert.DoesNotContain("wie", result.Value);
        Assert.True(result.IncludesValue);
    }

    [Theory]
    [InlineData("de", "Anton", "Berta", "Caesar")]
    [InlineData("en", "Alpha", "Bravo", "Charlie")]
    [InlineData("fr", "Alpha", "Bravo", "Charlie")]
    [InlineData("es", "Alpha", "Bravo", "Charlie")]
    [InlineData("it", "Alpha", "Bravo", "Charlie")]
    public async Task BuildAsync_WhenFreeTextValueIsDictatedLetterRun_ReadsBackPhonetically(
        string isoCode,
        string firstWord,
        string secondWord,
        string thirdWord)
    {
        // Eine gespeicherte Folge von Großbuchstaben auch außerhalb des
        // Buchstabiermodus mit den modellierten Buchstabierwörtern aufbereiten.
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(
            speechService.Object,
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateField(InputMode.Voice, speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "ABC"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            },
            isoCode: isoCode);

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("fallback"),
            CancellationToken.None);

        Assert.Contains(firstWord, result.Value);
        Assert.Contains(secondWord, result.Value);
        Assert.Contains(thirdWord, result.Value);
        Assert.DoesNotContain("ABC", result.Value, StringComparison.Ordinal);
        Assert.True(result.IncludesValue);
    }

    [Fact]
    public async Task BuildAsync_WhenFreeTextValueIsOrdinaryWord_IsSpokenUnchanged()
    {
        // Ein gemischt geschriebenes Wort soll von der Erkennung reiner
        // Großbuchstabenfolgen nicht betroffen sein.
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(
            speechService.Object,
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateField(InputMode.Voice, speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "Berlin"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            });

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("fallback"),
            CancellationToken.None);

        Assert.Equal("Berlin", result.Value);
        Assert.DoesNotContain("wie", result.Value);
        Assert.True(result.IncludesValue);
    }

    [Fact]
    public async Task BuildAsync_WhenSpellingModeValueHasUncoveredCharacters_FallsBackWithoutCrashing()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(
            speechService.Object,
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateField(InputMode.Spelling, speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "a1"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            },
            isoCode: "de");

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("fallback"),
            CancellationToken.None);

        Assert.Equal("A wie Anton. 1 wie eins", result.Value);
        Assert.True(result.IncludesValue);
    }

    [Fact]
    public async Task BuildAsync_WhenDigitsReadbackIsAllowed_PresentsValueDigitByDigit()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateField(InputMode.Digits, speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "123456"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            });

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("fallback"),
            CancellationToken.None);

        Assert.Equal("1 2 3 4 5 6", result.Value);
        Assert.True(result.IncludesValue);
    }

    [Fact]
    public async Task BuildAsync_WhenFullReadbackIsNotAllowed_UsesMaskedReadbackOwner()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateField(InputMode.Voice, speakValueInConfirmation: false),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "secret"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.RequiresReadbackOptInAsync("name")).ReturnsAsync(false);
                privacy.Setup(p => p.BuildMaskedReadbackAsync("name", "secret", "de")).ReturnsAsync("maskiert");
            });

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("fallback"),
            CancellationToken.None);

        Assert.Equal("maskiert", result.Value);
        Assert.True(result.IncludesValue);
        speechService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildAsync_WhenSensitiveOptInAccepted_PresentsChunkedCodeValue()
    {
        // Bei freigegebenem Vorlesen, aber gesperrter Buchstabierausgabe den
        // Code in Blöcken aufbereiten. Die Zustimmung wird simuliert.
        const string iban = "DE44500105175407324931";
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<string>(),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateField(InputMode.Voice, speakValueInConfirmation: false),
            values: ImmutableDictionary<string, string>.Empty.Add("name", iban),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.RequiresReadbackOptInAsync("name")).ReturnsAsync(true);
                privacy.Setup(p => p.CanSpeakValueInConfirmationAsync("name", true)).ReturnsAsync(true);
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(false);
            });

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("fallback"),
            CancellationToken.None);

        Assert.Equal("DE44 5001 0517 5407 3249 31", result.Value);
        Assert.True(result.IncludesValue);
    }

    [Fact]
    public async Task BuildAsync_WhenSensitiveOptInDeclined_PresentsMaskedReadback()
    {
        const string iban = "DE44500105175407324931";
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<string>(),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateField(InputMode.Voice, speakValueInConfirmation: false),
            values: ImmutableDictionary<string, string>.Empty.Add("name", iban),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.RequiresReadbackOptInAsync("name")).ReturnsAsync(true);
                privacy.Setup(p => p.BuildMaskedReadbackAsync("name", iban, "de")).ReturnsAsync("maskiert");
            });

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("fallback"),
            CancellationToken.None);

        Assert.Equal("maskiert", result.Value);
        Assert.True(result.IncludesValue);
    }

    [Fact]
    public async Task BuildUnderstoodValueAsync_WhenFieldIsNotClearedForReadback_ReturnsWithoutValueAndSpeaksNothing()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateField(InputMode.Voice, speakValueInConfirmation: false),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "geheim"),
            configurePrivacy: _ => { });

        var result = await presenter.BuildUnderstoodValueAsync(turnContext, "geheim");

        Assert.False(result.IncludesValue);
        Assert.Equal(string.Empty, result.Value);
        speechService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildUnderstoodValueAsync_WhenFieldIsClearedForReadback_ReusesPrivacyAwareValueRendering()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateField(InputMode.Digits, speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "123456"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            });

        var result = await presenter.BuildUnderstoodValueAsync(turnContext, "fallback");

        Assert.Equal("1 2 3 4 5 6", result.Value);
        Assert.True(result.IncludesValue);
        speechService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("de", "Grün")]
    [InlineData("en", "Green")]
    [InlineData("fr", "Vert")]
    [InlineData("es", "Verde")]
    [InlineData("it", "Verde")]
    public async Task BuildAsync_WhenEnumValueSelected_ReadsBackLocalizedOptionLabelNotCanonicalCode(
        string isoCode,
        string expectedLabel)
    {
        // Gespeicherte Optionscodes anhand der bereitgestellten lokalisierten
        // Bezeichnungen in einen Ansagetext umsetzen.
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var options = ImmutableArray.Create(
            new FormChoiceOption("red", LocalizedColor("red", isoCode)),
            new FormChoiceOption("green", LocalizedColor("green", isoCode)),
            new FormChoiceOption("blue", LocalizedColor("blue", isoCode)));
        var turnContext = CreateTurnContext(
            CreateChoiceField(ValueTypeKinds.Enum, options, speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "green"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            },
            isoCode: isoCode);

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("green"),
            CancellationToken.None);

        Assert.Equal(expectedLabel, result.Value);
        Assert.True(result.IncludesValue);
    }

    [Theory]
    [InlineData("de", "E-Mail, Telefon")]
    [InlineData("en", "Email, Phone")]
    [InlineData("fr", "E-mail, Téléphone")]
    [InlineData("es", "Correo, Teléfono")]
    [InlineData("it", "E-mail, Telefono")]
    public async Task BuildAsync_WhenSetValueSelected_ReadsBackLocalizedLabelsForEveryCode(
        string isoCode,
        string expectedLabels)
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var options = ImmutableArray.Create(
            new FormChoiceOption("email", LocalizedChannel("email", isoCode)),
            new FormChoiceOption("phone", LocalizedChannel("phone", isoCode)));
        var turnContext = CreateTurnContext(
            CreateChoiceField(ValueTypeKinds.Set, options, speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "email|phone"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            },
            isoCode: isoCode);

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("email"),
            CancellationToken.None);

        Assert.Equal(expectedLabels, result.Value);
        Assert.DoesNotContain("|", result.Value, StringComparison.Ordinal);
        Assert.True(result.IncludesValue);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task BuildAsync_WhenLoneAcronymSetOptionSelected_ReadsBackTheLabelNotThePhoneticSpellOut(string isoCode)
    {
        // Eine aus dem Optionskatalog aufgelöste Bezeichnung bleibt als solche
        // erhalten, auch wenn sie nur aus Großbuchstaben besteht. Der echte
        // Buchstabierformatter macht eine versehentliche Umwandlung im Test sichtbar.
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(
            speechService.Object,
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var options = ImmutableArray.Create(
            new FormChoiceOption("sms", "SMS"),
            new FormChoiceOption("email", "E-Mail"));
        var turnContext = CreateTurnContext(
            CreateChoiceField(ValueTypeKinds.Set, options, speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "sms"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            },
            isoCode: isoCode);

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("sms"),
            CancellationToken.None);

        Assert.Equal("SMS", result.Value);
        Assert.DoesNotContain("wie", result.Value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Samuel", result.Value, StringComparison.Ordinal);
        Assert.True(result.IncludesValue);
    }

    [Theory]
    [InlineData("de", "keine")]
    [InlineData("en", "none")]
    [InlineData("fr", "aucune")]
    [InlineData("es", "ninguna")]
    [InlineData("it", "nessuna")]
    public async Task BuildAsync_WhenOptionalSetWasCompletedEmpty_ReadsBackLocalizedEmptySelectionWord(
        string isoCode,
        string expectedEmptyWord)
    {
        // Eine gespeicherte leere Auswahl soll mit dem sprachabhängigen Wort
        // für keine Auswahl in der Bestätigungsfrage erscheinen.
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var options = ImmutableArray.Create(
            new FormChoiceOption("email", LocalizedChannel("email", isoCode)),
            new FormChoiceOption("phone", LocalizedChannel("phone", isoCode)));
        var turnContext = CreateTurnContext(
            CreateChoiceField(ValueTypeKinds.Set, options, speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", string.Empty),
            configurePrivacy: _ => { },
            isoCode: isoCode);

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest(string.Empty),
            CancellationToken.None);

        Assert.Equal(expectedEmptyWord, result.Value);
        Assert.True(result.IncludesValue);
        Assert.Contains(expectedEmptyWord, result.Prompt);
    }

    [Fact]
    public async Task BuildAsync_WhenEmptySetOnMaskedField_StaysValueFree()
    {
        // Bei gesperrtem Vorlesen bleibt auch eine leere Auswahl ohne Wertangabe
        // in der Bestätigung.
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var options = ImmutableArray.Create(new FormChoiceOption("email", "E-Mail"));
        var turnContext = CreateTurnContext(
            CreateChoiceField(ValueTypeKinds.Set, options, speakValueInConfirmation: false),
            values: ImmutableDictionary<string, string>.Empty.Add("name", string.Empty),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.RequiresReadbackOptInAsync("name")).ReturnsAsync(false);
            });

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest(string.Empty),
            CancellationToken.None);

        Assert.Equal(string.Empty, result.Value);
        Assert.False(result.IncludesValue);
    }

    [Theory]
    [InlineData("de", "achter Januar neunzehnhundertachtzig")]
    [InlineData("en", "January 8, nineteen eighty")]
    [InlineData("fr", "8 janvier 1980")]
    [InlineData("es", "8 de enero de 1980")]
    [InlineData("it", "8 gennaio 1980")]
    public async Task BuildAsync_WhenDateValueStored_ReadsBackSpokenDateNotIsoLiteral(
        string isoCode,
        string expectedSpokenDate)
    {
        // Das gespeicherte ISO-Datum in einen Ansagetext der Sitzungssprache
        // umsetzen. Die tatsächliche Aussprache wird hier nicht geprüft.
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateChoiceField(ValueTypeKinds.Date, options: [], speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "1980-01-08"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            },
            isoCode: isoCode);

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("achter Januar 1980"),
            CancellationToken.None);

        Assert.Equal(expectedSpokenDate, result.Value);
        Assert.True(result.IncludesValue);
        Assert.DoesNotContain("1980-01-08", result.Prompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de", "14 Uhr 30")]
    [InlineData("en", "2:30 PM")]
    [InlineData("fr", "14 heures 30")]
    [InlineData("es", "las 14 y 30")]
    [InlineData("it", "le 14 e 30")]
    public async Task BuildAsync_WhenTimeValueStored_ReadsBackSpokenTimeNotClockLiteral(
        string isoCode,
        string expectedSpokenTime)
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateChoiceField(ValueTypeKinds.Time, options: [], speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", "14:30"),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            },
            isoCode: isoCode);

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("halb drei"),
            CancellationToken.None);

        Assert.Equal(expectedSpokenTime, result.Value);
        Assert.True(result.IncludesValue);
    }

    [Theory]
    [InlineData("de", "true", "ja")]
    [InlineData("de", "false", "nein")]
    [InlineData("en", "true", "yes")]
    [InlineData("en", "false", "no")]
    [InlineData("fr", "true", "oui")]
    [InlineData("fr", "false", "non")]
    [InlineData("es", "true", "s\u00ed")]
    [InlineData("es", "false", "no")]
    [InlineData("it", "true", "s\u00ec")]
    [InlineData("it", "false", "no")]
    public async Task BuildAsync_WhenBooleanValueStored_ReadsBackPerLanguageYesNoWordNotEnglishLiteral(
        string isoCode,
        string storedValue,
        string expectedSpokenWord)
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(speechService.Object, new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var turnContext = CreateTurnContext(
            CreateChoiceField(ValueTypeKinds.Boolean, options: [], speakValueInConfirmation: true),
            values: ImmutableDictionary<string, string>.Empty.Add("name", storedValue),
            configurePrivacy: privacy =>
            {
                privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync("name")).ReturnsAsync(true);
            },
            isoCode: isoCode);

        var result = await presenter.BuildAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest(expectedSpokenWord),
            CancellationToken.None);

        Assert.Equal(expectedSpokenWord, result.Value);
        Assert.True(result.IncludesValue);
        Assert.DoesNotContain(storedValue, result.Prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfirmationFormatting_RuntimeShape_HasNoStaticFormatterOrCompatibilityHandlerOverload()
    {
        Assert.False(typeof(FormConfirmationReadbackValueFormatter).IsAbstract);
        Assert.Null(typeof(FormConfirmationReadbackValueFormatter).GetMethod(
            "Format",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static));

        var presenterCtor = typeof(FormVoiceCommandConfirmationValuePresenter).GetConstructors().Single();
        Assert.Equal(typeof(IPhoneticSpellOutRenderer), presenterCtor.GetParameters()[1].ParameterType);
        Assert.False(presenterCtor.GetParameters()[1].HasDefaultValue);

        var handlerCtors = typeof(FormVoiceCommandConfirmationHandler).GetConstructors(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.Single(handlerCtors);
        Assert.Equal(5, handlerCtors[0].GetParameters().Length);

        var rendererMethods = typeof(IPhoneticSpellOutRenderer).GetMethods();
        Assert.Equal(["Render"], rendererMethods.Select(method => method.Name).ToArray());
        Assert.Equal(2, rendererMethods[0].GetParameters().Length);
    }

    private static string LocalizedColor(string code, string isoCode)
        => (code, isoCode) switch
        {
            ("red", "de") => "Rot",
            ("red", "en") => "Red",
            ("red", "fr") => "Rouge",
            ("red", "es") => "Rojo",
            ("red", "it") => "Rosso",
            ("green", "de") => "Grün",
            ("green", "en") => "Green",
            ("green", "fr") => "Vert",
            ("green", "es") => "Verde",
            ("green", "it") => "Verde",
            ("blue", "de") => "Blau",
            ("blue", "en") => "Blue",
            ("blue", "fr") => "Bleu",
            ("blue", "es") => "Azul",
            ("blue", "it") => "Blu",
            _ => code
        };

    private static string LocalizedChannel(string code, string isoCode)
        => (code, isoCode) switch
        {
            ("email", "de") => "E-Mail",
            ("email", "en") => "Email",
            ("email", "fr") => "E-mail",
            ("email", "es") => "Correo",
            ("email", "it") => "E-mail",
            ("phone", "de") => "Telefon",
            ("phone", "en") => "Phone",
            ("phone", "fr") => "Téléphone",
            ("phone", "es") => "Teléfono",
            ("phone", "it") => "Telefono",
            _ => code
        };

    private static FormFieldSnapshot CreateChoiceField(
        string valueTypeKind,
        ImmutableArray<FormChoiceOption> options,
        bool speakValueInConfirmation)
        => new(
            new FormFieldIdentitySnapshot("name", "name", "main"),
            new FormFieldPresentationSnapshot("Main", "Auswahl", HelpText: null, Examples: [], Options: options),
            new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode.Voice,
                EditabilityModes.UserEditable,
                valueTypeKind,
                speakValueInConfirmation));

    private static FormVoiceCommandFieldTurnContext CreateTurnContext(
        FormFieldSnapshot field,
        ImmutableDictionary<string, string> values,
        Action<Mock<IFormDialogPrivacyReadbackPolicy>> configurePrivacy,
        string isoCode = "de")
    {
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter.Setup(i => i.GetValuesSnapshotAsync()).ReturnsAsync(values);
        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Strict);
        configurePrivacy(privacy);

        var session = new EvaluationSession("session-test", EvaluationChannel.Voice, "app", "app/current");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);

        return new FormVoiceCommandFieldTurnContext(
            CreateContext(isoCode),
            interpreter.Object,
            field,
            session,
            new FormVoiceCommandStateTracker(telemetry.Object, session),
            new FormVoiceCommandAutoHelpTracker(
                new VoiceDialogSettings(),
                promptService.Object,
                new Mock<IFormVoiceCommandDialogStateTracker>().Object,
                interpreter.Object,
                isoCode),
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            privacy.Object);
    }

    private static FormFieldSnapshot CreateField(InputMode inputMode, bool speakValueInConfirmation)
        => new(
            new FormFieldIdentitySnapshot("name", "name", "main"),
            new FormFieldPresentationSnapshot("Main", "Name", HelpText: null, Examples: [], Options: []),
            new FormFieldInteractionSnapshot(
                CodeStyle: false,
                inputMode,
                EditabilityModes.UserEditable,
                ValueTypeKinds.String,
                speakValueInConfirmation));

    private static FormVoiceCommandContext CreateContext(string isoCode = "de")
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: isoCode,
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: []),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: new ApplicationDescriptor
                {
                    ApplicationId = "app",
                    DisplayName = new LocalizedText { ["de"] = "App" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms = [form]
                },
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: new FormContract
                {
                    SchemaVersion = "1",
                    ContractVersion = "1",
                    ApplicationId = "app",
                    FormId = FormId.From("current"),
                    DisplayName = new LocalizedText { ["de"] = "Formular" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    StrictLocalization = true
                }),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }
}
