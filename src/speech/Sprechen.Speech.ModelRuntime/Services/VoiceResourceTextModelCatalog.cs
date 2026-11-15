using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Löst Textressourcen und Ansagevorlagen aus dem Dialogmodell auf
/// und berücksichtigt bei Vorlagen Sprachstandard und Höflichkeitsvarianten.
/// </summary>
public sealed class VoiceResourceTextModelCatalog
{
    private readonly VoiceInterface _model;
    private readonly Lazy<IReadOnlyDictionary<string, LocalizedText>> _resourcesById;
    private readonly Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> _promptTemplatesByLanguage;

    internal VoiceResourceTextModelCatalog(VoiceInterface model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _resourcesById = new Lazy<IReadOnlyDictionary<string, LocalizedText>>(BuildResourcesById);
        _promptTemplatesByLanguage = new Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>>(BuildPromptTemplatesByLanguage);
    }

    public bool HasResource(string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        return _resourcesById.Value.ContainsKey(resourceId);
    }

    public bool TryGetResourceText(string resourceId, string isoCode, out string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        text = string.Empty;
        if (!_resourcesById.Value.TryGetValue(resourceId, out var texts))
        {
            return false;
        }

        if (!LocalizedTextResolver.TryResolve(
                texts,
                isoCode,
                _model.DefaultLanguage,
                _model.SupportedLanguages,
                _model.StrictLocalization,
                politenessLevel: PolitenessLevel.Formal,
                out var resolved)
            || string.IsNullOrWhiteSpace(resolved))
        {
            return false;
        }

        text = resolved.Trim();
        return true;
    }

    public bool TryGetPromptTemplate(string promptId, string isoCode, out string template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        template = string.Empty;
        var normalizedIsoCode = NormalizeIsoCode(isoCode) ?? _model.DefaultLanguage;
        if (TryGetPromptTemplateForLanguage(promptId, normalizedIsoCode, out template)
            || TryGetPromptTemplateForLanguage(promptId, _model.DefaultLanguage, out template))
        {
            return true;
        }

        return false;
    }

    public bool TryGetPromptTemplate(string promptId, string isoCode, PolitenessLevel politenessLevel, out string template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        // Der Ansagekatalog führt Höflichkeitsvarianten als Kennungen mit Suffix neben
        // der Grundkennung. Zuerst wird die gewählte Variante aufgelöst, danach die
        // Grundvorlage. Eine Kennung ohne Variante behält so ihre allgemeine Formulierung.
        var variantSuffix = politenessLevel == PolitenessLevel.Informal ? "informal" : "formal";
        if (TryGetPromptTemplate($"{promptId}.{variantSuffix}", isoCode, out template))
        {
            return true;
        }

        return TryGetPromptTemplate(promptId, isoCode, out template);
    }

    private IReadOnlyDictionary<string, LocalizedText> BuildResourcesById()
    {
        return _model.Resources.ToDictionary(
            resource => resource.Id,
            resource => resource.Texts,
            StringComparer.Ordinal);
    }

    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> BuildPromptTemplatesByLanguage()
    {
        return _model.PromptCatalog.PromptsByLanguage.ToDictionary(
            language => language.Key,
            language => (IReadOnlyDictionary<string, string>)language.Value
                .Where(prompt => !string.IsNullOrWhiteSpace(prompt.Key) && !string.IsNullOrWhiteSpace(prompt.Value))
                .ToDictionary(
                    prompt => prompt.Key,
                    prompt => prompt.Value.Trim(),
                    StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    private bool TryGetPromptTemplateForLanguage(string promptId, string isoCode, out string template)
    {
        template = string.Empty;
        if (!_promptTemplatesByLanguage.Value.TryGetValue(isoCode, out var promptsById)
            || !promptsById.TryGetValue(promptId, out var resolvedTemplate)
            || string.IsNullOrWhiteSpace(resolvedTemplate))
        {
            return false;
        }

        template = resolvedTemplate;
        return true;
    }

    private string? NormalizeIsoCode(string isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return null;
        }

        return IsoCode.TryNormalizeIso6391(isoCode);
    }
}
