using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Leitet <see cref="IsSpellOutAllowedInConfirmationQuery"/> an die gemeinsame
/// <see cref="FormPrivacyReadbackPolicy"/> weiter.
/// </summary>
internal sealed class IsSpellOutAllowedInConfirmationQueryHandler : IQueryHandler<IsSpellOutAllowedInConfirmationQuery, bool>
{
    private readonly FormPrivacyReadbackPolicy _privacyReadbackPolicy;

    public IsSpellOutAllowedInConfirmationQueryHandler(FormPrivacyReadbackPolicy privacyReadbackPolicy)
    {
        _privacyReadbackPolicy = privacyReadbackPolicy ?? throw new ArgumentNullException(nameof(privacyReadbackPolicy));
    }

    public Task<bool> HandleAsync(IsSpellOutAllowedInConfirmationQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(_privacyReadbackPolicy.IsSpellOutAllowedInConfirmation(query.VariableId));
    }
}
