using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Sucht zur exakten Variablenkennung eine verfügbare lokalisierte Feldbeschriftung, damit Dialoge das
/// Feld mit seinem lesbaren Namen ansprechen können.
/// </summary>
internal sealed class FormFieldLabelResolver
{
    private readonly FormLocalizedTextValueResolver localizedTextValueResolver;

    public FormFieldLabelResolver()
        : this(new FormLocalizedTextValueResolver())
    {
    }

    public FormFieldLabelResolver(FormLocalizedTextValueResolver localizedTextValueResolver)
    {
        this.localizedTextValueResolver = localizedTextValueResolver;
    }

    public string? TryResolveFieldLabel(FormContract contract, string variableId, string isoCode)
    {
        if (contract is null || string.IsNullOrWhiteSpace(variableId) || string.IsNullOrWhiteSpace(isoCode))
        {
            return null;
        }

        var exactVariableId = variableId;
        var trimmedIsoCode = isoCode.Trim();

        foreach (var field in contract.Fields)
        {
            if (!IsMatchingVariable(field, exactVariableId))
            {
                continue;
            }

            var label = localizedTextValueResolver.Resolve(field.Label, trimmedIsoCode, contract.DefaultLanguage);
            if (!string.IsNullOrWhiteSpace(label))
            {
                return label.Trim();
            }
        }

        return null;
    }

    private static bool IsMatchingVariable(FieldDefinition? field, string variableId)
    {
        if (field is null || field.VariableId == default)
        {
            return false;
        }

        return string.Equals(field.VariableId.Value, variableId, StringComparison.Ordinal);
    }
}
