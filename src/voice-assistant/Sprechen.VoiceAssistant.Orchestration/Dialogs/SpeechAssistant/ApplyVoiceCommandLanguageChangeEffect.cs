using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Wendet einen Sprachwechsel über den Einstellungsablauf an und meldet das Ergebnis an den
/// Reducer, damit dieser den Dialog fortsetzen oder zur Aktivierungsbereitschaft zurückkehren kann.
/// </summary>
internal sealed class ApplyVoiceCommandLanguageChangeEffect(string targetIsoCode, string? correlationId = null)
    : IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>
{
    private readonly string? _correlationId = NormalizeOptional(correlationId);
    private readonly string _targetIsoCode = IsoCode.TryNormalizeIso6391(targetIsoCode)
        ?? throw new ArgumentException("Expected ISO 639-1 language code.", nameof(targetIsoCode));

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
                SpeechAssistantTraceScope.LanguageChangeEffect,
                SpeechAssistantTraceLevel.Information,
                $"Applying in-session language change to isoCode={_targetIsoCode}.",
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var settings = await context.SetupDependencies.UserLanguageSettingsWorkflow
                .SaveSelectedLanguageAsync(_targetIsoCode, cancellationToken)
                .ConfigureAwait(false);
            var normalizedIsoCode = IsoCode.TryNormalizeIso6391(settings.SelectedIsoCode)
                ?? throw new InvalidOperationException("Expected workflow to persist a normalized ISO 639-1 language code.");

            await context.InteractionDependencies.SpeechAssistantTrace
                .WriteIfConfiguredAsync(
                    resolvedCorrelationId,
                    SpeechAssistantTraceScope.LanguageChangeEffect,
                    SpeechAssistantTraceLevel.Information,
                    $"In-session language change persisted as isoCode={normalizedIsoCode}.",
                    cancellationToken)
                .ConfigureAwait(false);
            await dispatcher.DispatchAsync(
                    new SpeechAssistantVoiceCommandLanguageChangedEvent(normalizedIsoCode),
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
            context.InteractionDependencies.Logger.LogDebug(ex, "Applying in-session language change failed.");
            await context.InteractionDependencies.SpeechAssistantTrace
                .WriteIfConfiguredAsync(
                    resolvedCorrelationId,
                    SpeechAssistantTraceScope.LanguageChangeEffect,
                    SpeechAssistantTraceLevel.Error,
                    "Applying in-session language change failed.",
                    SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            await context.InteractionDependencies.SpeechInteractionSignals
                .ReportFailureAsync(SpeechErrorCode.VoiceCommandLanguageChangeFailed, "Applying in-session language change failed.")
                .ConfigureAwait(false);

            await dispatcher.DispatchAsync(
                    new SpeechAssistantVoiceCommandLanguageChangeFailedEvent(),
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
