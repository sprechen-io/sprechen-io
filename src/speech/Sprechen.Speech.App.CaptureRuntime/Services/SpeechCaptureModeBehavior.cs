namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Bündelt die Regeln eines Erfassungsmodus, darunter Aktivierungsprofil,
/// Kurzbestätigung, Fortsetzung und Unterbrechung einer Wiedergabe.
/// </summary>
internal readonly struct SpeechCaptureModeBehavior
{
    public static SpeechCaptureModeBehavior ActivationListening => For(SpeechCaptureMode.ActivationListening);

    public SpeechCaptureMode Mode { get; }
    public bool BargeInEnabled { get; }
    public bool UsesActivationProfile { get; }

    // Kurzbestätigungen verwenden eine kürzere Schutzfrist nach Wiedergabe,
    // damit weniger Audio kurzer Antworten entfällt. Die Factory ändert dabei
    // die Dauer zur Bestätigung eines Sprachbeginns nicht.
    public bool UsesShortConfirmationProfile { get; }
    public bool RequiresActivePlaybackContext { get; }
    public bool IsContinuationMode { get; }
    public string SpeechOnsetDescription { get; }

    private SpeechCaptureModeBehavior(
        SpeechCaptureMode mode,
        bool bargeInEnabled,
        bool usesActivationProfile,
        bool usesShortConfirmationProfile,
        bool requiresActivePlaybackContext,
        bool isContinuationMode,
        string speechOnsetDescription)
    {
        Mode = mode;
        BargeInEnabled = bargeInEnabled;
        UsesActivationProfile = usesActivationProfile;
        UsesShortConfirmationProfile = usesShortConfirmationProfile;
        RequiresActivePlaybackContext = requiresActivePlaybackContext;
        IsContinuationMode = isContinuationMode;
        SpeechOnsetDescription = speechOnsetDescription;
    }

    public static SpeechCaptureModeBehavior For(SpeechCaptureMode mode) => mode switch
    {
        SpeechCaptureMode.Listening => new(mode,
            bargeInEnabled: false,
            usesActivationProfile: false,
            usesShortConfirmationProfile: false,
            requiresActivePlaybackContext: false,
            isContinuationMode: false,
            speechOnsetDescription: "speech onset confirmed"),

        SpeechCaptureMode.ListeningContinuation => new(mode,
            bargeInEnabled: false,
            usesActivationProfile: false,
            usesShortConfirmationProfile: false,
            requiresActivePlaybackContext: false,
            isContinuationMode: true,
            speechOnsetDescription: "speech onset confirmed"),

        SpeechCaptureMode.ActivationListening => new(mode,
            bargeInEnabled: false,
            usesActivationProfile: true,
            usesShortConfirmationProfile: false,
            requiresActivePlaybackContext: false,
            isContinuationMode: false,
            speechOnsetDescription: "speech onset confirmed"),

        SpeechCaptureMode.ActivationListeningContinuation => new(mode,
            bargeInEnabled: false,
            usesActivationProfile: true,
            usesShortConfirmationProfile: false,
            requiresActivePlaybackContext: false,
            isContinuationMode: true,
            speechOnsetDescription: "speech onset confirmed"),

        SpeechCaptureMode.ShortConfirmation => new(mode,
            bargeInEnabled: false,
            usesActivationProfile: false,
            usesShortConfirmationProfile: true,
            requiresActivePlaybackContext: false,
            isContinuationMode: false,
            speechOnsetDescription: "speech onset confirmed"),

        SpeechCaptureMode.PromptInterruption => new(mode,
            bargeInEnabled: true,
            usesActivationProfile: false,
            usesShortConfirmationProfile: false,
            requiresActivePlaybackContext: true,
            isContinuationMode: false,
            speechOnsetDescription: "speech onset confirmed; requesting prompt interruption"),

        SpeechCaptureMode.PromptInterruptionDetection => new(mode,
            bargeInEnabled: false,
            usesActivationProfile: false,
            usesShortConfirmationProfile: false,
            requiresActivePlaybackContext: true,
            isContinuationMode: false,
            speechOnsetDescription: "speech onset confirmed for prompt interruption detection"),

        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown speech capture mode.")
    };
}
