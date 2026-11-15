namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Zulässige Geltungsbereiche von Validierungsregeln.
/// </summary>
public static class ConstraintScopes
{
    /// <summary>
    /// Feldbezogene Validierungsregel.
    /// </summary>
    public const string Field = "field";

    /// <summary>
    /// Feldgruppenbezogene Validierungsregel.
    /// </summary>
    public const string FieldGroup = "fieldGroup";

    /// <summary>
    /// Feldgruppenübergreifende Validierungsregel.
    /// </summary>
    public const string CrossGroup = "crossGroup";

    /// <summary>
    /// Formularbezogene Validierungsregel.
    /// </summary>
    public const string Form = "form";
}
