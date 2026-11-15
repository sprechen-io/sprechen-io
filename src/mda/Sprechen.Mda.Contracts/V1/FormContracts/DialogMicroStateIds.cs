namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Gemeinsame Kennungen für die Zustände innerhalb einer Feldinteraktion.
/// </summary>
public static class DialogMicroStateIds
{
    public const string Prompting = "PROMPTING";
    public const string Listening = "LISTENING";
    public const string Interpreting = "INTERPRETING";
    public const string Validating = "VALIDATING";
    public const string Confirmation = "CONFIRMATION";
    public const string ErrorPrompt = "ERROR_PROMPT";
    public const string Repair = "REPAIR";
}
