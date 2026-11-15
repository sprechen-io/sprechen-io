namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Ereigniskennungen für Reparaturen im Buchstabierablauf, damit dessen Meldungen dieselben Kennungen verwenden.
/// </summary>
internal static class SpellingWorkflowRepairEventCodes
{
    public const string UnrecognizedCharacters = "spelling_unrecognized_characters";
    public const string NoValue = "spelling_no_value";
    public const string DeleteLastCharacter = "spelling_delete_last_character";
    public const string DeleteLastBlock = "spelling_delete_last_block";
    public const string ClearBuffer = "spelling_clear_buffer";
    public const string ReadbackCharacters = "spelling_readback_characters";
}
