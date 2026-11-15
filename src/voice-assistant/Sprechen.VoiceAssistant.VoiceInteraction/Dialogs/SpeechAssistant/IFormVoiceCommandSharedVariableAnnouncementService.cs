using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt die Ansage übernommener gemeinsamer Angaben zu Beginn einer Formularsitzung mit Spracheingabe.
/// </summary>
public interface IFormVoiceCommandSharedVariableAnnouncementService
{
    Task SpeakSharedVariableAnnouncementAsync(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormContract contract,
        ImmutableArray<(string VariableId, string? SourceFormId)> adoptions,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken);
}

