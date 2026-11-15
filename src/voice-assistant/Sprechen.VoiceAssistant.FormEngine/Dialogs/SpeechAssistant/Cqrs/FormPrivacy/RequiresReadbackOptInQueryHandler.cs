using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Leitet <see cref="RequiresReadbackOptInQuery"/> an die gemeinsame
/// <see cref="FormPrivacyReadbackPolicy"/> weiter.
/// </summary>
internal sealed class RequiresReadbackOptInQueryHandler : IQueryHandler<RequiresReadbackOptInQuery, bool>
{
    private readonly FormPrivacyReadbackPolicy _privacyReadbackPolicy;

    public RequiresReadbackOptInQueryHandler(FormPrivacyReadbackPolicy privacyReadbackPolicy)
    {
        _privacyReadbackPolicy = privacyReadbackPolicy ?? throw new ArgumentNullException(nameof(privacyReadbackPolicy));
    }

    public Task<bool> HandleAsync(RequiresReadbackOptInQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(_privacyReadbackPolicy.RequiresReadbackOptIn(query.VariableId));
    }
}
