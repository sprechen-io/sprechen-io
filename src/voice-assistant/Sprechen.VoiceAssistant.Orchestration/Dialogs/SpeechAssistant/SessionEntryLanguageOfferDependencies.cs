using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Dienste für Einstiegsfragen, Antworterkennung und Spracheinstellungen,
/// damit der Fragenablauf sie über ihre jeweiligen Verträge anspricht.
/// </summary>
/// <param name="UserLanguageState">Liefert die Anredeform für die Fragen.</param>
/// <param name="UserLanguageSettingsWorkflow">Liest und schreibt Sprach- und Angebotsmerker.</param>
/// <param name="TextToSpeechPlayer">Übernimmt die Wiedergabe der Fragen und Rückmeldungen.</param>
/// <param name="RecordingIndicator">Stellt Aufnahmeindikator und hörbare Eingabesignale bereit.</param>
/// <param name="SpeechCaptureClient">Bereitet die Aufnahmesitzung vor dem Fragezug vor.</param>
/// <param name="ModeAwareSpeechCaptureClient">Nimmt Antworten im Modus für kurze Bestätigungen auf.</param>
/// <param name="SpeechRecognitionWorker">Reiht die Erkennung der aufgenommenen Antworten ein.</param>
/// <param name="CancelVocabulary">
/// Löst Abbruchäußerungen mit der beim Start bekannten Sprache und Anrede aus dem Katalog auf.
/// </param>
/// <param name="Logger">Protokolliert Ergebnisse und nicht verfügbare Schritte des Einstiegs.</param>
/// <param name="EvaluationSessionTelemetry">Erfasst gemeldete fehlende Sprache vor der Formularsitzung.</param>
internal sealed record SessionEntryLanguageOfferDependencies(
    IUserLanguageState UserLanguageState,
    IUserLanguageSettingsWorkflow UserLanguageSettingsWorkflow,
    ITextToSpeechPlayer TextToSpeechPlayer,
    ISpeechCaptureRecordingIndicator RecordingIndicator,
    ISpeechCaptureClient SpeechCaptureClient,
    IModeAwareSpeechCaptureClient ModeAwareSpeechCaptureClient,
    SpeechRecognitionWorker SpeechRecognitionWorker,
    ISessionEntryCancelVocabulary CancelVocabulary,
    ILogger Logger,
    IEvaluationSessionTelemetry EvaluationSessionTelemetry);
