namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Erkennt die für aktive Ansagen vorgesehenen Mikrozustände anhand ihrer exakten Kennungen, damit Ansagewege dieselbe Einordnung verwenden.
/// </summary>
internal static class DialogMicroStatePromptGate
{
    private static readonly HashSet<string> ActivePromptMicroStates = new(StringComparer.Ordinal)
    {
        DialogMicroStateIds.Prompting,
        DialogMicroStateIds.ErrorPrompt,
        DialogMicroStateIds.Repair
    };

    internal static bool IsActivePromptState(string? microState)
        => microState is not null && ActivePromptMicroStates.Contains(microState);
}
