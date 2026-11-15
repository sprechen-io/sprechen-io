using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Auflösung lesbarer Feld- und Formularnamen sowie der Formularwahl für
/// Dialogaufrufer. Die einzelnen Resolver besitzen die jeweilige Such- und Ersatzregel.
/// </summary>
public static class FormDialogTextResolver
{
    private static readonly FormFieldLabelResolver FieldLabelResolver = new();
    private static readonly FormSourceFormNameResolver SourceFormNameResolver = new();
    private static readonly FormCatalogFormResolver CatalogFormResolver = new();
    private static readonly FormArgumentFormResolver ArgumentFormResolver = new();

    /// <summary>
    /// Sucht eine lokalisierte Feldbeschriftung zur exakten Variablenkennung im Formularvertrag.
    /// </summary>
    public static string? TryResolveFieldLabel(FormContract contract, string variableId, string isoCode)
        => FieldLabelResolver.TryResolveFieldLabel(contract, variableId, isoCode);

    /// <summary>
    /// Ermittelt den lesbaren Namen eines referenzierten Formulars aus dem Anwendungskatalog.
    /// </summary>
    public static string ResolveSourceFormName(ApplicationDescriptor application, string sourceFormId, string isoCode)
        => SourceFormNameResolver.ResolveSourceFormName(application, sourceFormId, isoCode);

    /// <summary>
    /// Löst den Katalogtext für die gewünschte Sprache auf und verwendet ohne Anwendung den
    /// übergebenen Ersatztext.
    /// </summary>
    public static string ResolveCatalogText(ApplicationDescriptor application, LocalizedText? text, string isoCode, string fallback)
    {
        if (application is null)
        {
            return fallback;
        }

        return new FormsCatalogTextResolver().ResolveCatalogText(application, text, isoCode, fallback);
    }

    /// <summary>
    /// Sucht das Formular zur Kennung oder ohne Kennung den ersten geordneten Katalogeintrag.
    /// </summary>
    public static FormDescriptor? ResolveForm(ApplicationDescriptor application, string? formId)
        => CatalogFormResolver.ResolveForm(application, formId);

    /// <summary>
    /// Ordnet das Argument einer Listennummer oder einem passenden Formularnamen zu.
    /// </summary>
    public static bool TryResolveFormByArgument(
        ApplicationDescriptor application,
        IReadOnlyList<FormDescriptor> orderedForms,
        string isoCode,
        string argument,
        out FormDescriptor? form,
        out string? resolvedName)
        => ArgumentFormResolver.TryResolveFormByArgument(
            application,
            orderedForms,
            isoCode,
            argument,
            out form,
            out resolvedName);

}
