namespace Sprechen.Speech.State.Tests.DialogHostExamples;

internal sealed class WizardContext
{
    private readonly Dictionary<WizardFieldId, string> _fieldValues = new();

    public WizardContext(IReadOnlyList<WizardLanguage> supportedLanguages)
    {
        SupportedLanguages = supportedLanguages ?? throw new ArgumentNullException(nameof(supportedLanguages));
    }

    public IReadOnlyList<WizardLanguage> SupportedLanguages { get; }

    public string? SelectedLanguageCode { get; private set; }

    public string? SelectedApplicationId { get; private set; }

    public WizardFieldId? FocusField { get; set; }

    public IReadOnlyDictionary<WizardFieldId, string> FieldValues => _fieldValues;

    public List<string> Prompts { get; } = [];

    public void SetSelectedLanguage(string languageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        if (SupportedLanguages.All(lang => !lang.Code.Equals(languageCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Language '{languageCode}' is not supported.");
        }

        SelectedLanguageCode = languageCode;
    }

    public void SetSelectedApplication(string applicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        SelectedApplicationId = applicationId;
    }

    public void SetFieldValue(WizardFieldId field, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _fieldValues[field] = value;
    }
}


