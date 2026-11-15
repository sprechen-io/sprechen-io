namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Gemeinsame Auslöser für Zustandsübergänge innerhalb einer Feldinteraktion.
/// </summary>
public static class DialogMicroTransitionTriggers
{
    public const string PromptComplete = "promptComplete";
    public const string SpeechReceived = "speechReceived";
    public const string NoInputTimeout = "noInputTimeout";
    public const string InterpretationComplete = "interpretationComplete";
    public const string ValidationPassed = "validationPassed";
    public const string ValidationFailed = "validationFailed";
    public const string UserConfirmed = "userConfirmed";
    public const string UserRejected = "userRejected";
    public const string ErrorPromptComplete = "errorPromptComplete";
    public const string RepairComplete = "repairComplete";
    public const string MaxRetriesExceeded = "maxRetriesExceeded";
}
