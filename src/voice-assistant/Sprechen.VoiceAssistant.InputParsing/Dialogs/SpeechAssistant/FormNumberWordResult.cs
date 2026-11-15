namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis einer Zahlwortzusammensetzung mit Klassifikation und erkanntem Wert.
/// </summary>
internal readonly record struct FormNumberWordResult(FormNumberWordComposition Kind, int Value)
{
    public static readonly FormNumberWordResult None = new(FormNumberWordComposition.None, 0);

    public static readonly FormNumberWordResult Multiple = new(FormNumberWordComposition.MultipleNumbers, 0);

    public static readonly FormNumberWordResult OutOfRange = new(FormNumberWordComposition.OutOfRange, 0);

    public static FormNumberWordResult FromValue(int value) => new(FormNumberWordComposition.Value, value);
}
