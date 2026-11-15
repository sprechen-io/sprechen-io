using Sprechen.Proto;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Erzeugt eine PCM-Sprachausgabeanfrage mit getrimmtem Text und Sprachcode
/// sowie ausgewählter Höflichkeitsstufe. Das übergebene Sprechtempo wird auf
/// einen positiven endlichen Wert geprüft und in das Transportformat übernommen.
/// </summary>
internal sealed class TextToSpeechRequestFactory
{
    private readonly IUserLanguagePreferences _userLanguagePreferences;

    public TextToSpeechRequestFactory(IUserLanguagePreferences userLanguagePreferences)
    {
        _userLanguagePreferences = userLanguagePreferences ?? throw new ArgumentNullException(nameof(userLanguagePreferences));
    }

    public TextRequest Create(string text, string languageCode, double speechRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (!double.IsFinite(speechRate) || speechRate <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(speechRate), speechRate, "Speech rate must be a finite positive multiplier.");
        }

        return new TextRequest
        {
            Text = text.Trim(),
            LanguageCode = (languageCode ?? string.Empty).Trim(),
            DesiredMimeType = AudioMimeType.AudioPcm,
            PolitenessLevel = MapPolitenessLevel(_userLanguagePreferences.SelectedPolitenessLevel),
            SpeechRate = (float)speechRate
        };
    }

    private static TextToSpeechPolitenessLevel MapPolitenessLevel(PolitenessLevel? politenessLevel)
        => politenessLevel switch
        {
            PolitenessLevel.Formal => TextToSpeechPolitenessLevel.TtsPolitenessFormal,
            PolitenessLevel.Informal => TextToSpeechPolitenessLevel.TtsPolitenessInformal,
            _ => TextToSpeechPolitenessLevel.TtsPolitenessUnspecified
        };
}
