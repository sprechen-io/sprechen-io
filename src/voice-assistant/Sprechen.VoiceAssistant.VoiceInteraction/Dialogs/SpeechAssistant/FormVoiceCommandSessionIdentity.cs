using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Anwendung, Formular, Sprache und Aktivierungswörter zur
/// Zuordnung einer sprachgesteuerten Formularsitzung.
/// </summary>
public sealed record FormVoiceCommandSessionIdentity(
    string IsoCode,
    string ApplicationId,
    string? FormId,
    ImmutableArray<string> WakeWordActivationForms)
{
    public ImmutableArray<string> WakeWordRecognitionOnlyActivationForms { get; init; } = [];
}
