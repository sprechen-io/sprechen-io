namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Verbindet Laden, Bearbeiten und gesprochenes Einrichten der Aktivierungsformen,
/// damit die Einstellungsansicht Sprachwahl, Vorgaben und Überschreibungen gemeinsam erhält.
/// </summary>
internal sealed class WakeWordSettingsWorkflow(
    IUserLanguageState userLanguageState,
    IVoiceActivationSettings voiceActivationSettings,
    IWakeWordSpeechSetupRunner wakeWordSpeechSetupRunner,
    SettingsVoiceLanguageResolver languageResolver) : IWakeWordSettingsWorkflow
{
    private readonly SettingsVoiceLanguageResolver _languageResolver =
        languageResolver ?? throw new ArgumentNullException(nameof(languageResolver));
    private readonly IUserLanguageState _userLanguageState =
        userLanguageState ?? throw new ArgumentNullException(nameof(userLanguageState));
    private readonly IVoiceActivationSettings _voiceActivationSettings =
        voiceActivationSettings ?? throw new ArgumentNullException(nameof(voiceActivationSettings));
    private readonly IWakeWordSpeechSetupRunner _wakeWordSpeechSetupRunner =
        wakeWordSpeechSetupRunner ?? throw new ArgumentNullException(nameof(wakeWordSpeechSetupRunner));

    public async ValueTask<WakeWordConfigurationState> LoadWakeWordConfigurationAsync(
        string? preferredIsoCode,
        CancellationToken cancellationToken)
    {
        var overrideWakeWordActivationForms = new WakeWordActivationFormCodec().Normalize(
            await _voiceActivationSettings.GetWakeWordActivationFormsOverrideAsync(cancellationToken).ConfigureAwait(false));

        return CreateWakeWordConfigurationState(ResolveSelectedIsoCode(preferredIsoCode), overrideWakeWordActivationForms);
    }

    public async ValueTask<WakeWordConfigurationState> SaveWakeWordConfigurationAsync(
        string? editorText,
        string? preferredIsoCode,
        CancellationToken cancellationToken)
    {
        var overrideWakeWordActivationForms = ParseEditorActivationForms(editorText);
        await _voiceActivationSettings
            .SetWakeWordActivationFormsOverrideAsync(overrideWakeWordActivationForms, cancellationToken)
            .ConfigureAwait(false);
        return CreateWakeWordConfigurationState(ResolveSelectedIsoCode(preferredIsoCode), overrideWakeWordActivationForms);
    }

    public async ValueTask<WakeWordConfigurationState> ResetWakeWordConfigurationAsync(
        string? preferredIsoCode,
        CancellationToken cancellationToken)
    {
        await _voiceActivationSettings.ClearWakeWordActivationFormsOverrideAsync(cancellationToken).ConfigureAwait(false);
        return CreateWakeWordConfigurationState(ResolveSelectedIsoCode(preferredIsoCode), []);
    }

    public async ValueTask<WakeWordConfigurationState> RunWakeWordSpeechSetupAsync(
        string? preferredIsoCode,
        CancellationToken cancellationToken)
    {
        var selectedIsoCode = ResolveSelectedIsoCode(preferredIsoCode);
        await _wakeWordSpeechSetupRunner
            .EnsureWakeWordsConfiguredAsync(selectedIsoCode, cancellationToken)
            .ConfigureAwait(false);
        return await LoadWakeWordConfigurationAsync(selectedIsoCode, cancellationToken).ConfigureAwait(false);
    }

    private WakeWordConfigurationState CreateWakeWordConfigurationState(
        string selectedIsoCode,
        string[] overrideWakeWordActivationForms)
    {
        var normalizedIsoCode = ResolveSelectedIsoCode(selectedIsoCode);
        var defaultWakeWordActivationForms = new WakeWordActivationFormCodec().Normalize(
            VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage(normalizedIsoCode));
        var normalizedOverrideWakeWordActivationForms =
            new WakeWordActivationFormCodec().Normalize(overrideWakeWordActivationForms);
        var editorText = normalizedOverrideWakeWordActivationForms.Length == 0
            ? FormatActivationForms(defaultWakeWordActivationForms)
            : FormatActivationForms(normalizedOverrideWakeWordActivationForms);

        return new WakeWordConfigurationState(
            normalizedIsoCode,
            defaultWakeWordActivationForms,
            normalizedOverrideWakeWordActivationForms,
            editorText);
    }

    private string ResolveSelectedIsoCode(string? preferredIsoCode)
        => _languageResolver.ResolveSelectedIsoCode(_userLanguageState, preferredIsoCode);

    private static string[] ParseEditorActivationForms(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var lines = text
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return new WakeWordActivationFormCodec().Normalize(lines);
    }

    private static string FormatActivationForms(IEnumerable<string> activationForms)
    {
        return string.Join(Environment.NewLine, activationForms);
    }
}
