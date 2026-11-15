using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Lädt Katalog, Formularvertrag, Einstellungen, übernommene Werte und
/// Aktivierungswörter in einen gemeinsamen Kontext für die Formularsitzung.
/// </summary>
internal sealed class FormVoiceCommandContextLoader : IFormVoiceCommandContextLoader
{
    private readonly FormVoiceCommandCatalogSnapshotLoader _catalogSnapshotLoader;
    private readonly FormVoiceCommandContractLoader _contractLoader;
    private readonly SharedVariableResolver _sharedVariableResolver;
    private readonly IUserLanguageState _userLanguageState;
    private readonly IWakeWordProvider _wakeWordProvider;

    public FormVoiceCommandContextLoader(
        FormVoiceCommandCatalogSnapshotLoader catalogSnapshotLoader,
        FormVoiceCommandContractLoader contractLoader,
        SharedVariableResolver sharedVariableResolver,
        IUserLanguageState userLanguageState,
        IWakeWordProvider wakeWordProvider)
    {
        _catalogSnapshotLoader = catalogSnapshotLoader ?? throw new ArgumentNullException(nameof(catalogSnapshotLoader));
        _contractLoader = contractLoader ?? throw new ArgumentNullException(nameof(contractLoader));
        _sharedVariableResolver = sharedVariableResolver ?? throw new ArgumentNullException(nameof(sharedVariableResolver));
        _userLanguageState = userLanguageState ?? throw new ArgumentNullException(nameof(userLanguageState));
        _wakeWordProvider = wakeWordProvider ?? throw new ArgumentNullException(nameof(wakeWordProvider));
    }

    public async Task<FormVoiceCommandContextLoadResult> LoadAsync(
        string languageCode,
        string applicationId,
        string? formId,
        SpeechAssistantApplicationSessionState applicationSession,
        ImmutableDictionary<string, string> initialValuesByVariableId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentNullException.ThrowIfNull(applicationSession);
        ArgumentNullException.ThrowIfNull(initialValuesByVariableId);

        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(languageCode);
        var normalizedApplicationId = applicationId.Trim();
        var normalizedFormId = string.IsNullOrWhiteSpace(formId) ? null : formId.Trim();

        var abandoned = VoiceCommandDialogResult.Abandoned(
            normalizedApplicationId,
            normalizedFormId,
            initialValuesByVariableId);

        if (normalizedIsoCode is null)
        {
            return FormVoiceCommandContextLoadResult.FromAbandoned(abandoned);
        }

        var catalogSnapshot = await _catalogSnapshotLoader
            .LoadAsync(normalizedApplicationId, normalizedFormId, cancellationToken)
            .ConfigureAwait(false);
        if (catalogSnapshot is null)
        {
            return FormVoiceCommandContextLoadResult.FromAbandoned(abandoned);
        }

        var contract = await _contractLoader
            .LoadAsync(catalogSnapshot.Application, catalogSnapshot.SelectedForm, cancellationToken)
            .ConfigureAwait(false);
        if (contract is null)
        {
            return FormVoiceCommandContextLoadResult.FromAbandoned(abandoned);
        }

        var voiceSettings = VoiceDialogSettingsRuntimeOverrides.ApplyFormVoiceProfile(
            VoiceDialogSettingsRuntimeOverrides.ApplySelectedPoliteness(catalogSnapshot.BaseVoiceSettings, _userLanguageState),
            contract);

        var (effectiveValues, sharedAdoptions) = _sharedVariableResolver.ApplySharedVariables(
            catalogSnapshot.Application,
            catalogSnapshot.SelectedForm,
            contract,
            applicationSession,
            initialValuesByVariableId);

        var completedFormIds = applicationSession.FormsById
            .Where(pair => pair.Value.IsSubmitted)
            .Select(pair => pair.Key)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        var wakeWordInventory = await _wakeWordProvider
            .GetWakeWordInventoryAsync(normalizedIsoCode, cancellationToken)
            .ConfigureAwait(false);

        return FormVoiceCommandContextLoadResult.FromContext(
            new FormVoiceCommandContext(
                new FormVoiceCommandSessionIdentity(
                    normalizedIsoCode,
                    normalizedApplicationId,
                    normalizedFormId,
                    wakeWordInventory.ActivationForms.ToImmutableArray())
                {
                    WakeWordRecognitionOnlyActivationForms = wakeWordInventory.RecognitionOnlyActivationForms.ToImmutableArray()
                },
                new FormVoiceCommandSelectionContext(
                    voiceSettings,
                    catalogSnapshot.Application,
                    completedFormIds,
                    catalogSnapshot.SelectedForm,
                    contract),
                new FormVoiceCommandValueSnapshot(
                    effectiveValues,
                    sharedAdoptions)));
    }

}
