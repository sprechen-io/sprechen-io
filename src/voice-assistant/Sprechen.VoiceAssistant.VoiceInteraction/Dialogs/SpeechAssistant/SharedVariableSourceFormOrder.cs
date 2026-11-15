using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet vorausgesetzte Quellen vor den übrigen Formularen ein.
/// Wiederholte Voraussetzungen werden entfernt. Das Zielformular und bereits
/// als Voraussetzung genannte Formulare fehlen in der anschließenden Liste.
/// </summary>
internal sealed class SharedVariableSourceFormOrder
{
    public ImmutableArray<string> Resolve(
        ApplicationDescriptor application,
        FormDescriptor targetForm)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(targetForm);

        var orderedForms = new FormsCatalogFormOrder().OrderForms(application)
            .Select(form => form.FormId.Value.Trim())
            .ToImmutableArray();

        var prerequisiteFormIds = (targetForm.PrerequisiteForms ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        var remainingFormIds = orderedForms
            .Where(formId =>
                !string.Equals(formId, targetForm.FormId, StringComparison.OrdinalIgnoreCase)
                && !prerequisiteFormIds.Contains(formId, StringComparer.OrdinalIgnoreCase))
            .ToImmutableArray();

        return prerequisiteFormIds.AddRange(remainingFormIds);
    }
}
