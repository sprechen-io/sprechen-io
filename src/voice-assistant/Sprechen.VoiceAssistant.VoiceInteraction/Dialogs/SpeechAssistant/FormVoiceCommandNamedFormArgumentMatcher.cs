using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Sucht das erste Formular mit passendem normalisiertem Anzeigenamen oder passender Kennung in der übergebenen Reihenfolge.
/// </summary>
internal sealed class FormVoiceCommandNamedFormArgumentMatcher
{
    private readonly FormVoiceCommandFormDisplayNameResolver _displayNameResolver;

    public FormVoiceCommandNamedFormArgumentMatcher(FormVoiceCommandFormDisplayNameResolver displayNameResolver)
    {
        _displayNameResolver = displayNameResolver ?? throw new ArgumentNullException(nameof(displayNameResolver));
    }

    public bool TryMatch(
        ApplicationDescriptor application,
        ImmutableArray<FormDescriptor> orderedForms,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        string trimmedArgument,
        out FormDescriptor form,
        out string formName)
    {
        form = null!;
        formName = string.Empty;
        var normalizedArgument = TextNormalizer.NormalizeForPhraseMatching(trimmedArgument);

        foreach (var candidate in orderedForms)
        {
            var candidateName = _displayNameResolver.Resolve(application, candidate, isoCode, voiceSettings);
            if (MatchesCandidate(candidate, candidateName, normalizedArgument))
            {
                form = candidate;
                formName = candidateName;
                return true;
            }
        }

        return false;
    }

    private static bool MatchesCandidate(FormDescriptor candidate, string candidateName, string normalizedArgument)
    {
        return string.Equals(TextNormalizer.NormalizeForPhraseMatching(candidateName), normalizedArgument, StringComparison.Ordinal)
            || string.Equals(TextNormalizer.NormalizeForPhraseMatching(candidate.FormId), normalizedArgument, StringComparison.Ordinal);
    }
}
