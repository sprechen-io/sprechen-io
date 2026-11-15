namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Bündelt Fehlerkategorien der Sprachverarbeitung für komponentenübergreifend auswertbare Rückmeldungen.
/// </summary>
public enum SpeechErrorCode
{
    Unspecified = 0,
    AuthMissingApiKey = 1,
    AuthInvalidApiKey = 2,
    TextRequired = 3,
    LanguageCodeRequired = 4,
    LanguageCodeInvalid = 5,
    LanguageCodeUnsupported = 6,
    AudioInputMimeTypeRequired = 7,
    AudioInputMimeTypeUnsupported = 8,
    AudioOutputMimeTypeRequired = 9,
    AudioOutputMimeTypeUnsupported = 10,
    ProviderFailure = 11,
    ProviderRateLimited = 12,
    VoiceConfigurationInvalidRequest = 13,
    VoiceConfigurationUnsupportedVersion = 14,
    VoiceConfigurationInvalidConfiguration = 15,
    NoSpeechDetected = 16,
    ApplicationCatalogDialogFailed = 17,
    FormSelectionDialogFailed = 18,
    VoiceCommandDialogFailed = 19,
    VoiceCommandLanguageChangeFailed = 20,
    VoiceCommandPolitenessChangeFailed = 21,
    MicrophonePermissionDenied = 22,
    MicrophoneUnavailable = 23,
    SpeechKeytermsInvalid = 24,
    FormRuleLoopGuardExceeded = 25
}
