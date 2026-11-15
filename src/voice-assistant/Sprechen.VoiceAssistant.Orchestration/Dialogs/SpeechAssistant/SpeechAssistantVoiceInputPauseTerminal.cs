using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Behandelt das vereinbarte Pausensignal als erwartetes Ende eines Dialogeffekts, während andere
/// Fehler und der Auftragsabbruch weitergegeben werden.
/// </summary>
internal static class SpeechAssistantVoiceInputPauseTerminal
{
    internal static async Task CompleteExpectedAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await operation(cancellationToken).ConfigureAwait(false);
        }
        catch (VoiceInputPausedException)
        {
            // Das vereinbarte Pausensignal beendet diesen Effekt erwartungsgemäß. Die
            // Zustandsumstellung gehört zur Assistentensteuerung.
        }
    }
}
