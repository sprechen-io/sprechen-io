using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Formularargumente zuerst als Position und danach als Namen oder Kennung, damit die Auswahl eine feste Auflösungsreihenfolge erhält.
/// </summary>
internal sealed class FormVoiceCommandFormMatcher : IFormVoiceCommandFormMatcher
{
    private readonly FormVoiceCommandNumericFormArgumentMatcher _numericMatcher;
    private readonly FormVoiceCommandNamedFormArgumentMatcher _namedMatcher;

    internal FormVoiceCommandFormMatcher()
        : this(
            new FormVoiceCommandNumericFormArgumentMatcher(new FormVoiceCommandFormDisplayNameResolver()),
            new FormVoiceCommandNamedFormArgumentMatcher(new FormVoiceCommandFormDisplayNameResolver()))
    {
    }

    public FormVoiceCommandFormMatcher(
        FormVoiceCommandNumericFormArgumentMatcher numericMatcher,
        FormVoiceCommandNamedFormArgumentMatcher namedMatcher)
    {
        _numericMatcher = numericMatcher ?? throw new ArgumentNullException(nameof(numericMatcher));
        _namedMatcher = namedMatcher ?? throw new ArgumentNullException(nameof(namedMatcher));
    }

    public bool TryResolveFormByArgument(
        ApplicationDescriptor application,
        ImmutableArray<FormDescriptor> orderedForms,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        string argument,
        out FormDescriptor form,
        out string formName)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(voiceSettings);

        form = null!;
        formName = string.Empty;

        var trimmedArgument = argument.Trim();
        if (trimmedArgument.Length == 0)
        {
            return false;
        }

        return _numericMatcher.TryMatch(application, orderedForms, isoCode, voiceSettings, trimmedArgument, out form, out formName)
            || _namedMatcher.TryMatch(application, orderedForms, isoCode, voiceSettings, trimmedArgument, out form, out formName);
    }
}

