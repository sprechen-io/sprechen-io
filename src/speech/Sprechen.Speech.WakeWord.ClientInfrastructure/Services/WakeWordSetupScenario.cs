namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Hält die sprachabhängigen Einrichtungsansagen und ihre Vorlagen zusammen,
/// damit Bestätigung und Speicheransage die gerade aufgenommenen Aktivierungsformen nennen.
/// </summary>
internal sealed class WakeWordSetupScenario
{
    private const string WakeWordsPlaceholder = "{wakeWords}";
    private const string YesPlaceholder = "{yes}";
    private const string NoPlaceholder = "{no}";

    private readonly string _confirmPromptTemplate;
    private readonly string _savedPromptTemplate;

    public WakeWordSetupScenario(
        string isoCode,
        string changePrompt,
        string askForNewWakeWordsPrompt,
        string notRecognizedPrompt,
        string confirmPromptTemplate,
        string savedPromptTemplate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(changePrompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(askForNewWakeWordsPrompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(notRecognizedPrompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmPromptTemplate);
        ArgumentException.ThrowIfNullOrWhiteSpace(savedPromptTemplate);

        IsoCode = isoCode;
        ChangePrompt = changePrompt;
        AskForNewWakeWordsPrompt = askForNewWakeWordsPrompt;
        NotRecognizedPrompt = notRecognizedPrompt;
        _confirmPromptTemplate = confirmPromptTemplate;
        _savedPromptTemplate = savedPromptTemplate;
    }

    public string IsoCode { get; }

    public string ChangePrompt { get; }

    public string AskForNewWakeWordsPrompt { get; }

    public string NotRecognizedPrompt { get; }

    public string CreateConfirmationPrompt(string[] wakeWordActivationForms)
    {
        ArgumentNullException.ThrowIfNull(wakeWordActivationForms);

        return ApplyPlaceholders(
            _confirmPromptTemplate,
            (WakeWordsPlaceholder, new WakeWordActivationFormCodec().Format(wakeWordActivationForms)),
            (YesPlaceholder, YesNo.GetYesWord(IsoCode)),
            (NoPlaceholder, YesNo.GetNoWord(IsoCode)));
    }

    public string CreateSavedPrompt(string[] wakeWordActivationForms)
    {
        ArgumentNullException.ThrowIfNull(wakeWordActivationForms);

        return ApplyPlaceholders(
            _savedPromptTemplate,
            (WakeWordsPlaceholder, new WakeWordActivationFormCodec().Format(wakeWordActivationForms)));
    }

    private static string ApplyPlaceholders(string template, params (string placeholder, string value)[] replacements)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(replacements);

        var result = template;

        foreach (var (placeholder, value) in replacements)
        {
            if (string.IsNullOrEmpty(placeholder))
            {
                continue;
            }

            result = result.Replace(placeholder, value ?? string.Empty, StringComparison.Ordinal);
        }

        return result;
    }
}
