using System.Globalization;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Liefert lokalisierte Texte für Navigation, Schaltflächen und Seitenbeschriftungen
/// aus dem gemeinsamen Textkatalog. Bei fehlender oder nicht unterstützter Sprache
/// wird Deutsch verwendet.
/// </summary>
public sealed class FormsChromeCopyResolver
{
    private readonly string _language;

    private FormsChromeCopyResolver(string language) => _language = language;

    /// <summary>
    /// Erstellt die Textauswahl für eine Sprachkennung ohne äußere Leerzeichen
    /// und unabhängig von Groß- und Kleinschreibung. Unbekannte Kennungen verwenden Deutsch.
    /// </summary>
    public static FormsChromeCopyResolver For(string? language)
        => new((language ?? string.Empty).Trim().ToLowerInvariant());

    private string Select(string copyId) => FormsChromeCopy.Select(copyId, _language);


    public string SkipToContent => Select(nameof(SkipToContent));
    public string Settings => Select(nameof(Settings));


    public string LabelLanguage => Select(nameof(LabelLanguage));
    public string LabelApplication => Select(nameof(LabelApplication));
    public string LabelForm => Select(nameof(LabelForm));
    public string ActionLoad => Select(nameof(ActionLoad));
    public string PlaceholderChoose => Select(nameof(PlaceholderChoose));
    public string PlaceholderSelect => Select(nameof(PlaceholderSelect));
    public string ExamplesLabel => Select(nameof(ExamplesLabel));
    public string ReferenceTitle => Select(nameof(ReferenceTitle));
    public string SelectionTitle => Select(nameof(SelectionTitle));
    public string SelectionHelp => Select(nameof(SelectionHelp));
    public string ReferenceHelp => Select(nameof(ReferenceHelp));
    public string ErrorLabel => Select(nameof(ErrorLabel));

    // Übersetzt bekannte Ladefehlercodes erst bei der Seitenausgabe.

    public string? ErrorMessageFor(string? errorCode) => errorCode switch
    {
        "CATALOG_LOAD_FAILED" => Select("ErrorCatalogLoadFailed"),
        "E_LOCALIZATION_STRICT_MISSING" => Select("ErrorFormLocalizationStrictMissing"),
        "FORM_CONTRACT_PREFLIGHT_FAILED" => Select("ErrorFormPreflightFailed"),
        HtmlProjectionProfileResolver.ErrorCodeUnsupportedProfile => Select("ErrorFormProjectionProfileUnsupported"),
        "FORM_CONTRACT_LOAD_FAILED" => Select("ErrorFormLoadFailed"),
        _ => null
    };
    // Text für ein schreibgeschütztes Feld ohne Wert, entsprechend
    // der Dialogphrase dialog.field.readOnly.emptyValue in der Sprachoberfläche.
    public string ReadOnlyEmptyValue => Select(nameof(ReadOnlyEmptyValue));
    public string StudyContextLabel => Select(nameof(StudyContextLabel));
    public string StudyParticipantLabel => Select(nameof(StudyParticipantLabel));
    public string StudyTaskLabel => Select(nameof(StudyTaskLabel));
    public string StudyConditionLabel => Select(nameof(StudyConditionLabel));
    public string StudyVariantLabel => Select(nameof(StudyVariantLabel));


    public string NavLabel => Select(nameof(NavLabel));
    public string ProgressLabel => Select(nameof(ProgressLabel));
    public string NavStep(int currentFormNumber, int totalForms) => FormatInvariant(
        FormsChromeCopy.Select($"{nameof(NavStep)}Template", _language),
        currentFormNumber,
        totalForms);
    public string NavBack => Select(nameof(NavBack));
    public string NavCancel => Select(nameof(NavCancel));
    public string NavNext => Select(nameof(NavNext));
    public string NavSubmit => Select(nameof(NavSubmit));


    public string LanguageLabel => Select(nameof(LanguageLabel));
    public string SaveLabel => Select(nameof(SaveLabel));
    public string BackLabel => Select(nameof(BackLabel));


    public string ThemeLabel => Select(nameof(ThemeLabel));
    public string ThemeSystem => Select(nameof(ThemeSystem));
    public string ThemeLight => Select(nameof(ThemeLight));
    public string ThemeDark => Select(nameof(ThemeDark));

    public string ThemeOptionLabel(string value) => value switch
    {
        FormsThemePreference.Light => ThemeLight,
        FormsThemePreference.Dark => ThemeDark,
        _ => ThemeSystem,
    };

    private static string FormatInvariant(string template, int currentFormNumber, int totalForms)
        => string.Format(CultureInfo.InvariantCulture, template, currentFormNumber, totalForms);
}
