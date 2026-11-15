using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Löst Bezeichnungen gemeinsamer Variablen aus passenden Feldern auf und entfernt wiederholte Bezeichnungen für die Herkunftsansage.
/// </summary>
internal sealed class SharedVariableAnnouncementLabelResolver
{
    public string[] ResolveLabels(
        FormContract contract,
        ImmutableArray<(string VariableId, string? SourceFormId)> adoptions,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(voiceSettings);

        return adoptions
            .Select(adoption => TryResolveFieldLabel(contract, adoption.VariableId, isoCode, voiceSettings))
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Select(label => label!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? TryResolveFieldLabel(
        FormContract contract,
        string variableId,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);

        var exactVariableId = variableId;

        foreach (var field in contract.Fields)
        {
            if (field is null)
            {
                continue;
            }

            if (!string.Equals(ResolveFieldVariableId(field), exactVariableId, StringComparison.Ordinal))
            {
                continue;
            }

            return ResolveLocalizedFieldLabel(contract, field, isoCode, voiceSettings) ?? field.FieldId;
        }

        return exactVariableId;
    }

    private static string ResolveFieldVariableId(FieldDefinition field)
        => field.VariableId == default
            ? field.FieldId
            : field.VariableId.Value;

    private static string? ResolveLocalizedFieldLabel(
        FormContract contract,
        FieldDefinition field,
        string isoCode,
        VoiceDialogSettings voiceSettings)
        => LocalizedTextResolver.TryResolve(
                field.Label,
                isoCode,
                contract.DefaultLanguage,
                contract.SupportedLanguages,
                contract.StrictLocalization,
                voiceSettings.GetPolitenessLevel(),
                out var resolved)
            && !string.IsNullOrWhiteSpace(resolved)
                ? resolved.Trim()
                : null;
}
