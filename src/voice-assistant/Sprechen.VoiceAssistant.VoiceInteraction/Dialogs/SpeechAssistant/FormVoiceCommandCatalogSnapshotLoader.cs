using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Lädt Anwendung und Formular aus dem Katalog und verbindet Anwendungs- und Katalogeinstellungen mit lokalen Abweichungen. Bei abgefangenen Fehlern der lokalen Verarbeitung bleiben die Basiseinstellungen.
/// </summary>
internal sealed class FormVoiceCommandCatalogSnapshotLoader
{
    private readonly IFormsCatalogDataClient _catalogClient;
    private readonly IVoiceDialogSettingsOverrideStore _voiceDialogSettingsOverrideStore;
    private readonly ILogger<FormVoiceCommandCatalogSnapshotLoader> _logger;

    public FormVoiceCommandCatalogSnapshotLoader(
        IFormsCatalogDataClient catalogClient,
        IVoiceDialogSettingsOverrideStore voiceDialogSettingsOverrideStore,
        ILogger<FormVoiceCommandCatalogSnapshotLoader> logger)
    {
        _catalogClient = catalogClient ?? throw new ArgumentNullException(nameof(catalogClient));
        _voiceDialogSettingsOverrideStore = voiceDialogSettingsOverrideStore ?? throw new ArgumentNullException(nameof(voiceDialogSettingsOverrideStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FormVoiceCommandCatalogSnapshot?> LoadAsync(
        string applicationId,
        string? formId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        FormsCatalog catalog;
        try
        {
            catalog = await _catalogClient.GetFormsCatalogAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load application catalog.");
            return null;
        }

        var normalizedApplicationId = applicationId.Trim();
        var normalizedFormId = string.IsNullOrWhiteSpace(formId) ? null : formId.Trim();
        var application = catalog.Applications.FirstOrDefault(app =>
            string.Equals(app.ApplicationId, normalizedApplicationId, StringComparison.OrdinalIgnoreCase));

        if (application is null)
        {
            _logger.LogDebug("Application '{ApplicationId}' not found in MDA catalog.", normalizedApplicationId);
            return null;
        }

        var selectedForm = ResolveForm(application, normalizedFormId);
        if (selectedForm is null)
        {
            _logger.LogDebug("Form '{FormId}' not found for application '{ApplicationId}'.", normalizedFormId, normalizedApplicationId);
            return null;
        }

        var baseVoiceSettings = (application.VoiceDialogSettings ?? new VoiceDialogSettings())
            .MergeWith(catalog.VoiceDialogSettings);
        var effectiveVoiceSettings = await ApplyLocalOverridesAsync(
                baseVoiceSettings,
                normalizedApplicationId,
                cancellationToken)
            .ConfigureAwait(false);

        return new FormVoiceCommandCatalogSnapshot(catalog, application, selectedForm, effectiveVoiceSettings);
    }

    private async Task<VoiceDialogSettings> ApplyLocalOverridesAsync(
        VoiceDialogSettings settings,
        string applicationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var applicationAllowBargeInOverride = await _voiceDialogSettingsOverrideStore
                .GetApplicationAllowBargeInOverrideAsync(applicationId, cancellationToken)
                .ConfigureAwait(false);

            var deviceAllowBargeInOverride = await _voiceDialogSettingsOverrideStore
                .GetDeviceAllowBargeInOverrideAsync(cancellationToken)
                .ConfigureAwait(false);

            var applicationAutoAdvanceOverride = await _voiceDialogSettingsOverrideStore
                .GetApplicationAutoAdvanceAfterCommitOverrideAsync(applicationId, cancellationToken)
                .ConfigureAwait(false);

            var deviceAutoAdvanceOverride = await _voiceDialogSettingsOverrideStore
                .GetDeviceAutoAdvanceAfterCommitOverrideAsync(cancellationToken)
                .ConfigureAwait(false);

            var overridden = VoiceDialogSettingsRuntimeOverrides.ApplyAllowBargeInOverride(
                VoiceDialogSettingsRuntimeOverrides.ApplyAllowBargeInOverride(settings, applicationAllowBargeInOverride),
                deviceAllowBargeInOverride);

            return VoiceDialogSettingsRuntimeOverrides.ApplyAutoAdvanceAfterCommitOverride(
                VoiceDialogSettingsRuntimeOverrides.ApplyAutoAdvanceAfterCommitOverride(overridden, applicationAutoAdvanceOverride),
                deviceAutoAdvanceOverride);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load local voice dialog overrides for application '{ApplicationId}'.", applicationId);
            return settings;
        }
    }

    private static FormDescriptor? ResolveForm(ApplicationDescriptor application, string? formId)
    {
        if (!string.IsNullOrWhiteSpace(formId))
        {
            return application.Forms.FirstOrDefault(form =>
                string.Equals(form.FormId, formId, StringComparison.OrdinalIgnoreCase));
        }

        if (application.Forms.Count == 0)
        {
            return null;
        }

        return new FormsCatalogFormOrder().OrderForms(application).FirstOrDefault();
    }
}

