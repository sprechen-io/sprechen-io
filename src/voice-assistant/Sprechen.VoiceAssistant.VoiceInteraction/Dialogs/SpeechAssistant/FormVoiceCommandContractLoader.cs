using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Lädt die im Katalog bezeichnete Vertragsversion und weist fehlende Feld- oder Gruppenbestände zurück, bevor der Sitzungskontext sie übernimmt.
/// </summary>
internal sealed class FormVoiceCommandContractLoader
{
    private readonly IMdaFormContractDataClient _formContractClient;
    private readonly ILogger<FormVoiceCommandContractLoader> _logger;

    public FormVoiceCommandContractLoader(
        IMdaFormContractDataClient formContractClient,
        ILogger<FormVoiceCommandContractLoader> logger)
    {
        _formContractClient = formContractClient ?? throw new ArgumentNullException(nameof(formContractClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FormContract?> LoadAsync(
        ApplicationDescriptor application,
        FormDescriptor selectedForm,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(selectedForm);

        if (string.IsNullOrWhiteSpace(selectedForm.LatestContractVersion))
        {
            _logger.LogDebug(
                "Form '{ApplicationId}/{FormId}' has no latestContractVersion.",
                application.ApplicationId,
                selectedForm.FormId);
            return null;
        }

        var contractVersion = selectedForm.LatestContractVersion.Trim();

        FormContract contract;
        try
        {
            contract = await _formContractClient.GetFormContractAsync(
                    application.ApplicationId,
                    selectedForm.FormId,
                    contractVersion,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex,
                "Failed to load form contract '{ApplicationId}/{FormId}/{ContractVersion}'.",
                application.ApplicationId,
                selectedForm.FormId,
                contractVersion);
            return null;
        }

        if (contract.Fields.Count == 0 || contract.FieldGroups.Count == 0)
        {
            _logger.LogDebug(
                "Form contract '{ApplicationId}/{FormId}/{ContractVersion}' contains no fields.",
                contract.ApplicationId,
                contract.FormId,
                contract.ContractVersion);
            return null;
        }

        return contract;
    }
}
