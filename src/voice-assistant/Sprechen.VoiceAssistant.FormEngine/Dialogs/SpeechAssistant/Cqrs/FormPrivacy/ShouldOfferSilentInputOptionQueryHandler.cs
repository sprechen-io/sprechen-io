using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Leitet <see cref="ShouldOfferSilentInputOptionQuery"/> an die gemeinsame
/// <see cref="FormPrivacyReadbackPolicy"/> weiter.
/// </summary>
internal sealed class ShouldOfferSilentInputOptionQueryHandler : IQueryHandler<ShouldOfferSilentInputOptionQuery, VoiceInputAllowedResult>
{
    private readonly FormPrivacyReadbackPolicy _privacyReadbackPolicy;

    public ShouldOfferSilentInputOptionQueryHandler(FormPrivacyReadbackPolicy privacyReadbackPolicy)
    {
        _privacyReadbackPolicy = privacyReadbackPolicy ?? throw new ArgumentNullException(nameof(privacyReadbackPolicy));
    }

    public Task<VoiceInputAllowedResult> HandleAsync(ShouldOfferSilentInputOptionQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(_privacyReadbackPolicy.ShouldOfferSilentInputOption(query.VariableId));
    }
}
