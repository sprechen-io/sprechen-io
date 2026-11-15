using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Löst lokalisierte Vertragstexte über den gemeinsamen Sprachauflöser auf, damit
/// Formularprojektionen dieselben Sprachregeln verwenden.
/// </summary>
internal sealed class FormLocalizationResolver
{
    private readonly string _requestedIsoCode;
    private readonly string? _defaultLanguage;
    private readonly ImmutableArray<string> _supportedLanguages;
    private readonly bool _strictLocalization;
    private readonly PolitenessLevel _politenessLevel;
    private readonly IReadOnlyDictionary<FieldGroupId, FieldGroupDefinition> _fieldGroupsById;
    private readonly FormChoiceOptionsResolver _choiceOptionsResolver;

    public FormLocalizationResolver(
        string requestedIsoCode,
        string? defaultLanguage,
        IEnumerable<string> supportedLanguages,
        bool strictLocalization,
        PolitenessLevel politenessLevel,
        IReadOnlyDictionary<VariableId, VariableDefinition> variablesById,
        IReadOnlyDictionary<FieldGroupId, FieldGroupDefinition> fieldGroupsById)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedIsoCode);
        ArgumentNullException.ThrowIfNull(supportedLanguages);
        ArgumentNullException.ThrowIfNull(variablesById);
        ArgumentNullException.ThrowIfNull(fieldGroupsById);

        _requestedIsoCode = requestedIsoCode.Trim();
        _defaultLanguage = defaultLanguage;
        _supportedLanguages = supportedLanguages.ToImmutableArray();
        _strictLocalization = strictLocalization;
        _politenessLevel = politenessLevel;
        _fieldGroupsById = fieldGroupsById;
        _choiceOptionsResolver = new FormChoiceOptionsResolver(variablesById, ResolveText);
    }

    /// <summary>
    /// Übergibt den lokalisierten Text und die gewünschte Sprache an den gemeinsamen
    /// Auflöser. Dessen Sprachregeln bestimmen, ob eine Ersatzauflösung zulässig ist.
    /// </summary>
    public string ResolveText(LocalizedText? text, string? fallback)
    {
        if (text is null)
        {
            return fallback ?? string.Empty;
        }

        if (LocalizedTextResolver.TryResolve(
                text,
                _requestedIsoCode,
                _defaultLanguage,
                _supportedLanguages,
                _strictLocalization,
                _politenessLevel,
                out var resolved)
            && !string.IsNullOrWhiteSpace(resolved))
        {
            return resolved.Trim();
        }

        return fallback ?? string.Empty;
    }

    public string ResolveFieldGroupTitle(string fieldGroupId)
    {
        if (!_fieldGroupsById.TryGetValue(FieldGroupId.From(fieldGroupId), out var fieldGroup))
        {
            return fieldGroupId;
        }

        return ResolveText(fieldGroup.Title, fallback: fieldGroupId);
    }

    /// <summary>
    /// Projiziert vorhandene Auswahloptionen mit ihren lokalisierten Bezeichnungen.
    /// Der Werttyp der Variablen wird an diesem Einstieg nicht eigens geprüft.
    /// </summary>
    public ImmutableArray<FormChoiceOption> ResolveChoiceOptions(string variableId)
        => _choiceOptionsResolver.ResolveChoiceOptions(variableId);
}
