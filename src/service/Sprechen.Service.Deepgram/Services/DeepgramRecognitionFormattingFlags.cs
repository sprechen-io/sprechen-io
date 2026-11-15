using Sprechen.Service.Abstractions;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Ordnet Erkennungsprofilen die Deepgram-Flags für Formatierung, Satzzeichen und Ziffern zu.
/// Buchstabieren deaktiviert alle drei Flags. Das Ziffernprofil aktiviert nur Numerals.
/// </summary>
internal readonly record struct DeepgramRecognitionFormattingFlags(bool SmartFormat, bool Punctuate, bool? Numerals)
{
    internal static DeepgramRecognitionFormattingFlags FromProfile(RecognitionProfile profile) => profile switch
    {
        RecognitionProfile.Spelling => new DeepgramRecognitionFormattingFlags(SmartFormat: false, Punctuate: false, Numerals: false),
        RecognitionProfile.Digits => new DeepgramRecognitionFormattingFlags(SmartFormat: false, Punctuate: false, Numerals: true),
        // Gesprächsprofil und unbekannte Werte verwenden Formatierung und Satzzeichen. Numerals bleibt ungesetzt.
        _ => new DeepgramRecognitionFormattingFlags(SmartFormat: true, Punctuate: true, Numerals: null)
    };
}
