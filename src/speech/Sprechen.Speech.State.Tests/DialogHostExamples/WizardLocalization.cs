namespace Sprechen.Speech.State.Tests.DialogHostExamples;

internal static class WizardLocalization
{
    public static string Pick(string? languageCode, string german, string english)
    {
        return languageCode?.Equals("de", StringComparison.OrdinalIgnoreCase) == true ? german : english;
    }
}


