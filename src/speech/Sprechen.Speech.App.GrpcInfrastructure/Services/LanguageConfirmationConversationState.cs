using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Sammelt Transkript und Sprachereignisse für das Bestätigungsergebnis. Die festgelegte Sprache
/// hat Vorrang vor Erkennung und Vorgabe, damit spätere Erkennungshinweise diese Festlegung nicht verdrängen.
/// </summary>
internal sealed class LanguageConfirmationConversationState
{
    private readonly SupportedLanguages _supportedLanguages;
    private readonly string? _requestedLanguageCode;

    private string? _lastTranscript;
    private string? _lockedLanguageCode;
    private IReadOnlyList<string> _detectedLanguageCodes = Array.Empty<string>();

    public LanguageConfirmationConversationState(SupportedLanguages supportedLanguages, string? requestedLanguageCode)
    {
        _supportedLanguages = supportedLanguages ?? throw new ArgumentNullException(nameof(supportedLanguages));
        _requestedLanguageCode = string.IsNullOrWhiteSpace(requestedLanguageCode) ? null : requestedLanguageCode.Trim();
    }

    public void Apply(SpeechConversationEvent conversationEvent)
    {
        ArgumentNullException.ThrowIfNull(conversationEvent);

        switch (conversationEvent)
        {
            case SpeechConversationTranscriptUpdateEvent update when !string.IsNullOrWhiteSpace(update.Transcript):
                _lastTranscript = update.Transcript.Trim();
                break;
            case SpeechConversationTranscriptFinalizedEvent finalized when !string.IsNullOrWhiteSpace(finalized.Transcript):
                _lastTranscript = finalized.Transcript.Trim();
                break;
            case SpeechConversationEndOfTurnEvent endOfTurn when !string.IsNullOrWhiteSpace(endOfTurn.Transcript):
                _lastTranscript = endOfTurn.Transcript.Trim();
                break;
            case SpeechConversationEagerEndOfTurnEvent eagerEndOfTurn when !string.IsNullOrWhiteSpace(eagerEndOfTurn.Transcript):
                _lastTranscript = eagerEndOfTurn.Transcript.Trim();
                break;
            case SpeechConversationLanguageLockedEvent languageLocked:
                _lockedLanguageCode = NormalizeIso(languageLocked.LanguageCode);
                if (!string.IsNullOrWhiteSpace(_lockedLanguageCode))
                {
                    _detectedLanguageCodes = new[] { _lockedLanguageCode };
                }

                break;
            case SpeechConversationLanguageDetectedEvent languageDetected when languageDetected.Languages.Count > 0:
                _detectedLanguageCodes = languageDetected.Languages
                    .Select(NormalizeIso)
                    .Where(static iso => !string.IsNullOrWhiteSpace(iso))
                    .Cast<string>()
                    .ToArray();
                break;
        }
    }

    public void EnsureTranscriptPresent()
    {
        if (string.IsNullOrWhiteSpace(_lastTranscript))
        {
            throw new NoSpeechDetectedException();
        }
    }

    public LanguageConfirmationStreamingResult BuildResult()
    {
        var lockedLanguage = NormalizeIso(_lockedLanguageCode);
        if (!string.IsNullOrWhiteSpace(lockedLanguage))
        {
            return new LanguageConfirmationStreamingResult(
                CreateDetectedLanguage(
                    lockedLanguage,
                    new[] { lockedLanguage },
                    probability: 1.0f),
                _lastTranscript);
        }

        if (_detectedLanguageCodes.Count > 0)
        {
            var primary = NormalizeIso(_detectedLanguageCodes[0]);
            if (!string.IsNullOrWhiteSpace(primary))
            {
                return new LanguageConfirmationStreamingResult(
                    CreateDetectedLanguage(primary, _detectedLanguageCodes, probability: 0.0f),
                    _lastTranscript);
            }
        }

        var requestedLanguage = NormalizeIso(_requestedLanguageCode);
        if (!string.IsNullOrWhiteSpace(requestedLanguage))
        {
            return new LanguageConfirmationStreamingResult(
                CreateDetectedLanguage(
                    requestedLanguage,
                    new[] { requestedLanguage },
                    probability: 1.0f),
                _lastTranscript);
        }

        throw new InvalidOperationException("Language confirmation stream produced no language result.");
    }

    private DetectedLanguage CreateDetectedLanguage(
        string isoCode,
        IReadOnlyList<string> languageCodes,
        float probability)
    {
        var normalizedIso = NormalizeIso(isoCode)
            ?? throw new InvalidOperationException("Detected language code is missing.");
        var displayName = ResolveDisplayName(normalizedIso);
        var candidates = languageCodes
            .Select(NormalizeIso)
            .Where(static iso => !string.IsNullOrWhiteSpace(iso))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(candidateIso => new DetectedLanguageCandidate(
                candidateIso,
                string.Equals(candidateIso, normalizedIso, StringComparison.OrdinalIgnoreCase) ? probability : 0.0f,
                ResolveDisplayName(candidateIso)))
            .ToArray();

        return new DetectedLanguage(
            normalizedIso,
            displayName,
            candidates);
    }

    private string ResolveDisplayName(string isoCode)
    {
        var match = _supportedLanguages.Languages.FirstOrDefault(language =>
            string.Equals(language.IsoCode, isoCode, StringComparison.OrdinalIgnoreCase));
        return match?.DisplayName ?? isoCode;
    }

    private static string? NormalizeIso(string? isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return null;
        }

        var token = isoCode.Trim();
        var dashIndex = token.IndexOf('-', StringComparison.Ordinal);
        if (dashIndex > 0)
        {
            token = token[..dashIndex];
        }

        token = token.ToLowerInvariant();
        return token.Length == 2 && token.All(char.IsLetter) ? token : null;
    }
}
