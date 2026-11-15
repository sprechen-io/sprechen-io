using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Leitet <see cref="CanSpeakValueInConfirmationQuery"/> an die gemeinsame
/// <see cref="FormPrivacyReadbackPolicy"/> weiter.
/// </summary>
internal sealed class CanSpeakValueInConfirmationQueryHandler : IQueryHandler<CanSpeakValueInConfirmationQuery, bool>
{
    private readonly FormPrivacyReadbackPolicy _privacyReadbackPolicy;

    public CanSpeakValueInConfirmationQueryHandler(FormPrivacyReadbackPolicy privacyReadbackPolicy)
    {
        _privacyReadbackPolicy = privacyReadbackPolicy ?? throw new ArgumentNullException(nameof(privacyReadbackPolicy));
    }

    public Task<bool> HandleAsync(CanSpeakValueInConfirmationQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(_privacyReadbackPolicy.CanSpeakValueInConfirmation(query.VariableId, query.ExplicitOptIn));
    }
}
