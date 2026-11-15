using Sprechen.Mda.Contracts.V1;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Vergleicht mögliche Zieltexte nach gemeinsamer Normalisierung mit dem normalisierten Suchziel, damit Sprungauflöser dieselbe Textgleichheit verwenden.
/// </summary>
internal sealed class FormJumpTargetTextMatcher
{
    private readonly FormLocalizationResolver localizationResolver;

    public FormJumpTargetTextMatcher(FormLocalizationResolver localizationResolver)
    {
        this.localizationResolver = localizationResolver ?? throw new ArgumentNullException(nameof(localizationResolver));
    }

    public bool Matches(LocalizedText? text, string normalizedTarget)
        => Matches(localizationResolver.ResolveText(text, fallback: null), normalizedTarget);

    public bool Matches(string? candidate, string normalizedTarget)
        => !string.IsNullOrWhiteSpace(candidate)
            && string.Equals(
                TextNormalizer.NormalizeForPhraseMatching(candidate),
                normalizedTarget,
                StringComparison.Ordinal);
}
