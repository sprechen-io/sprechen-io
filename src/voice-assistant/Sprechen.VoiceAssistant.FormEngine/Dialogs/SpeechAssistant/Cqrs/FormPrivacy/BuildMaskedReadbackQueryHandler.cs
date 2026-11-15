using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Leitet <see cref="BuildMaskedReadbackQuery"/> an die gemeinsame
/// <see cref="FormPrivacyReadbackPolicy"/> weiter.
/// </summary>
internal sealed class BuildMaskedReadbackQueryHandler : IQueryHandler<BuildMaskedReadbackQuery, string>
{
    private readonly FormPrivacyReadbackPolicy _privacyReadbackPolicy;

    public BuildMaskedReadbackQueryHandler(FormPrivacyReadbackPolicy privacyReadbackPolicy)
    {
        _privacyReadbackPolicy = privacyReadbackPolicy ?? throw new ArgumentNullException(nameof(privacyReadbackPolicy));
    }

    public Task<string> HandleAsync(BuildMaskedReadbackQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(_privacyReadbackPolicy.BuildMaskedReadback(query.VariableId, query.Value, query.IsoCode));
    }
}
