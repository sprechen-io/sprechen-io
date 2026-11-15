namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Zulässige Auslöser der Regelauswertung.
/// </summary>
public static class RuleScopes
{
    /// <summary>
    /// Auswertung bei der Initialisierung.
    /// </summary>
    public const string OnInit = "onInit";

    /// <summary>
    /// Auswertung bei Wertänderung.
    /// </summary>
    public const string OnValueChanged = "onValueChanged";

    /// <summary>
    /// Auswertung beim Betreten einer Feldgruppe.
    /// </summary>
    public const string OnEnterFieldGroup = "onEnterFieldGroup";

    /// <summary>
    /// Auswertung bei einem Versuch abzusenden.
    /// </summary>
    public const string OnSubmitAttempt = "onSubmitAttempt";
}
