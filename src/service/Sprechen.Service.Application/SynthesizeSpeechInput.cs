namespace Sprechen.Service.Application;

/// <summary>
/// Eingabe für die Sprachsynthese mit Text, Sprache, Audioformat, Tempo und Höflichkeitsform.
/// </summary>
public sealed record SynthesizeSpeechInput(
    string Text,
    string LanguageCode,
    AudioMimeType DesiredMimeType,
    float SpeechRate,
    TextToSpeechPolitenessLevel PolitenessLevel);
