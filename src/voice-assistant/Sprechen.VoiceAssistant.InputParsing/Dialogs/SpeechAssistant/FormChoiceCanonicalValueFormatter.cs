namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Bildet kanonische Auswahlwerte aus Optionscodes. Mehrfachauswahlen werden
/// eindeutig sortiert und für die weitere Verarbeitung verbunden.
/// </summary>
internal static class FormChoiceCanonicalValueFormatter
{
    public static string Format(IEnumerable<string> selectedCodes, bool allowMultiple)
    {
        var selected = selectedCodes.ToArray();
        return allowMultiple
            ? string.Join("|", selected.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
            : selected[0];
    }
}
