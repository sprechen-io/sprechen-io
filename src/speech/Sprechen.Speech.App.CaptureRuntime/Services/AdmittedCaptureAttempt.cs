namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verbindet die gemeinsame Aufnahmezulassung mit der für diesen Versuch bestimmten
/// Sprachpegelschwelle, damit beide zusammen an die Aufnahme weitergegeben werden.
/// </summary>
internal sealed record AdmittedCaptureAttempt
{
    internal AdmittedCaptureAttempt(CaptureAttemptAdmission admission, int speechAmplitudeThreshold)
    {
        Admission = admission ?? throw new ArgumentNullException(nameof(admission));
        SpeechAmplitudeThreshold = speechAmplitudeThreshold;
    }

    /// <summary>Gemeinsam genutzte Zulassung. Dieser Wert gibt sie nicht frei.</summary>
    internal CaptureAttemptAdmission Admission { get; }

    internal int SpeechAmplitudeThreshold { get; }
}
