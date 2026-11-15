namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Erzeugt den Standardtranskriptparser für Formulareingaben mit den
/// übergebenen Klassifikatoren und dem eingebetteten Lexikon.
/// </summary>
public static class FormTranscriptParsers
{
    public static IFormTranscriptParser CreateDefault(
        IYesNoTranscriptClassifier yesNoClassifier,
        INegationParticleResolver negationParticleResolver)
    {
        return new FormTranscriptParser(yesNoClassifier, negationParticleResolver);
    }
}
