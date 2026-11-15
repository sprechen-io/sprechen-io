namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Gemeinsame Auslöser für Übergänge zwischen den übergeordneten Dialogzuständen.
/// </summary>
public static class DialogMacroTransitionTriggers
{
    public const string IntroComplete = "introComplete";
    public const string AllFieldsProcessed = "allFieldsProcessed";
    public const string SummaryAccepted = "summaryAccepted";
    public const string SummaryRejected = "summaryRejected";
    public const string NavigateNext = "navigateNext";
    public const string NavigateBack = "navigateBack";
    public const string JumpToFieldGroup = "jumpToFieldGroup";
    public const string ProcessConfirmed = "processConfirmed";
    public const string ProcessRejected = "processRejected";
    public const string CancelConfirmed = "cancelConfirmed";
}
