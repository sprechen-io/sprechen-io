using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Löst eine positive Formularnummer als Position in der übergebenen Reihenfolge auf und ergänzt den gemeinsamen sprachbezogenen Anzeigenamen.
/// </summary>
internal sealed class FormVoiceCommandNumericFormArgumentMatcher
{
    private readonly FormVoiceCommandFormDisplayNameResolver _displayNameResolver;

    public FormVoiceCommandNumericFormArgumentMatcher(FormVoiceCommandFormDisplayNameResolver displayNameResolver)
    {
        _displayNameResolver = displayNameResolver ?? throw new ArgumentNullException(nameof(displayNameResolver));
    }

    public bool TryMatch(
        ApplicationDescriptor application,
        ImmutableArray<FormDescriptor> orderedForms,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        string trimmedArgument,
        out FormDescriptor form,
        out string formName)
    {
        form = null!;
        formName = string.Empty;
        if (!int.TryParse(trimmedArgument, out var number) || number <= 0 || number > orderedForms.Length)
        {
            return false;
        }

        form = orderedForms[number - 1];
        formName = _displayNameResolver.Resolve(application, form, isoCode, voiceSettings);
        return true;
    }
}
