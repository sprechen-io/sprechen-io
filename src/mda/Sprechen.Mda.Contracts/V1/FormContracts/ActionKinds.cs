namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Zulässige Arten von Regelaktionen gemäß form-contract.schema.json.
/// </summary>
public static class ActionKinds
{
    public const string SetVisible = "setVisible";
    public const string SetEnabled = "setEnabled";
    public const string SetReadOnly = "setReadOnly";
    public const string SetValue = "setValue";
    public const string ClearValue = "clearValue";
    public const string CopyValue = "copyValue";
    public const string NavigateToFieldGroup = "navigateToFieldGroup";
    public const string FocusField = "focusField";
    public const string Speak = "speak";
    public const string CustomAction = "customAction";
}
