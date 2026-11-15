using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Runtime.Services;
using Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Buchstabierung, Vorlesen und Bestätigung mit Produktionskomponenten und
/// vorgegebenen Transkripten. Die fünfsprachigen Feldtests vergleichen ein geheimes Feld,
/// ein sensibles Feld mit abgelehnter Vorlesezustimmung und ein öffentliches Vergleichsfeld.
/// Geprüft werden übernommene Werte und Ausgabeaufrufe; weitere Fälle prüfen
/// Hinweisreihenfolge und Korrektur bei öffentlichem Vorlesen. Die Doubles messen
/// weder physische Aufnahme noch tatsächlich hörbare Wiedergabe.
/// </summary>
public sealed class SpellingSecretFieldPrivacyEndToEndTests
{
    // Dieselben NATO-Buchstabierwörter ergeben den Testwert in allen fünf Sprachfällen.
    private const string SpelledSecretTranscript = "sierra echo charlie romeo echo tango";
    private const string ExpectedSecretValue = "secret";

    // Annahme- und Absendewort je Sprache aus dem Standard-Kommandokatalog.
    private static readonly IReadOnlyDictionary<string, string> AcceptWords = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["de"] = "übernehmen", ["en"] = "accept", ["fr"] = "accepter", ["es"] = "aceptar", ["it"] = "accettare"
    };

    private static readonly IReadOnlyDictionary<string, string> SubmitWords = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["de"] = "absenden", ["en"] = "submit", ["fr"] = "envoyer", ["es"] = "enviar", ["it"] = "invia"
    };

    [Fact]
    public async Task SensitiveBlock_ProductionCaptureAndWorkflow_ProceedDirectlyToNextListening()
    {
        var auditoryEvents = new List<string>();
        var earcon = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earcon
            .Setup(player => player.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
            .Callback((EarconId id, CancellationToken _) => auditoryEvents.Add($"earcon:{id}"))
            .Returns(ValueTask.CompletedTask);

        var innerIndicator = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Strict);
        innerIndicator
            .Setup(indicator => indicator.BeginRecordingAsync())
            .Callback(() => auditoryEvents.Add("recording:begin"))
            .Returns(new ValueTask<IAsyncDisposable>(Mock.Of<IAsyncDisposable>()));
        var captureBoundary = new ListeningCueRecordingIndicatorDecorator(
            innerIndicator.Object,
            earcon.Object,
            NullLogger<ListeningCueRecordingIndicatorDecorator>.Instance);

        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()])));
        speechClient.EnqueueTranscript("delta echo one two", confidencePercent: 97);
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()])));
        speechClient.EnqueueTranscript("done", confidencePercent: 99);

        await using var recognitionWorker = new SpeechRecognitionWorker(speechClient);
        var speechService = new FormVoiceCommandSpeechService(
            Mock.Of<IEvaluationSessionTelemetry>(),
            captureBoundary,
            Mock.Of<ISpeechBargeInSignals>(),
            recognitionWorker,
            new RecordingTextToSpeechPlayer(message => auditoryEvents.Add(message)),
            new FormVoiceCommandRecordingService(speechClient, speechClient),
            NullLogger<FormVoiceCommandSpeechService>.Instance);
        var contract = CreateSpellingContract(PrivacyClass.Sensitive);
        var context = CreateContext(
            contract,
            "en",
            new VoiceDialogSettings { MaxRetries = 0, NoInputTimeoutMs = 100 });

        var initialCapture = await speechService.CaptureTranscriptAsync(
            "Spell the IBAN block.",
            context.IsoCode,
            context.CaptureSettings.WithInputAcceptanceFeedback(SpeechInputAcceptanceFeedback.CueFree),
            onStartListening: () => { },
            telemetryContext: null,
            RecognitionProfile.Spelling,
            keyterms: null,
            CancellationToken.None);

        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Strict);
        privacy
            .Setup(policy => policy.IsSpellOutAllowedInConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var formTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose);
        var parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());
        var workflow = new FormVoiceCommandSpellingWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            new SpellingWorkflowCommandResolver(
                new SpellingWorkflowLocalCommandMapper(),
                new SpellingWorkflowGlobalCommandMapper()),
            parser,
            speechService,
            new SpellingWorkflowCommandActionFlow(
                formTelemetry.Object,
                speechService,
                new SpellingWorkflowNoValuePrompt(speechService)),
            new SpellingWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(formTelemetry.Object, speechService)),
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));

        var session = new EvaluationSession("cue-session", EvaluationChannel.Voice, "app", "form-1");
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            context.IsoCode,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var stateTracker = ArmFieldLoop(new FormVoiceCommandStateTracker(telemetry.Object, session));
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            Mock.Of<IFormVoiceCommandPromptService>(),
            stateTracker,
            interpreter,
            context.IsoCode);
        var field = await interpreter.GetFocusedFieldAsync();

        var result = await workflow.CaptureSpellingTranscriptAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            privacy.Object,
            initialCapture.Transcript!,
            initialCapture.SttConfidencePercent,
            CancellationToken.None);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal("de12", result.CapturedTranscript);
        Assert.Equal(
            [
                "tts:en:Spell the IBAN block.",
                $"earcon:{EarconId.FlowListening}",
                "recording:begin",
                // Kein Ack, kein Readback-Ton, aber genau eine inhaltsfreie Aufforderung, die das
                // Abschlusswort nennt.
                "tts:en:Say more letters, or done.",
                $"earcon:{EarconId.FlowListening}",
                "recording:begin"
            ],
            auditoryEvents);
    }

    [Fact]
    public async Task PublicEmail_ProductionCaptureParserAndReadback_SpeakEverySymbolBeforeNextListening()
    {
        const string expectedEmail = "marta.smith@example.co.uk";
        var auditoryEvents = new List<string>();
        var earcon = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earcon
            .Setup(player => player.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
            .Callback((EarconId id, CancellationToken _) => auditoryEvents.Add($"earcon:{id}"))
            .Returns(ValueTask.CompletedTask);

        var innerIndicator = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Strict);
        innerIndicator
            .Setup(indicator => indicator.BeginRecordingAsync())
            .Callback(() => auditoryEvents.Add("recording:begin"))
            .Returns(new ValueTask<IAsyncDisposable>(Mock.Of<IAsyncDisposable>()));
        var captureBoundary = new ListeningCueRecordingIndicatorDecorator(
            innerIndicator.Object,
            earcon.Object,
            NullLogger<ListeningCueRecordingIndicatorDecorator>.Instance);

        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()])));
        speechClient.EnqueueTranscript("marta dot smith at example dot co dot uk", confidencePercent: 97);
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()])));
        speechClient.EnqueueTranscript("done", confidencePercent: 99);

        await using var recognitionWorker = new SpeechRecognitionWorker(speechClient);
        var speechService = new FormVoiceCommandSpeechService(
            Mock.Of<IEvaluationSessionTelemetry>(),
            captureBoundary,
            Mock.Of<ISpeechBargeInSignals>(),
            recognitionWorker,
            new RecordingTextToSpeechPlayer(message => auditoryEvents.Add(message)),
            new FormVoiceCommandRecordingService(speechClient, speechClient),
            NullLogger<FormVoiceCommandSpeechService>.Instance);
        var contract = CreateSpellingContract(privacyClass: null);
        var context = CreateContext(
            contract,
            "en",
            new VoiceDialogSettings { MaxRetries = 0, NoInputTimeoutMs = 100 });

        var initialCapture = await speechService.CaptureTranscriptAsync(
            "Spell the email address.",
            context.IsoCode,
            context.CaptureSettings.WithInputAcceptanceFeedback(SpeechInputAcceptanceFeedback.PostInterpretation),
            onStartListening: () => { },
            telemetryContext: null,
            RecognitionProfile.Spelling,
            keyterms: null,
            CancellationToken.None);

        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Strict);
        privacy
            .Setup(policy => policy.IsSpellOutAllowedInConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var formTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose);
        var parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());
        var workflow = new FormVoiceCommandSpellingWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            new SpellingWorkflowCommandResolver(
                new SpellingWorkflowLocalCommandMapper(),
                new SpellingWorkflowGlobalCommandMapper()),
            parser,
            speechService,
            new SpellingWorkflowCommandActionFlow(
                formTelemetry.Object,
                speechService,
                new SpellingWorkflowNoValuePrompt(speechService)),
            new SpellingWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(formTelemetry.Object, speechService)),
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));

        var session = new EvaluationSession("email-cue-session", EvaluationChannel.Voice, "app", "form-1");
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            context.IsoCode,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var stateTracker = ArmFieldLoop(new FormVoiceCommandStateTracker(telemetry.Object, session));
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            Mock.Of<IFormVoiceCommandPromptService>(),
            stateTracker,
            interpreter,
            context.IsoCode);

        var result = await workflow.CaptureSpellingTranscriptAsync(
            context,
            interpreter,
            await interpreter.GetFocusedFieldAsync(),
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            privacy.Object,
            initialCapture.Transcript!,
            initialCapture.SttConfidencePercent,
            CancellationToken.None);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal(expectedEmail, result.CapturedTranscript);
        var symbolReadback = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance)
            .Render(expectedEmail, "en");
        Assert.Equal(3, CountOccurrences(symbolReadback, "dot"));
        Assert.Equal(1, CountOccurrences(symbolReadback, "at sign"));
        Assert.Equal(
            [
                "tts:en:Spell the email address.",
                $"earcon:{EarconId.FlowListening}",
                "recording:begin",
                $"tts:en:{symbolReadback}",
                $"tts:en:{FormSpellingWorkflowPromptBuilder.GetSpellingWorkflowPrompt(contract, "en", expectedEmail, context.VoiceSettings.GetPolitenessLevel())}",
                $"earcon:{EarconId.FlowListening}",
                "recording:begin"
            ],
            auditoryEvents);
    }

    [Fact]
    public async Task PublicName_HighConfidenceCharacterLoss_IsReadBackAndRepairedBeforeFinalStorage()
    {
        var auditoryEvents = new List<string>();
        var earcon = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earcon
            .Setup(player => player.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
            .Callback((EarconId id, CancellationToken _) => auditoryEvents.Add($"earcon:{id}"))
            .Returns(ValueTask.CompletedTask);

        var innerIndicator = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Strict);
        innerIndicator
            .Setup(indicator => indicator.BeginRecordingAsync())
            .Callback(() => auditoryEvents.Add("recording:begin"))
            .Returns(new ValueTask<IAsyncDisposable>(Mock.Of<IAsyncDisposable>()));
        var captureBoundary = new ListeningCueRecordingIndicatorDecorator(
            innerIndicator.Object,
            earcon.Object,
            NullLogger<ListeningCueRecordingIndicatorDecorator>.Instance);

        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()])));
        speechClient.EnqueueTranscript("capital m r t", confidencePercent: 98);
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()])));
        speechClient.EnqueueTranscript("clear all", confidencePercent: 99);
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()])));
        speechClient.EnqueueTranscript("capital m a r t a", confidencePercent: 99);
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromResult(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()])));
        speechClient.EnqueueTranscript("done", confidencePercent: 99);

        await using var recognitionWorker = new SpeechRecognitionWorker(speechClient);
        var speechService = new FormVoiceCommandSpeechService(
            Mock.Of<IEvaluationSessionTelemetry>(),
            captureBoundary,
            Mock.Of<ISpeechBargeInSignals>(),
            recognitionWorker,
            new RecordingTextToSpeechPlayer(message => auditoryEvents.Add(message)),
            new FormVoiceCommandRecordingService(speechClient, speechClient),
            NullLogger<FormVoiceCommandSpeechService>.Instance);
        var contract = CreateSpellingContract(privacyClass: null);
        var context = CreateContext(
            contract,
            "en",
            new VoiceDialogSettings { MaxRetries = 0, NoInputTimeoutMs = 100 });

        var initialCapture = await speechService.CaptureTranscriptAsync(
            "Spell the first name.",
            context.IsoCode,
            context.CaptureSettings.WithInputAcceptanceFeedback(SpeechInputAcceptanceFeedback.PostInterpretation),
            onStartListening: () => { },
            telemetryContext: null,
            RecognitionProfile.Spelling,
            keyterms: null,
            CancellationToken.None);

        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Strict);
        privacy
            .Setup(policy => policy.IsSpellOutAllowedInConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var formTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose);
        var parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());
        var workflow = new FormVoiceCommandSpellingWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            new SpellingWorkflowCommandResolver(
                new SpellingWorkflowLocalCommandMapper(),
                new SpellingWorkflowGlobalCommandMapper()),
            parser,
            speechService,
            new SpellingWorkflowCommandActionFlow(
                formTelemetry.Object,
                speechService,
                new SpellingWorkflowNoValuePrompt(speechService)),
            new SpellingWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(formTelemetry.Object, speechService)),
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));

        var session = new EvaluationSession("marta-recovery-session", EvaluationChannel.Voice, "app", "form-1");
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            context.IsoCode,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var stateTracker = ArmFieldLoop(new FormVoiceCommandStateTracker(telemetry.Object, session));
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            Mock.Of<IFormVoiceCommandPromptService>(),
            stateTracker,
            interpreter,
            context.IsoCode);

        var result = await workflow.CaptureSpellingTranscriptAsync(
            context,
            interpreter,
            await interpreter.GetFocusedFieldAsync(),
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            privacy.Object,
            initialCapture.Transcript!,
            initialCapture.SttConfidencePercent,
            CancellationToken.None);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal("Marta", result.CapturedTranscript);
        Assert.Equal(99, result.AggregatedConfidencePercent);
        var applyResult = await interpreter.TryApplyComposedValueAsync(result.CapturedTranscript!);
        var storedValues = await interpreter.GetValuesSnapshotAsync();
        Assert.True(applyResult.ValueCaptured);
        Assert.Equal("Marta", storedValues["name"]);

        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);
        var truncatedReadback = renderer.Render("Mrt", "en");
        var repairedReadback = renderer.Render("Marta", "en");
        var politeness = context.VoiceSettings.GetPolitenessLevel();
        Assert.Equal(
            [
                "tts:en:Spell the first name.",
                $"earcon:{EarconId.FlowListening}",
                "recording:begin",
                $"tts:en:{truncatedReadback}",
                $"tts:en:{FormSpellingWorkflowPromptBuilder.GetSpellingWorkflowPrompt(contract, "en", "Mrt", politeness)}",
                $"earcon:{EarconId.FlowListening}",
                "recording:begin",
                $"tts:en:{FormSpellingWorkflowPromptBuilder.GetSpellingBufferClearedPrompt(contract, "en", politeness)}",
                $"tts:en:{FormSpellingWorkflowPromptBuilder.GetSpellingWorkflowPrompt(contract, "en", string.Empty, politeness)}",
                $"earcon:{EarconId.FlowListening}",
                "recording:begin",
                $"tts:en:{repairedReadback}",
                $"tts:en:{FormSpellingWorkflowPromptBuilder.GetSpellingWorkflowPrompt(contract, "en", "Marta", politeness)}",
                $"earcon:{EarconId.FlowListening}",
                "recording:begin"
            ],
            auditoryEvents);
        Assert.DoesNotContain($"earcon:{EarconId.FlowAck}", auditoryEvents);
        Assert.DoesNotContain($"earcon:{EarconId.FlowProcessing}", auditoryEvents);
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(token, offset, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            offset += token.Length;
        }

        return count;
    }

    // Die Fixture des geheimen Feldes verbietet Spracheingabe. Der Interpreter darf
    // den vorbereiteten Buchstabierwert deshalb nicht übernehmen.
    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task SpellingSecretField_VoiceSpellingBlockedByPolicy_SecretNeverCapturedAndNeverAudible(
        string isoCode)
    {
        var feedback = new FeedbackLog();
        var drive = await DriveSpellingFieldAsync(isoCode, PrivacyClass.Secret, feedback);

        Assert.Null(drive.CommittedValue);

        // Weder der vollständige Testwert noch seine Buchstabierwörter dürfen in den
        // aufgezeichneten Ansagen erscheinen; auch FlowAck darf nicht angefordert werden.
        foreach (var alphabetWord in AudibleAlphabetWords(ExpectedSecretValue, isoCode))
        {
            Assert.DoesNotContain(
                feedback.SpokenLines,
                line => line.Contains(alphabetWord, StringComparison.OrdinalIgnoreCase));
        }
        Assert.DoesNotContain(
            feedback.SpokenLines,
            line => line.Contains(ExpectedSecretValue, StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(EarconId.FlowAck, feedback.Earcons);
    }

    // Das öffentliche Vergleichsfeld muss alle erwarteten Buchstabierwörter ausgeben,
    // damit ein nie ausgeführter Renderer die Negativprüfung nicht scheinbar erfüllt.
    // FlowAck darf diese Wertausgabe nicht ersetzen.
    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task SpellingPublicField_DoesSpeakTheSpelledValue_Control(string isoCode)
    {
        var feedback = new FeedbackLog();
        var drive = await DriveSpellingFieldAsync(isoCode, privacyClass: null, feedback);

        Assert.Equal(ExpectedSecretValue, drive.CommittedValue);

        var alphabetWords = AudibleAlphabetWords(ExpectedSecretValue, isoCode);
        Assert.NotEmpty(alphabetWords);
        Assert.All(
            alphabetWords,
            word => Assert.Contains(
                feedback.SpokenLines,
                line => line.Contains(word, StringComparison.OrdinalIgnoreCase)));

        Assert.DoesNotContain(EarconId.FlowAck, feedback.Earcons);
    }

    // Bei abgelehnter Vorlesezustimmung bleibt die Übernahme des sensiblen Werts möglich.
    // Die aufgezeichneten Ansagen dürfen weder Vollwert noch Buchstabierwörter enthalten
    // und die unterdrückte Ausgabe nicht durch FlowAck ersetzen.
    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task SpellingSensitiveField_SpellsSilently_ReadbackSuppressed_ConfirmationMasked(string isoCode)
    {
        var feedback = new FeedbackLog();
        var drive = await DriveSpellingFieldAsync(isoCode, PrivacyClass.Sensitive, feedback, confirmReadbackOptIn: false);

        Assert.Equal(ExpectedSecretValue, drive.CommittedValue);

        foreach (var alphabetWord in AudibleAlphabetWords(ExpectedSecretValue, isoCode))
        {
            Assert.DoesNotContain(
                feedback.SpokenLines,
                line => line.Contains(alphabetWord, StringComparison.OrdinalIgnoreCase));
        }
        Assert.DoesNotContain(
            feedback.SpokenLines,
            line => line.Contains(ExpectedSecretValue, StringComparison.OrdinalIgnoreCase));

        // Annahmehinweis und folgende Aufnahme liegen außerhalb dieses Helfers;
        // die geprüfte Komponente darf keinen zusätzlichen Hinweis je Zeichenblock erzeugen.
        Assert.DoesNotContain(EarconId.FlowAck, feedback.Earcons);

        // In dieser Fixture enthält die maskierte Bestätigung nach abgelehnter Zustimmung nur die Endung cret.
        Assert.Contains(
            feedback.SpokenLines,
            line => line.Contains("cret", StringComparison.OrdinalIgnoreCase));
    }

    // Das letzte Wort jedes phonetischen Abschnitts dient als Suchwert, etwa Samuel
    // aus S wie Samuel. Einzelbuchstaben und Bindewörter werden ausgelassen,
    // damit die Prüfung auf die tatsächlich werttragenden Buchstabierwörter zielt.
    private static IReadOnlyList<string> AudibleAlphabetWords(string value, string isoCode)
        => new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance).Render(value, isoCode)
            .Split(['.', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[^1])
            .Where(word => word.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static async Task<DriveResult> DriveSpellingFieldAsync(
        string isoCode,
        PrivacyClass? privacyClass,
        FeedbackLog feedback,
        bool confirmReadbackOptIn = false)
    {
        var session = new EvaluationSession("secret-session", EvaluationChannel.Voice, "app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);

        // Sprachdienst- und Hinweistonaufrufe werden gemeinsam protokolliert; die Wiedergabe bleibt simuliert.
        // Die modellierte Eingaberegel erlaubt diesem sensiblen Feld die Aufnahme
        // ohne vorgeschaltete Zustimmung zum Vorlesen.
        // Nach dem Zeichenblock beenden zwei stille Ergebnisse die Sammlung.
        // Annahme- und Absendewort beantworten danach Bestätigung und Abschluss; bei leerer Queue liefert der Mock null.
        var transcripts = new Queue<string?>(
            [SpelledSecretTranscript, null, null, AcceptWords[isoCode], SubmitWords[isoCode]]);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService.ForwardSingleAttemptCaptureToValueCapture();
        speechService.SetReturnsDefault(Task.CompletedTask);
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                isoCode,
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (prompt, _, _, onStartListening, _, _, _, _) =>
                {
                    feedback.RecordCapturePrompt(prompt);
                    onStartListening();
                    return Task.FromResult<SpeechCaptureResult>(
                        new SpeechCaptureResult(transcripts.Count > 0 ? transcripts.Dequeue() : null, null));
                });
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), isoCode, It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) =>
            {
                feedback.RecordSpoken(text);
                return Task.CompletedTask;
            });
        speechService
            .Setup(s => s.SpeakReplayAsync(It.IsAny<string>(), isoCode, It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) =>
            {
                feedback.RecordSpoken(text);
                return Task.CompletedTask;
            });
        speechService
            .Setup(s => s.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<string>(),
                isoCode,
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .Returns<FormVoiceCommandCaptureSettings, string, string, SpeechCaptureTelemetryContext?, CancellationToken>(
                (_, prompt, _, _, _) =>
                {
                    feedback.RecordConfirmPrompt(prompt);
                    return Task.FromResult<bool?>(confirmReadbackOptIn);
                });

        var earcon = new Mock<IEarconPlayer>(MockBehavior.Loose);
        earcon
            .Setup(e => e.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
            .Returns<EarconId, CancellationToken>((id, _) =>
            {
                feedback.RecordEarcon(id);
                return ValueTask.CompletedTask;
            });

        // Das erfolgreiche Absendeergebnis schließt die fertig bearbeitete Feldeingabe ab.
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Loose);
        submissionService
            .Setup(s => s.TrySubmitAsync(
                It.IsAny<IFormDialogRuntime>(),
                It.IsAny<FormContract>(),
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<IFormVoiceCommandDialogStateTracker>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormSubmissionResult.Submitted);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Loose);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(commandId => commandId == GlobalCommandIds.Submit),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                FormId.From("current"),
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object,
            earcon.Object);

        var contract = CreateSpellingContract(privacyClass);
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: isoCode,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var context = CreateContext(contract, isoCode);
        var stateTracker = ArmFieldLoop(new FormVoiceCommandStateTracker(telemetry.Object, session));
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose).Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var field = await interpreter.GetFocusedFieldAsync();

        // Die bevorzugte Buchstabierung des geheimen Feldes ist nicht verfügbar;
        // nach der Auflösung verhindert die Eingabeprüfung die Aufnahme. Die übrigen Fälle behalten Buchstabierung.
        Assert.Equal(privacyClass == PrivacyClass.Secret ? InputMode.Voice : InputMode.Spelling, field.InputMode);

        await handler.HandleAsync(
            new FormVoiceCommandFieldTurnContext(
                context,
                interpreter,
                field,
                session,
                stateTracker,
                autoHelp,
                new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
                interpreter as IFormDialogPrivacyReadbackPolicy ?? Mock.Of<IFormDialogPrivacyReadbackPolicy>()),
            CancellationToken.None);

        var values = await interpreter.GetValuesSnapshotAsync();
        return new DriveResult(values.TryGetValue("name", out var committed) ? committed : null);
    }

    private static FormContract CreateSpellingContract(PrivacyClass? privacyClass)
        => new()
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1", ["en"] = "Part 1", ["fr"] = "Partie 1", ["es"] = "Parte 1", ["it"] = "Parte 1" },
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            StrictLocalization = false,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement,
                    PrivacyClass = privacyClass,
                    SensitiveDataPolicy = privacyClass == PrivacyClass.Sensitive
                        ? new SensitiveDataPolicy
                        {
                            ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackOptInRequired
                        }
                        : null
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name", ["en"] = "Name", ["fr"] = "Nom", ["es"] = "Nombre", ["it"] = "Nome" },
                    PreferredInputModes = [InputModes.Spelling],
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main", ["en"] = "Main", ["fr"] = "Main", ["es"] = "Main", ["it"] = "Main" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }
                    ]
                }
            ]
        };

    private static FormVoiceCommandContext CreateContext(
        FormContract contract,
        string isoCode,
        VoiceDialogSettings? voiceSettings = null)
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            Description = new LocalizedText { ["de"] = "Teil 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: isoCode,
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: ["okay"]),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: voiceSettings ?? new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }

    // Die Feldkomponenten verwenden protokollierende Ausgabedoubles. Der echte
    // PhoneticSpellOutRenderer macht zugelassene Wertausgabe bei Buchstabierung
    // und Bestätigung in den aufgezeichneten Anforderungen sichtbar.
    private static FormVoiceCommandFieldInteractionHandler CreateFieldInteractionHandler(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService,
        IFormVoiceCommandSubmissionService submissionService,
        IFormVoiceCommandGlobalCommandRouter commandRouter,
        IEarconPlayer earconPlayer)
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);
        var commandExecutionService = new FormVoiceCommandGlobalCommandExecutionService(telemetry, commandRouter);
        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var errorHandlingService = new FormVoiceCommandFieldErrorHandlingService(
            telemetry,
            speechService,
            replayBuffer,
            new CaptureReplayPolicy());
        var parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());
        var ambiguityTelemetry = new FormVoiceCommandInputVsCommandTelemetry(telemetry, new InputVsCommandResolutionStrategyMapper());

        var digitsWorkflowService = new FormVoiceCommandDigitsWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(commandExecutionService),
            parser,
            new DigitsWorkflowCommandResolver(new DigitsWorkflowLocalCommandMapper(), new DigitsWorkflowGlobalCommandMapper()),
            new DigitsWorkflowCommandActionFlow(telemetry, speechService, new DigitsWorkflowNoValuePrompt(speechService)),
            new DigitsWorkflowInputRejectionFlow(new WorkflowUnrecognizedInputAnnouncer(telemetry, speechService)),
            new DigitsWorkflowTranscriptCapture(speechService));

        var spellingWorkflowService = new FormVoiceCommandSpellingWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(commandExecutionService),
            new SpellingWorkflowCommandResolver(new SpellingWorkflowLocalCommandMapper(), new SpellingWorkflowGlobalCommandMapper()),
            parser,
            speechService,
            new SpellingWorkflowCommandActionFlow(telemetry, speechService, new SpellingWorkflowNoValuePrompt(speechService)),
            new SpellingWorkflowInputRejectionFlow(new WorkflowUnrecognizedInputAnnouncer(telemetry, speechService)),
            renderer);

        var transcriptOrchestrator = new FormVoiceCommandFieldTranscriptOrchestrator(
            new FormVoiceCommandFieldTranscriptCapture(speechService, errorHandlingService),
            new FormVoiceCommandTranscriptInterpretationService(),
            new ParsedCommandBranch(
                commandExecutionService,
                errorHandlingService,
                ambiguityTelemetry,
                new FormVoiceCommandConfirmationDecisionService(
                    new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                    new FormVoiceCommandConfirmationTranscriptInterpreter(
                        new FormVoiceCommandConfirmationCommandDecisionMapper(),
                        parser),
                    new FormVoiceCommandConfirmationMissingDecisionPolicy()),
                telemetry),
            new FormVoiceCommandFieldEntryDependencies(
                new FormVoiceCommandInputAvailabilityGuard(errorHandlingService),
                new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                new FormVoiceCommandReadOnlyFieldSpeaker(speechService, replayBuffer)),
            new FormVoiceCommandDateComponentTranscriptBranch(
                new DateComponentEntryWorkflowDriver(speechService, parser, parser)),
            new FormVoiceCommandDigitsTranscriptBranch(digitsWorkflowService),
            new FormVoiceCommandSpellingTranscriptBranch(spellingWorkflowService),
            new FormVoiceCommandSetTranscriptBranch(
                new FormVoiceCommandSetWorkflowService(
                    new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
                    AnchoredCommandFlowTestFactory.Create(commandExecutionService),
                    new SetWorkflowCommandResolver(new SetWorkflowGlobalCommandMapper()),
                    new SetWorkflowCommandActionFlow(telemetry),
                    new SetWorkflowInputRejectionFlow(new WorkflowUnrecognizedInputAnnouncer(telemetry, speechService)),
                    parser,
                    speechService)),
            new FormVoiceCommandAmbiguityTranscriptBranch(
                new FormVoiceCommandAmbiguityResolver(),
                errorHandlingService,
                ambiguityTelemetry));

        var fieldApplyService = new FormVoiceCommandFieldApplyService(
            telemetry,
            telemetry,
            errorHandlingService,
            new FormVoiceCommandConfirmationHandler(
                new FormVoiceCommandConfirmationValuePresenter(speechService, renderer),
                new FormVoiceCommandConfirmationDecisionService(
                    new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                    new FormVoiceCommandConfirmationTranscriptInterpreter(
                        new FormVoiceCommandConfirmationCommandDecisionMapper(),
                        parser),
                    new FormVoiceCommandConfirmationMissingDecisionPolicy()),
                new FormVoiceCommandConfirmationRepairActionFlow(telemetry, speechService, commandExecutionService),
                new FormVoiceCommandConfirmedFieldCompletionFlow(
                    speechService,
                    earconPlayer,
                    new FormClosingReviewTurnFlow(speechService, commandExecutionService)),
                errorHandlingService),
            new FormVoiceCommandConfirmationValuePresenter(speechService, renderer),
            new FormVoiceCommandAdditionalFieldProposalHandler(
                telemetry,
                new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()),
                speechService),
            new FormVoiceCommandSpellingEscalationFlow(
                new SpellingConfidenceEscalationPolicy(),
                new FormVoiceCommandConfirmationValuePresenter(speechService, renderer),
                new FormVoiceCommandConfirmationDecisionService(
                    new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                    new FormVoiceCommandConfirmationTranscriptInterpreter(
                        new FormVoiceCommandConfirmationCommandDecisionMapper(),
                        parser),
                    new FormVoiceCommandConfirmationMissingDecisionPolicy()),
                new FormVoiceCommandConfirmationRepairActionFlow(telemetry, speechService, commandExecutionService),
                earconPlayer),
            new FormRepairReasonResolver());

        return new FormVoiceCommandFieldInteractionHandler(
            telemetry,
            speechService,
            transcriptOrchestrator,
            fieldApplyService,
            replayBuffer);
    }

    private static FormVoiceCommandStateTracker ArmFieldLoop(FormVoiceCommandStateTracker tracker)
    {
        tracker.BeginFieldPrompt("main");
        tracker.BeginListening();
        return tracker;
    }
}
