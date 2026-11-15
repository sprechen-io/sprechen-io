namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Status eines zum Sprechen aufbereiteten Werts. Oberflächen müssen die einzelnen
/// Zustände unterscheiden und dürfen sie nicht auf <c>IncludesValue</c> oder <c>HasValue</c> reduzieren.
/// </summary>
public enum FormSpokenValueProjectionStatus
{
    FullValue,
    Empty,
    Masked,
    Withheld,
    Unavailable
}
