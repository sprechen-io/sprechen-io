using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet die Auflösung übernommener gemeinsamer Angaben mit ihrer Ansage, damit der Formularstart deren Herkunft erläutern kann.
/// </summary>
internal sealed class FormVoiceCommandSharedVariableAnnouncementService
    : IFormVoiceCommandSharedVariableAnnouncementService
{
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly SharedVariableAnnouncementContentResolver _contentResolver;
    private readonly SharedVariableAnnouncementPromptResolver _promptResolver;

    public FormVoiceCommandSharedVariableAnnouncementService(
        IFormVoiceCommandSpeechService speechService,
        SharedVariableAnnouncementContentResolver contentResolver,
        SharedVariableAnnouncementPromptResolver promptResolver)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _contentResolver = contentResolver ?? throw new ArgumentNullException(nameof(contentResolver));
        _promptResolver = promptResolver ?? throw new ArgumentNullException(nameof(promptResolver));
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
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(currentForm);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(voiceSettings);

        if (adoptions.Length == 0)
        {
            return;
        }

        var content = _contentResolver.Resolve(
            application,
            contract,
            adoptions,
            isoCode,
            voiceSettings);
        var announcement = _promptResolver.Resolve(
            contract,
            isoCode,
            voiceSettings.GetPolitenessLevel(),
            content);

        await _speechService.SpeakSafeAsync(announcement, isoCode, voiceSettings, cancellationToken).ConfigureAwait(false);
    }
}

