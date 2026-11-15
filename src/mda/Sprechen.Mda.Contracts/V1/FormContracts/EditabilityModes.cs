namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Zulässige Regeln für die Bearbeitbarkeit von Feldern gemäß field-definitions.schema.json.
/// </summary>
public static class EditabilityModes
{
    public const string UserEditable = "userEditable";
    public const string ReadOnly = "readOnly";
    public const string Computed = "computed";
}
