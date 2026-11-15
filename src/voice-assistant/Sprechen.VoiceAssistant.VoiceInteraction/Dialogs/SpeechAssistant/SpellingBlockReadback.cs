namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert die phonetische Wiedergabe eines Buchstabierblocks oder des gesamten
/// Werts nach Datenschutzfreigabe an. Ein gesperrter Block bleibt ohne Wiedergabe.
/// Bei gesperrter Gesamtwertabfrage wird stattdessen ein inhaltsfreier Hinweis angefordert.
/// </summary>
internal sealed class SpellingBlockReadback
{
    // Für die phonetische Wiedergabe den Ratenfaktor anwenden und die Rate nach
    // unten begrenzen. Eine bereits unter der Grenze liegende Rate kann dadurch steigen.
    private const double ReadbackSpeechRateFactor = 0.85;
    private const double MinReadbackSpeechRate = 0.5;

    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly IFormDialogPrivacyReadbackPolicy _privacyReadbackPolicy;
    private readonly IPhoneticSpellOutRenderer _phoneticSpellOutRenderer;

    public SpellingBlockReadback(
        IFormVoiceCommandSpeechService speechService,
        IFormDialogPrivacyReadbackPolicy privacyReadbackPolicy,
        IPhoneticSpellOutRenderer phoneticSpellOutRenderer)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _privacyReadbackPolicy = privacyReadbackPolicy ?? throw new ArgumentNullException(nameof(privacyReadbackPolicy));
        _phoneticSpellOutRenderer = phoneticSpellOutRenderer ?? throw new ArgumentNullException(nameof(phoneticSpellOutRenderer));
    }

    // Nur den übergebenen Block nach Datenschutzfreigabe phonetisch vorlesen.
    // Ohne Freigabe hier keine ersatzweise Rückmeldung anfordern.
    public async Task ReadBackBlockAsync(
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        string block,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(block))
        {
            return;
        }

        if (!await IsSpellOutAllowedAsync(field).ConfigureAwait(false))
        {
            return;
        }

        var phonetic = RenderPhonetic(block, context.IsoCode);
        if (string.IsNullOrWhiteSpace(phonetic))
        {
            return;
        }

        await _speechService
            .SpeakSafeAsync(phonetic, context.IsoCode, SlowedForReadback(context.VoiceSettings), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task ReadBackWholeValueAsync(
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        string value,
        string spokenCharactersFallback,
        CancellationToken cancellationToken)
    {
        if (!await IsSpellOutAllowedAsync(field).ConfigureAwait(false))
        {
            await _speechService
                .SpeakSafeAsync(
                    FormSpellingWorkflowPromptBuilder.GetSpellingReadbackUnavailablePrompt(
                        context.Contract,
                        context.IsoCode,
                        context.VoiceSettings.GetPolitenessLevel()),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var phonetic = RenderPhonetic(value, context.IsoCode);
        await _speechService
            .SpeakSafeAsync(
                FormSpellingWorkflowPromptBuilder.GetSpellingCurrentReadbackPrompt(
                    context.Contract,
                    context.IsoCode,
                    string.IsNullOrWhiteSpace(phonetic) ? spokenCharactersFallback : phonetic,
                    context.VoiceSettings.GetPolitenessLevel()),
                context.IsoCode,
                SlowedForReadback(context.VoiceSettings),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static VoiceDialogSettings SlowedForReadback(VoiceDialogSettings voiceSettings)
        => VoiceDialogSettingsRuntimeOverrides.ApplySpeechRateOverride(
            voiceSettings,
            Math.Max(MinReadbackSpeechRate, voiceSettings.GetSpeechRate() * ReadbackSpeechRateFactor));

    private Task<bool> IsSpellOutAllowedAsync(FormFieldSnapshot field)
        => _privacyReadbackPolicy.IsSpellOutAllowedInConfirmationAsync(field.VariableId);

    private string RenderPhonetic(string value, string isoCode)
        => _phoneticSpellOutRenderer.Render(value, isoCode);
}
