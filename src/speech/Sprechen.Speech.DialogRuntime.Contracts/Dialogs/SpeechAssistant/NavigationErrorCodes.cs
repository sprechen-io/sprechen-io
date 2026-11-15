namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt stabile Kennungen für Dialogfehler der Navigation sowie der Sprach- und
/// Höflichkeitswahl, damit Dialogsteuerung und Auswertung dieselben Fehlerfälle benennen.
/// </summary>
public static class NavigationErrorCodes
{
    public const string CannotMoveBack = "cannot_move_back";
    public const string LostContext = "lost_context";
    public const string JumpMissingArgument = "jump_missing_argument";
    public const string JumpTargetNotFound = "jump_target_not_found";
    public const string SingleFormOnly = "single_form_only";
    public const string FormNotFound = "form_not_found";
    public const string FormAlreadySelected = "form_already_selected";
    public const string FormPrerequisitesMissing = "form_prerequisites_missing";
    public const string LanguageMissingArgument = "language_missing_argument";
    public const string LanguageNotSupported = "language_not_supported";
    public const string LanguageAlreadySelected = "language_already_selected";
    public const string PolitenessUnavailable = "politeness_unavailable";
    public const string PolitenessMissingArgument = "politeness_missing_argument";
    public const string PolitenessNotSupported = "politeness_not_supported";
    public const string PolitenessAlreadySelected = "politeness_already_selected";
}
