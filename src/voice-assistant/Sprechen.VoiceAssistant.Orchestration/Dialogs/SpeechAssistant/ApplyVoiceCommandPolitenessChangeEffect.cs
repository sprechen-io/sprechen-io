using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Wendet eine geänderte Anrede über den Einstellungsablauf an und meldet Erfolg oder Fehler an den
/// Reducer, der den weiteren Dialog bestimmt.
/// </summary>
internal sealed class ApplyVoiceCommandPolitenessChangeEffect(PolitenessLevel targetPolitenessLevel, string? correlationId = null)
    : IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>
{
    private readonly string? _correlationId = NormalizeOptional(correlationId);
    private readonly PolitenessLevel _targetPolitenessLevel = targetPolitenessLevel;

    public async ValueTask ExecuteAsync(
        SpeechAssistantContext context,
        IEventDispatcher<SpeechAssistantEvent> dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatcher);

        var resolvedCorrelationId = ResolveCorrelationId();
        await context.InteractionDependencies.SpeechAssistantTrace
            .WriteIfConfiguredAsync(
                resolvedCorrelationId,
                SpeechAssistantTraceScope.PolitenessChangeEffect,
                SpeechAssistantTraceLevel.Information,
                $"Applying in-session politeness change to level={_targetPolitenessLevel}.",
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var settings = await context.SetupDependencies.UserLanguageSettingsWorkflow
                .SaveSelectedPolitenessAsync(_targetPolitenessLevel, cancellationToken)
                .ConfigureAwait(false);
            var selectedPolitenessLevel = settings.SelectedPolitenessLevel
                ?? throw new InvalidOperationException("Expected workflow to persist a politeness selection.");

            await context.InteractionDependencies.SpeechAssistantTrace
                .WriteIfConfiguredAsync(
                    resolvedCorrelationId,
                    SpeechAssistantTraceScope.PolitenessChangeEffect,
                    SpeechAssistantTraceLevel.Information,
                    $"In-session politeness change persisted as level={selectedPolitenessLevel}.",
                    cancellationToken)
                .ConfigureAwait(false);
            await dispatcher.DispatchAsync(
                    new SpeechAssistantVoiceCommandPolitenessChangedEvent(selectedPolitenessLevel),
                    DispatchMode.Enqueue,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            context.InteractionDependencies.Logger.LogDebug(ex, "Applying in-session politeness change failed.");
            await context.InteractionDependencies.SpeechAssistantTrace
                .WriteIfConfiguredAsync(
                    resolvedCorrelationId,
                    SpeechAssistantTraceScope.PolitenessChangeEffect,
                    SpeechAssistantTraceLevel.Error,
                    "Applying in-session politeness change failed.",
                    SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            await context.InteractionDependencies.SpeechInteractionSignals
                .ReportFailureAsync(SpeechErrorCode.VoiceCommandPolitenessChangeFailed, "Applying in-session politeness change failed.")
                .ConfigureAwait(false);

            await dispatcher.DispatchAsync(
                    new SpeechAssistantVoiceCommandPolitenessChangeFailedEvent(),
                    DispatchMode.Enqueue,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private string ResolveCorrelationId()
        => string.IsNullOrWhiteSpace(_correlationId)
            ? Guid.NewGuid().ToString("N")
            : _correlationId;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
