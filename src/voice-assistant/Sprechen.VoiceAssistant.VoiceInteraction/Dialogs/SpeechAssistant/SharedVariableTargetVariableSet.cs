using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bildet die Menge nicht leerer gemeinsamer Variablenkennungen des Zielformulars ohne zusätzliche Textbereinigung.
/// </summary>
internal sealed class SharedVariableTargetVariableSet
{
    public ImmutableArray<string> Resolve(FormDescriptor targetForm)
    {
        ArgumentNullException.ThrowIfNull(targetForm);

        return targetForm.SharedVariables is { Count: > 0 } sharedVariables
            ? sharedVariables
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToImmutableArray()
            : [];
    }
}
