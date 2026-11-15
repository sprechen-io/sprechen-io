using Sprechen.Mda.Contracts.V1.SpeechCapture;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert die Prüfung von Mikrofonregel, Segmentierung
/// und Antwortfenstern des Sitzungseinstiegs.
/// </summary>
internal sealed class SpeechCaptureRuntimeSettingsValidator
{
    private readonly SpeechCaptureMicGateValidator _micGateValidator = new();
    private readonly SpeechCaptureSegmenterValidator _segmenterValidator = new();
    private readonly SpeechCaptureSessionEntryValidator _sessionEntryValidator = new();

    public void Validate(List<PreflightIssue> errors, SpeechCaptureProfileCatalog catalog)
    {
        _micGateValidator.Validate(errors, catalog.MicGate);
        _segmenterValidator.Validate(errors, catalog.Segmenter);
        _sessionEntryValidator.Validate(errors, catalog.SessionEntry);
    }
}
