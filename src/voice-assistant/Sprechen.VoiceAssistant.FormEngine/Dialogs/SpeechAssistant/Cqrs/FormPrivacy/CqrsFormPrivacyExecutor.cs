using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Bündelt die Datenschutzabfragen für Spracheingabe und Bestätigungen, damit beide dieselben Regeln verwenden.
/// </summary>
internal sealed class CqrsFormPrivacyExecutor : ICqrsFormPrivacyExecutor
{
    private readonly IsVoiceInputAllowedQueryHandler _isVoiceInputAllowedHandler;
    private readonly ShouldOfferSilentInputOptionQueryHandler _shouldOfferSilentInputOptionHandler;
    private readonly RequiresReadbackOptInQueryHandler _requiresReadbackOptInHandler;
    private readonly CanSpeakValueInConfirmationQueryHandler _canSpeakValueInConfirmationHandler;
    private readonly IsSpellOutAllowedInConfirmationQueryHandler _isSpellOutAllowedInConfirmationHandler;
    private readonly BuildMaskedReadbackQueryHandler _buildMaskedReadbackHandler;

    public CqrsFormPrivacyExecutor(FormPrivacyReadbackPolicy privacyReadbackPolicy)
    {
        ArgumentNullException.ThrowIfNull(privacyReadbackPolicy);

        _isVoiceInputAllowedHandler = new IsVoiceInputAllowedQueryHandler(privacyReadbackPolicy);
        _shouldOfferSilentInputOptionHandler = new ShouldOfferSilentInputOptionQueryHandler(privacyReadbackPolicy);
        _requiresReadbackOptInHandler = new RequiresReadbackOptInQueryHandler(privacyReadbackPolicy);
        _canSpeakValueInConfirmationHandler = new CanSpeakValueInConfirmationQueryHandler(privacyReadbackPolicy);
        _isSpellOutAllowedInConfirmationHandler = new IsSpellOutAllowedInConfirmationQueryHandler(privacyReadbackPolicy);
        _buildMaskedReadbackHandler = new BuildMaskedReadbackQueryHandler(privacyReadbackPolicy);
    }

    public Task<VoiceInputAllowedResult> IsVoiceInputAllowedAsync(string variableId)
        => _isVoiceInputAllowedHandler.HandleAsync(new IsVoiceInputAllowedQuery(variableId));

    public Task<VoiceInputAllowedResult> ShouldOfferSilentInputOptionAsync(string variableId)
        => _shouldOfferSilentInputOptionHandler.HandleAsync(new ShouldOfferSilentInputOptionQuery(variableId));

    public Task<bool> RequiresReadbackOptInAsync(string variableId)
        => _requiresReadbackOptInHandler.HandleAsync(new RequiresReadbackOptInQuery(variableId));

    public Task<bool> CanSpeakValueInConfirmationAsync(string variableId, bool explicitOptIn)
        => _canSpeakValueInConfirmationHandler.HandleAsync(new CanSpeakValueInConfirmationQuery(variableId, explicitOptIn));

    public Task<bool> IsSpellOutAllowedInConfirmationAsync(string variableId)
        => _isSpellOutAllowedInConfirmationHandler.HandleAsync(new IsSpellOutAllowedInConfirmationQuery(variableId));

    public Task<string> BuildMaskedReadbackAsync(string variableId, string value, string isoCode)
        => _buildMaskedReadbackHandler.HandleAsync(new BuildMaskedReadbackQuery(variableId, value, isoCode));
}
