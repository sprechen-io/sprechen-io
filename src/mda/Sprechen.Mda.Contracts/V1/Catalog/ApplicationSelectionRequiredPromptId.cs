namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Erforderliche Ansagekennungen für Wiederholung und erneute Eingabe
/// bei der Anwendungs- und Formularauswahl.
/// </summary>
public static class ApplicationSelectionRequiredPromptId
{
    /// <summary>Erinnerung bei ausbleibender Eingabe zur Bestätigung einer Anwendung.</summary>
    public const string AppConfirmationRepeat = "catalog.app.confirmation.repeat";

    /// <summary>Erinnerung bei ausbleibender Eingabe zur Bestätigung eines Formulars.</summary>
    public const string FormConfirmationRepeat = "catalog.form.confirmation.repeat";

    /// <summary>Ansage zur erneuten Eingabe, wenn die erkannte Anwendungsauswahl ohne andere Ansage fortgesetzt wird.</summary>
    public const string AppReListen = "catalog.app.reListen";

    /// <summary>Ansage zur erneuten Eingabe, wenn die erkannte Formularauswahl ohne andere Ansage fortgesetzt wird.</summary>
    public const string FormReListen = "catalog.form.reListen";

    /// <summary>Erforderliche Ansagekennungen. Groß- und Kleinschreibung müssen exakt übereinstimmen.</summary>
    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(
    [
        AppConfirmationRepeat,
        FormConfirmationRepeat,
        AppReListen,
        FormReListen
    ]);
}
