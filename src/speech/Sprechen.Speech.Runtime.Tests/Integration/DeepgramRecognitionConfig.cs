namespace Sprechen.Speech.Runtime.Tests.Integration;

// Numerals bleibt nullable: Ohne Vorgabe wird der Parameter nicht an den Anbieter gesendet.
internal readonly record struct DeepgramRecognitionConfig(bool SmartFormat, bool Punctuate, bool? Numerals)
{
    public static DeepgramRecognitionConfig Default { get; } = new(SmartFormat: true, Punctuate: true, Numerals: null);
}
