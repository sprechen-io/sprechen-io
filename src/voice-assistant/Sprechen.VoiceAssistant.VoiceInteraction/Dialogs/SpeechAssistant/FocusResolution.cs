using Sprechen.Contracts.FormSurfaces;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Enthält das nächste fokussierte Formularfeld oder das abschließende
/// Sitzungsergebnis für die weitere Feldverarbeitung.
/// </summary>
internal sealed record FocusResolution(
    FormFieldSnapshot? Field,
    FormVoiceCommandSessionRunResult? Terminal);
