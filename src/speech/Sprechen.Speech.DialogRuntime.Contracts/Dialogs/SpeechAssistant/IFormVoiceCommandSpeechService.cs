using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Sprachdienst des Formulardialogs für Feldantworten, Wertbestätigung, Ja/Nein
/// und Ansagen. Die Aufnahme ist über einen eigenen Vertrag angebunden.
/// </summary>
public interface IFormVoiceCommandSpeechService
{
    /// <summary>
    /// Erfasst eine Feldantwort mit optionalen Kontext-Keyterms, etwa den lokalisierten
    /// Bezeichnungen eines Auswahlfelds. Diese Hinweise beeinflussen die Spracherkennung.
    /// Die akzeptierte Grammatik und die anschließende deterministische Zuordnung bleiben
    /// unverändert. <paramref name="keyterms"/> steuert auch, ob die Basisbegriffe
    /// des Servers verwendet werden.
    /// </summary>
    Task<SpeechCaptureResult> CaptureTranscriptAsync(
        string prompt,
        string isoCode,
        FormVoiceCommandCaptureSettings captureSettings,
        Action onStartListening,
        SpeechCaptureTelemetryContext? telemetryContext,
        RecognitionProfile recognitionProfile,
        SpeechKeyterms? keyterms,
        CancellationToken cancellationToken);

    /// <summary>
    /// Erfasst eine Feldantwort in genau einem Aufnahmefenster. Der aufrufende Sammelablauf
    /// behandelt Stille selbst. Eine interne Wiederholung mit einem Hinweis auf fehlende Eingabe
    /// würde diesen Ablauf verändern. Der Erfassungsmodus entspricht CaptureTranscriptAsync.
    /// </summary>
    Task<SpeechCaptureResult> CaptureTranscriptSingleAttemptAsync(
        string prompt,
        string isoCode,
        FormVoiceCommandCaptureSettings captureSettings,
        Action onStartListening,
        SpeechCaptureTelemetryContext? telemetryContext,
        RecognitionProfile recognitionProfile,
        SpeechKeyterms? keyterms,
        CancellationToken cancellationToken);

    /// <summary>
    /// Erfasst eine Antwort auf die Wertbestätigung: annehmen, ändern, buchstabieren
    /// oder einen globalen Befehl. Nach der Bestätigungsansage gilt eine gelockerte
    /// Sprachstartprüfung, um kurze Antworten nach der Wiedergabe zu erfassen.
    /// Die Erkennung verwendet weiterhin das Gesprächsprofil. Feldwerte werden
    /// über <see cref="CaptureTranscriptAsync"/> erfasst.
    /// </summary>
    Task<SpeechCaptureResult> CaptureConfirmationTranscriptAsync(
        string prompt,
        string isoCode,
        FormVoiceCommandCaptureSettings captureSettings,
        Action onStartListening,
        SpeechCaptureTelemetryContext? telemetryContext,
        SpeechKeyterms? keyterms,
        CancellationToken cancellationToken);

    Task<bool?> ConfirmYesNoAsync(
        FormVoiceCommandCaptureSettings captureSettings,
        string prompt,
        string isoCode,
        SpeechCaptureTelemetryContext? telemetryContext,
        CancellationToken cancellationToken);

    Task SpeakSafeAsync(
        string text,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken);

    /// <summary>
    /// Wiederholt eine Ansage langsamer, ohne sie erneut im Wiederholpuffer
    /// aufzuzeichnen. Dadurch bleibt die automatische Wiederholung als verbraucht
    /// markiert. Der nutzerseitige Befehl <c>repeat</c> verwendet dagegen
    /// <see cref="SpeakSafeAsync(string,string,VoiceDialogSettings,CancellationToken)"/>.
    /// </summary>
    Task SpeakReplayAsync(
        string text,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken);
}
