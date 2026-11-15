using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Spricht feldbezogene Hilfen, Formularorientierung und modellierte Ansagen im Formulardialog.
/// </summary>
public interface IFormVoiceCommandPromptService
{
    Task SpeakHelpForFocusedFieldAsync(
        IFormDialogRuntime interpreter,
        IFormVoiceCommandDialogStateTracker stateTracker,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken);

    Task SpeakSharedVariableAnnouncementAsync(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormContract contract,
        ImmutableArray<(string VariableId, string? SourceFormId)> adoptions,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken);

    Task SpeakPromptIdsAsync(
        FormContract contract,
        ImmutableArray<string> promptIds,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken);

    Task SpeakFormOrientationAsync(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormContract contract,
        IFormDialogRuntime interpreter,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken);
}


