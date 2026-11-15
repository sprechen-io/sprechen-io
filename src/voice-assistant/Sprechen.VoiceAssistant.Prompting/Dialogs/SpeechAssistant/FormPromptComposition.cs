using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Auflösung der Ansagen mit Vorrang für Vertragsvorlagen und passende
/// Anredevarianten, damit die Dialogtexte dieselbe Reihenfolge für Ersatztexte nutzen.
/// </summary>
internal sealed class FormPromptComposition
{
    internal static FormPromptComposition Default { get; } = new();

    internal string JoinPromptItems(IEnumerable<string> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return string.Join(
            " ",
            items
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim()));
    }

    internal string NormalizeIsoCode(string isoCode)
        => IsoCode.TryNormalizeIso6391(isoCode) ?? VoiceInterfaceModelCatalog.DefaultLanguage;

    internal string ResolveFallbackPrompt(string isoCode, string promptId)
        => PromptTemplateFormatter.Apply(
            FormDialogPromptFallbackResolver.ResolveTemplate(NormalizeIsoCode(isoCode), promptId),
            templateParameters: null);

    internal string ResolveFallbackPrompt(
        string isoCode,
        string promptId,
        IReadOnlyDictionary<string, string?> templateParameters)
    {
        ArgumentNullException.ThrowIfNull(templateParameters);

        return PromptTemplateFormatter.Apply(
            FormDialogPromptFallbackResolver.ResolveTemplate(NormalizeIsoCode(isoCode), promptId),
            templateParameters);
    }

    internal string ResolvePromptOrFallback(
        FormContract contract,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fallback)
        => ResolvePromptOrFallback(contract, promptId, isoCode, politenessLevel, fallback, out _);

    // Diese Überladung liefert die Herkunft mit dem ausgewählten Text. Aufrufer, deren
    // Zusammensetzung von einer Vertragsvorlage abhängt, brauchen dadurch keine zweite Auflösung
    // und beurteilen die tatsächlich zurückgegebene Vorlage.
    internal string ResolvePromptOrFallback(
        FormContract contract,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fallback,
        out bool resolvedFromContractOverride)
    {
        ArgumentNullException.ThrowIfNull(contract);

        if (FormContractPromptResolver.TryResolvePrompt(
                contract,
                promptId,
                isoCode,
                politenessLevel,
                templateParameters: null,
                out var resolvedPrompt))
        {
            resolvedFromContractOverride = true;
            return resolvedPrompt;
        }

        resolvedFromContractOverride = false;
        return TryResolvePolitenessVariant(promptId, isoCode, politenessLevel, templateParameters: null, out var variant)
            ? variant
            : fallback;
    }

    internal string ResolvePromptOrFallback(
        FormContract contract,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fallback,
        IReadOnlyDictionary<string, string?> templateParameters)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(templateParameters);

        if (FormContractPromptResolver.TryResolvePrompt(
                contract,
                promptId,
                isoCode,
                politenessLevel,
                templateParameters,
                out var resolvedPrompt))
        {
            return resolvedPrompt;
        }

        return TryResolvePolitenessVariant(promptId, isoCode, politenessLevel, templateParameters, out var variant)
            ? variant
            : fallback;
    }

    internal string ResolveOrFallback(
        FormContract? contract,
        string promptId,
        string fallbackPromptId,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        if (contract is not null
            && FormContractPromptResolver.TryResolvePrompt(
                contract,
                promptId,
                isoCode,
                politenessLevel,
                templateParameters: null,
                out var resolvedPrompt))
        {
            return resolvedPrompt;
        }

        return TryResolvePolitenessVariant(promptId, isoCode, politenessLevel, templateParameters: null, out var variant)
            ? variant
            : ResolveFallbackPrompt(isoCode, fallbackPromptId);
    }

    internal string ResolveOrFallback(
        FormContract? contract,
        string promptId,
        string fallbackPromptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyDictionary<string, string?> templateParameters)
    {
        ArgumentNullException.ThrowIfNull(templateParameters);

        if (contract is not null
            && FormContractPromptResolver.TryResolvePrompt(
                contract,
                promptId,
                isoCode,
                politenessLevel,
                templateParameters,
                out var resolvedPrompt))
        {
            return resolvedPrompt;
        }

        return TryResolvePolitenessVariant(promptId, isoCode, politenessLevel, templateParameters, out var variant)
            ? variant
            : ResolveFallbackPrompt(isoCode, fallbackPromptId, templateParameters);
    }

    // Der eingebettete Katalog führt Anredevarianten unter Schlüsseln mit den Endungen informal und
    // formal. Ohne Vertragsüberschreibung wird eine vorhandene nichtleere Variante der ausgewählten
    // Anrede vor dem allgemeinen Ersatztext verwendet. Fehlt sie, wird der Ersatztext verwendet.
    private bool TryResolvePolitenessVariant(
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyDictionary<string, string?>? templateParameters,
        out string resolved)
    {
        resolved = string.Empty;
        var variantPromptId = FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(promptId, politenessLevel);
        if (!VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(variantPromptId, NormalizeIsoCode(isoCode), out var template))
        {
            return false;
        }

        resolved = PromptTemplateFormatter.Apply(template, templateParameters);
        return !string.IsNullOrWhiteSpace(resolved);
    }

    internal string ResolveOrFallback(
        FormContract? contract,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ResolveOrFallback(contract, promptId, promptId, isoCode, politenessLevel);

    internal string ResolveOrFallback(
        FormContract? contract,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyDictionary<string, string?> templateParameters)
        => ResolveOrFallback(contract, promptId, promptId, isoCode, politenessLevel, templateParameters);

    internal string ResolvePolitenessVariantPromptId(string promptId, PolitenessLevel politenessLevel)
        => FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(promptId, politenessLevel);
}
