using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.LanguageContracts;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

/// <summary>
/// Ermöglicht im Sitzungseinstieg getrennte Testfehler bei Frageausgabe und Aufnahme.
/// Antworten werden nach Sprache bereitgestellt und Ausgabeversuche aufgezeichnet; die übrigen Abhängigkeiten sind Testdoubles.
/// </summary>
internal sealed class SessionEntryUnspokenRungHarness
{
    private readonly List<(string IsoCode, string Text)> _spoken;
    private readonly string _sessionIsoCode;

    private SessionEntryUnspokenRungHarness(
        SessionEntryLanguageOfferTurn turn,
        List<(string IsoCode, string Text)> spoken,
        string sessionIsoCode)
    {
        Turn = turn;
        _spoken = spoken;
        _sessionIsoCode = sessionIsoCode;
    }

    public SessionEntryLanguageOfferTurn Turn { get; }

    /// <summary>
    /// Zählt Aufrufe mit dem genauen Fragetext der angebotenen Sprache, einschließlich fehlgeschlagener Ausgabeversuche.
    /// Der Textvergleich schließt Annahmemeldungen in derselben Sprache aus.
    /// </summary>
    public int QuestionAttemptsFor(string isoCode)
    {
        var question = string.Equals(isoCode, _sessionIsoCode, StringComparison.OrdinalIgnoreCase)
            ? SessionLanguageOfferPromptTextResolver.GetKeepQuestionSegment(isoCode, PolitenessLevel.Formal).Text
            : SessionLanguageOfferPromptTextResolver.GetLanguageQuestionSegment(isoCode, PolitenessLevel.Formal).Text;

        return _spoken.Count(call => call.Text == question);
    }

    public static SessionEntryUnspokenRungHarness Create(
        IReadOnlyDictionary<string, string?> transcripts,
        string sessionIsoCode = "de",
        string? failSpeakingOnce = null,
        string? failSpeakingAlways = null,
        string? failCaptureOnce = null)
    {
        var spoken = new List<(string IsoCode, string Text)>();
        var spokenIsoCodes = new List<string>();
        var speakFailures = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var captureFailures = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var textToSpeech = new Mock<ITextToSpeechPlayer>();
        textToSpeech
            .Setup(player => player.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string text, string isoCode, CancellationToken _) =>
            {
                spoken.Add((isoCode, text));
                spokenIsoCodes.Add(isoCode);

                if (failSpeakingAlways is not null
                    && string.Equals(isoCode, failSpeakingAlways, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"speech synthesis unavailable for '{isoCode}'");
                }

                if (failSpeakingOnce is not null
                    && string.Equals(isoCode, failSpeakingOnce, StringComparison.OrdinalIgnoreCase)
                    && Count(speakFailures, isoCode) == 0)
                {
                    Mark(speakFailures, isoCode);
                    throw new InvalidOperationException($"speech synthesis unavailable for '{isoCode}'");
                }

                return Task.CompletedTask;
            });

        var captured = new CapturedSpeechAudio(SpeechAudioMimeType.Wav, [new byte[] { 0 }]);
        var capture = new Mock<IModeAwareSpeechCaptureClient>();
        capture
            .Setup(client => client.RecordAsync(
                It.IsAny<SpeechCaptureMode>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                var isoCode = spokenIsoCodes.Count == 0 ? string.Empty : spokenIsoCodes[^1];
                if (failCaptureOnce is not null
                    && string.Equals(isoCode, failCaptureOnce, StringComparison.OrdinalIgnoreCase)
                    && Count(captureFailures, isoCode) == 0)
                {
                    Mark(captureFailures, isoCode);
                    throw new InvalidOperationException($"capture unavailable for '{isoCode}'");
                }

                return Task.FromResult(captured);
            });

        var recognition = new Mock<ISpeechRecognitionClient>();
        recognition
            .Setup(client => client.SpeechToTextAsync(
                It.IsAny<CapturedSpeechAudio>(),
                It.IsAny<string>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()))
            .Returns(() =>
            {
                // Antworten nach zuletzt ausgegebener Sprache statt nach Reihenfolge wählen, damit übersprungene Angebote keine Folgeantworten verschieben.
                var isoCode = spokenIsoCodes.Count == 0 ? string.Empty : spokenIsoCodes[^1];
                transcripts.TryGetValue(isoCode, out var answer);
                return Task.FromResult(new SpeechTranscript(answer, 90));
            });

        var languageState = new UserLanguageState();
        var dependencies = new SessionEntryLanguageOfferDependencies(
            languageState,
            new InMemoryUserLanguageSettingsWorkflow(languageState, new InMemoryUserLanguageSettingsStore()),
            textToSpeech.Object,
            Mock.Of<ISpeechCaptureRecordingIndicator>(),
            Mock.Of<ISpeechCaptureClient>(),
            capture.Object,
            new SpeechRecognitionWorker(recognition.Object),
            new CatalogWordSessionEntryCancelVocabulary(),
            NullLogger.Instance,
            Mock.Of<IEvaluationSessionTelemetry>());

        return new SessionEntryUnspokenRungHarness(
            new SessionEntryLanguageOfferTurn(dependencies, enabled: true),
            spoken,
            sessionIsoCode);
    }

    private static int Count(Dictionary<string, int> counters, string isoCode)
        => counters.TryGetValue(isoCode, out var value) ? value : 0;

    private static void Mark(Dictionary<string, int> counters, string isoCode)
        => counters[isoCode] = Count(counters, isoCode) + 1;
}
