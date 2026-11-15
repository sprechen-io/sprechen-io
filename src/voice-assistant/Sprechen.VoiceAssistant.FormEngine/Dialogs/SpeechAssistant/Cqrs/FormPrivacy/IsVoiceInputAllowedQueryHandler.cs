using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Leitet <see cref="IsVoiceInputAllowedQuery"/> an die gemeinsame
/// <see cref="FormPrivacyReadbackPolicy"/> weiter.
/// </summary>
internal sealed class IsVoiceInputAllowedQueryHandler : IQueryHandler<IsVoiceInputAllowedQuery, VoiceInputAllowedResult>
{
    private readonly FormPrivacyReadbackPolicy _privacyReadbackPolicy;

    public IsVoiceInputAllowedQueryHandler(FormPrivacyReadbackPolicy privacyReadbackPolicy)
    {
        _privacyReadbackPolicy = privacyReadbackPolicy ?? throw new ArgumentNullException(nameof(privacyReadbackPolicy));
    }

    public Task<VoiceInputAllowedResult> HandleAsync(IsVoiceInputAllowedQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(_privacyReadbackPolicy.IsVoiceInputAllowed(query.VariableId));
    }
}
