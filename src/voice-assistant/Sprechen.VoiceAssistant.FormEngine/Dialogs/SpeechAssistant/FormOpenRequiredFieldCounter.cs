using System.Collections.Immutable;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Zählt offene Pflichtfelder anhand derselben Auswahl wie die Navigation.
/// </summary>
internal sealed class FormOpenRequiredFieldCounter
{
    private readonly FormOpenRequiredFieldSelector selector;

    public FormOpenRequiredFieldCounter(FormOpenRequiredFieldSelector selector)
    {
        this.selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    public int CountOpenRequiredFields(
        IReadOnlyDictionary<string, string> valuesByVariableId,
        ImmutableArray<FormConstraintViolation> violations,
        Func<string, bool> isFieldEffectivelyVisible)
        => selector
            .SelectOpenRequiredFieldIds(valuesByVariableId, violations, isFieldEffectivelyVisible)
            .Length;
}
