namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Erstellt Einrichtungsansagen aus Modellressourcen und aktuellem Aktivierungsinventar,
/// damit der Ablauf zur gewählten Sprache und den vorhandenen Formen passt.
/// </summary>
internal sealed class WakeWordSetupScenarioFactory(
    IWakeWordProvider wakeWordProvider) : IWakeWordSetupScenarioFactory
{
    private const string WakeWordsPromptResourceId = VoiceInterfaceResourceIds.SessionSetupWakeWordsPrompt;
    private const string WakeWordsAskNewResourceId = VoiceInterfaceResourceIds.SessionSetupWakeWordsAskNew;
    private const string WakeWordsNotRecognizedResourceId = VoiceInterfaceResourceIds.SessionSetupWakeWordsNotRecognized;
    private const string WakeWordsConfirmResourceId = VoiceInterfaceResourceIds.SessionSetupWakeWordsConfirm;
    private const string WakeWordsSavedResourceId = VoiceInterfaceResourceIds.SessionSetupWakeWordsSaved;

    private const string WakeWordsPlaceholder = "{wakeWords}";
    private const string YesPlaceholder = "{yes}";
    private const string NoPlaceholder = "{no}";

    private readonly IWakeWordProvider _wakeWordProvider =
        wakeWordProvider ?? throw new ArgumentNullException(nameof(wakeWordProvider));

    public async Task<WakeWordSetupScenario?> CreateAsync(string isoCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var normalizedIsoCode = NormalizeIsoCode(isoCode);
        var wakeWordInventory = await _wakeWordProvider
            .GetWakeWordInventoryAsync(normalizedIsoCode, cancellationToken)
            .ConfigureAwait(false);
        if (wakeWordInventory.ActivationForms.Count == 0)
        {
            return null;
        }

        var promptTemplate = ResolveResourceText(WakeWordsPromptResourceId, normalizedIsoCode);
        var changePrompt = ApplyPlaceholders(
            promptTemplate,
            (WakeWordsPlaceholder, new WakeWordActivationFormCodec().Format(
                wakeWordInventory.ActivationForms)),
            (YesPlaceholder, YesNo.GetYesWord(normalizedIsoCode)),
            (NoPlaceholder, YesNo.GetNoWord(normalizedIsoCode)));

        return new WakeWordSetupScenario(
            normalizedIsoCode,
            changePrompt,
            ResolveResourceText(WakeWordsAskNewResourceId, normalizedIsoCode),
            ResolveResourceText(WakeWordsNotRecognizedResourceId, normalizedIsoCode),
            ResolveResourceText(WakeWordsConfirmResourceId, normalizedIsoCode),
            ResolveResourceText(WakeWordsSavedResourceId, normalizedIsoCode));
    }

    private static string ResolveResourceText(string resourceId, string isoCode)
    {
        if (VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text))
        {
            return text;
        }

        throw new InvalidOperationException(
            $"Voice interface resource '{resourceId}' is missing text for '{isoCode}'.");
    }

    private static string NormalizeIsoCode(string? raw)
    {
        return IsoCode.TryNormalizeIso6391(raw) ?? VoiceInterfaceModelCatalog.DefaultLanguage;
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
