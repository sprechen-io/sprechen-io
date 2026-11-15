using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt die Auflösung eines gesprochenen Formulararguments gegen den
/// verfügbaren Formularbestand der Anwendung.
/// </summary>
public interface IFormVoiceCommandFormMatcher
{
    bool TryResolveFormByArgument(
        ApplicationDescriptor application,
        ImmutableArray<FormDescriptor> orderedForms,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        string argument,
        out FormDescriptor form,
        out string formName);
}

