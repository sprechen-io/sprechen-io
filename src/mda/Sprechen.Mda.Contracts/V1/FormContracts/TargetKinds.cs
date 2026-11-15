namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Zulässige Zielarten für Validierungsregeln und Aktionen gemäß constraint-language.schema.json.
/// </summary>
public static class TargetKinds
{
    /// <summary>
    /// field: Bezug auf ein einzelnes Feld.
    /// </summary>
    public const string Field = "field";

    /// <summary>
    /// variable: Bezug auf eine Variable.
    /// </summary>
    public const string Variable = "variable";

    /// <summary>
    /// form: Bezug auf das gesamte Formular.
    /// </summary>
    public const string Form = "form";

    /// <summary>
    /// fieldGroup: Bezug auf eine Feldgruppe.
    /// </summary>
    public const string FieldGroup = "fieldGroup";
}
