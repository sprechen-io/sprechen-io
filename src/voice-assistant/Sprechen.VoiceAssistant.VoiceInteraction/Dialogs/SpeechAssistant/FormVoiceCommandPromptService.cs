using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt die gemeinsamen Ansagewege des Formulardialogs bereit, damit dessen Aufrufer die passenden Inhalte über denselben Dienst anfordern.
/// </summary>
internal sealed class FormVoiceCommandPromptService : IFormVoiceCommandPromptService
{
    private readonly FormVoiceCommandFocusedFieldHelpSpeaker _focusedFieldHelpSpeaker;
    private readonly IFormVoiceCommandSharedVariableAnnouncementService _sharedVariableAnnouncementService;
    private readonly FormVoiceCommandPromptIdSpeaker _promptIdSpeaker;
    private readonly FormVoiceCommandFormOrientationSpeaker _formOrientationSpeaker;

    internal FormVoiceCommandPromptService(
        IFormVoiceCommandSpeechService speechService,
        IFormVoiceCommandSharedVariableAnnouncementService sharedVariableAnnouncementService)
        : this(
            new FormVoiceCommandFocusedFieldHelpSpeaker(speechService),
            sharedVariableAnnouncementService,
            new FormVoiceCommandPromptIdSpeaker(
                speechService,
                new FormVoiceCommandPromptTextResolver()),
            new FormVoiceCommandFormOrientationSpeaker(speechService))
    {
    }

    public FormVoiceCommandPromptService(
        FormVoiceCommandFocusedFieldHelpSpeaker focusedFieldHelpSpeaker,
        IFormVoiceCommandSharedVariableAnnouncementService sharedVariableAnnouncementService,
        FormVoiceCommandPromptIdSpeaker promptIdSpeaker,
        FormVoiceCommandFormOrientationSpeaker formOrientationSpeaker)
    {
        _focusedFieldHelpSpeaker = focusedFieldHelpSpeaker ?? throw new ArgumentNullException(nameof(focusedFieldHelpSpeaker));
        _sharedVariableAnnouncementService = sharedVariableAnnouncementService
            ?? throw new ArgumentNullException(nameof(sharedVariableAnnouncementService));
        _promptIdSpeaker = promptIdSpeaker ?? throw new ArgumentNullException(nameof(promptIdSpeaker));
        _formOrientationSpeaker = formOrientationSpeaker ?? throw new ArgumentNullException(nameof(formOrientationSpeaker));
    }

    public async Task SpeakHelpForFocusedFieldAsync(
        IFormDialogRuntime interpreter,
        IFormVoiceCommandDialogStateTracker stateTracker,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        await _focusedFieldHelpSpeaker
            .SpeakAsync(interpreter, stateTracker, isoCode, voiceSettings, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SpeakSharedVariableAnnouncementAsync(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormContract contract,
        ImmutableArray<(string VariableId, string? SourceFormId)> adoptions,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        await _sharedVariableAnnouncementService
            .SpeakSharedVariableAnnouncementAsync(
                application,
                currentForm,
                contract,
                adoptions,
                isoCode,
                voiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SpeakPromptIdsAsync(
        FormContract contract,
        ImmutableArray<string> promptIds,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        await _promptIdSpeaker
            .SpeakAsync(contract, promptIds, isoCode, voiceSettings, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SpeakFormOrientationAsync(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormContract contract,
        IFormDialogRuntime interpreter,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        await _formOrientationSpeaker
            .SpeakAsync(application, currentForm, contract, interpreter, isoCode, voiceSettings, cancellationToken)
            .ConfigureAwait(false);
    }

}
