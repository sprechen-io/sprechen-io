namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die Reparaturkennungen des Ziffernablaufs für dessen Telemetrieaufrufe zusammen, damit die Ereignisse einheitlich benannt werden.
/// </summary>
internal static class DigitsWorkflowRepairEventCodes
{
    public const string UnrecognizedBlock = "digits_unrecognized_block";
    public const string NoValue = "digits_no_value";
    public const string DeleteLastDigit = "digits_delete_last_digit";
    public const string DeleteLastBlock = "digits_delete_last_block";
    public const string ReadbackDigits = "digits_readback";
    public const string RedictateBlock = "digits_redictate_block";
}
