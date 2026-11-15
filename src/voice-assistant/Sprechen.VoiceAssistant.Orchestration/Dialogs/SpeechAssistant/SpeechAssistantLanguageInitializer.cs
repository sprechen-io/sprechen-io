namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Löst die anfängliche Sitzungssprache auf und übergibt sie dem Einstellungsablauf, bevor das
/// Einstiegsangebot eine Korrektur dieser Auswahl anbietet.
/// </summary>
internal sealed class SpeechAssistantLanguageInitializer
{
    private readonly SpeechAssistantSetupDependencies _setupServices;

    public SpeechAssistantLanguageInitializer(SpeechAssistantSetupDependencies setupServices)
    {
        _setupServices = setupServices ?? throw new ArgumentNullException(nameof(setupServices));
    }

    public async ValueTask<string> InitializeAsync(CancellationToken cancellationToken)
    {
        var selectedIsoCode = SpeechAssistantSelectedIsoCodeResolver.Resolve(_setupServices.UserLanguageState.SelectedIsoCode);
        await _setupServices.UserLanguageSettingsWorkflow
            .SaveSelectedLanguageAsync(selectedIsoCode, cancellationToken)
            .ConfigureAwait(false);

        return selectedIsoCode;
    }
}
