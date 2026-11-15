namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Kennzeichnet einen unzulässigen Auswahlzustand oder Übergang, damit widersprüchliche Kandidaten-,
/// Revisions- und Phasenangaben nicht weiterverarbeitet werden.
/// </summary>
public sealed class SelectionCandidateStateException : Exception
{
    public SelectionCandidateStateException(string message)
        : base(message)
    {
    }
}
