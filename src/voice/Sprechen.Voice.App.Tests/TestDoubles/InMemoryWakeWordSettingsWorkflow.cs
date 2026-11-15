using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class InMemoryWakeWordSettingsWorkflow(
    IUserLanguageState userLanguageState,
    IVoiceActivationSettings voiceActivationSettings,
    Func<string, CancellationToken, ValueTask>? runSpeechSetupAsync = null) : IWakeWordSettingsWorkflow
{
    public async ValueTask<WakeWordConfigurationState> LoadWakeWordConfigurationAsync(
        string? preferredIsoCode,
        CancellationToken cancellationToken)
    {
        return await CreateStateAsync(preferredIsoCode, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<WakeWordConfigurationState> SaveWakeWordConfigurationAsync(
        string? editorText,
        string? preferredIsoCode,
        CancellationToken cancellationToken)
    {
        var overrideWords = ParseActivationForms(editorText);
        await voiceActivationSettings.SetWakeWordActivationFormsOverrideAsync(overrideWords, cancellationToken)
            .ConfigureAwait(false);
        return await CreateStateAsync(preferredIsoCode, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<WakeWordConfigurationState> ResetWakeWordConfigurationAsync(
        string? preferredIsoCode,
        CancellationToken cancellationToken)
    {
        await voiceActivationSettings.ClearWakeWordActivationFormsOverrideAsync(cancellationToken).ConfigureAwait(false);
        return await CreateStateAsync(preferredIsoCode, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<WakeWordConfigurationState> RunWakeWordSpeechSetupAsync(
        string? preferredIsoCode,
        CancellationToken cancellationToken)
    {
        var isoCode = ResolveIsoCode(preferredIsoCode);
        if (runSpeechSetupAsync is not null)
        {
            await runSpeechSetupAsync(isoCode, cancellationToken).ConfigureAwait(false);
        }

        return await CreateStateAsync(isoCode, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<WakeWordConfigurationState> CreateStateAsync(
        string? preferredIsoCode,
        CancellationToken cancellationToken)
    {
        var isoCode = ResolveIsoCode(preferredIsoCode);
        var defaults = VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage(isoCode).ToArray();
        var overrides = await voiceActivationSettings.GetWakeWordActivationFormsOverrideAsync(cancellationToken)
            .ConfigureAwait(false);
        var editorText = overrides.Length > 0
            ? string.Join(Environment.NewLine, overrides)
            : string.Join(Environment.NewLine, defaults);
        return new WakeWordConfigurationState(isoCode, defaults, overrides, editorText);
    }

    private string ResolveIsoCode(string? preferredIsoCode)
    {
        return InMemoryUserLanguageSettingsWorkflow.NormalizeIsoCode(
            preferredIsoCode ?? userLanguageState.SelectedIsoCode);
    }

    private static string[] ParseActivationForms(string? editorText)
    {
        return (editorText ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
