using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Wählt Katalogtexte anhand von Sprache und Anrede und nutzt bei Bedarf Ersatztexte.
/// </summary>
public sealed class FormsCatalogTextResolver
{
    /// <summary>
    /// Wählt den Katalogtext für die wirksame Anrede der Anwendung.
    /// </summary>
    public string ResolveCatalogText(
        ApplicationDescriptor application,
        LocalizedText? text,
        string languageCode,
        string fallback)
    {
        ArgumentNullException.ThrowIfNull(application);

        return ResolveCatalogText(application, text, languageCode, fallback, ResolvePolitenessLevel(application));
    }

    /// <summary>
    /// Wählt den Katalogtext für die angegebene Anrede oder verwendet den Ersatztext.
    /// </summary>
    public string ResolveCatalogText(
        ApplicationDescriptor application,
        LocalizedText? text,
        string languageCode,
        string fallback,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(application);

        return TryResolveCatalogText(application, text, languageCode, politenessLevel, out var resolved)
            ? resolved
            : NormalizeFallback(fallback);
    }

    /// <summary>
    /// Versucht, den Katalogtext für die angegebene Anrede deterministisch aufzulösen.
    /// </summary>
    public bool TryResolveCatalogText(
        ApplicationDescriptor application,
        LocalizedText? text,
        string languageCode,
        PolitenessLevel politenessLevel,
        out string resolved)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        resolved = string.Empty;
        if (text is null || text.Count == 0)
        {
            return false;
        }

        if (!LocalizedTextResolver.TryResolve(
                text,
                languageCode,
                application.DefaultLanguage,
                application.SupportedLanguages,
                strictLocalization: false,
                politenessLevel,
                out var localized)
            || string.IsNullOrWhiteSpace(localized))
        {
            return false;
        }

        resolved = localized.Trim();
        return true;
    }

    public string ResolveApplicationDisplayName(ApplicationDescriptor application, string languageCode)
    {
        ArgumentNullException.ThrowIfNull(application);

        return ResolveApplicationDisplayName(application, languageCode, ResolvePolitenessLevel(application));
    }

    public string ResolveApplicationDisplayName(
        ApplicationDescriptor application,
        string languageCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(application);

        return ResolveCatalogText(
            application,
            application.DisplayName,
            languageCode,
            application.ApplicationId,
            politenessLevel);
    }

    /// <summary>
    /// Wählt die übersetzte Anwendungsbeschreibung für die wirksame Anrede.
    /// </summary>
    public string? ResolveApplicationDescription(ApplicationDescriptor application, string languageCode)
    {
        ArgumentNullException.ThrowIfNull(application);

        return ResolveApplicationDescription(application, languageCode, ResolvePolitenessLevel(application));
    }

    /// <summary>
    /// Wählt die übersetzte Anwendungsbeschreibung für die angegebene Anrede.
    /// </summary>
    public string? ResolveApplicationDescription(
        ApplicationDescriptor application,
        string languageCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(application);

        return TryResolveCatalogText(application, application.Description, languageCode, politenessLevel, out var resolved)
            ? resolved
            : null;
    }

    public string ResolveFormDisplayName(
        ApplicationDescriptor application,
        FormDescriptor form,
        string languageCode)
    {
        ArgumentNullException.ThrowIfNull(application);

        return ResolveFormDisplayName(application, form, languageCode, ResolvePolitenessLevel(application));
    }

    public string ResolveFormDisplayName(
        ApplicationDescriptor application,
        FormDescriptor form,
        string languageCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(form);

        return ResolveCatalogText(
            application,
            form.DisplayName,
            languageCode,
            form.FormId,
            politenessLevel);
    }

    private static string NormalizeFallback(string fallback)
        => string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();

    private static PolitenessLevel ResolvePolitenessLevel(ApplicationDescriptor application)
        => application.VoiceDialogSettings?.GetPolitenessLevel() ?? PolitenessLevel.Formal;
}
