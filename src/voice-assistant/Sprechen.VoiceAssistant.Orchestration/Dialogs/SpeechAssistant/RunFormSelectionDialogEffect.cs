using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Führt die Formularauswahl für eine Anwendung aus und übersetzt Ergebnis oder Fehler in
/// Ereignisse, über die der Reducer den Anschlussdialog bestimmt.
/// </summary>
internal sealed class RunFormSelectionDialogEffect(
    string? selectedIsoCode,
    string? selectedApplicationId,
    string? correlationId,
    ImmutableHashSet<string> completedFormIds)
    : IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>
{
    private const string FailureMessage = "Form selection dialog failed.";
    private readonly string? _correlationId = NormalizeOptional(correlationId);
    private readonly string? _selectedIsoCode = IsoCode.TryNormalizeIso6391(selectedIsoCode);
    private readonly string? _selectedApplicationId = string.IsNullOrWhiteSpace(selectedApplicationId) ? null : selectedApplicationId.Trim();
    private readonly ImmutableHashSet<string> _completedFormIds = completedFormIds ?? throw new ArgumentNullException(nameof(completedFormIds));

    public RunFormSelectionDialogEffect(
        string? selectedIsoCode,
        string? selectedApplicationId,
        ImmutableHashSet<string> completedFormIds)
        : this(selectedIsoCode, selectedApplicationId, correlationId: null, completedFormIds)
    {
    }

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
                SpeechAssistantTraceScope.FormSelectionEffect,
                SpeechAssistantTraceLevel.Information,
                $"Starting form selection dialog for applicationId={_selectedApplicationId ?? "<missing>"}",
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await SpeechAssistantVoiceInputPauseTerminal.CompleteExpectedAsync(async _ =>
            {
                if (string.IsNullOrWhiteSpace(_selectedIsoCode))
                {
                    throw new InvalidOperationException("Form selection dialog requires a valid selected ISO code.");
                }

                if (string.IsNullOrWhiteSpace(_selectedApplicationId))
                {
                    throw new InvalidOperationException("Form selection dialog requires a selected application identifier.");
                }

                var result = await context.DialogDependencies.FormSelectionDialog.ExecuteAsync(
                    _selectedIsoCode!,
                    _selectedApplicationId!,
                    resolvedCorrelationId,
                    _completedFormIds,
                    cancellationToken).ConfigureAwait(false);

                if (result.WasCancelled || string.IsNullOrWhiteSpace(result.SelectedFormId))
                {
                    await context.InteractionDependencies.SpeechAssistantTrace
                        .WriteIfConfiguredAsync(
                            resolvedCorrelationId,
                            SpeechAssistantTraceScope.FormSelectionEffect,
                            SpeechAssistantTraceLevel.Information,
                            $"Form selection dialog cancelled. reason={result.CancellationReason?.ToString() ?? "<missing>"}",
                            cancellationToken)
                        .ConfigureAwait(false);
                    await dispatcher.DispatchAsync(new SpeechAssistantFormSelectionCancelledEvent(), DispatchMode.Enqueue, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await context.InteractionDependencies.SpeechAssistantTrace
                        .WriteIfConfiguredAsync(
                            resolvedCorrelationId,
                            SpeechAssistantTraceScope.FormSelectionEffect,
                            SpeechAssistantTraceLevel.Information,
                            $"Form selection dialog selected formId={result.SelectedFormId}",
                            cancellationToken)
                        .ConfigureAwait(false);
                    await dispatcher.DispatchAsync(new SpeechAssistantFormSelectedEvent(FormId.From(result.SelectedFormId), result.SelectedFormName), DispatchMode.Enqueue, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            await context.InteractionDependencies.SpeechAssistantTrace
                .WriteIfConfiguredAsync(
                    resolvedCorrelationId,
                    SpeechAssistantTraceScope.FormSelectionEffect,
                    SpeechAssistantTraceLevel.Warning,
                    "Form selection dialog was interrupted before completion.",
                    SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            await dispatcher.DispatchAsync(
                    new SpeechAssistantFormSelectionCancelledEvent(),
                    DispatchMode.Enqueue,
                    SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            context.InteractionDependencies.Logger.LogDebug(ex, FailureMessage);
            await context.InteractionDependencies.SpeechAssistantTrace
                .WriteIfConfiguredAsync(
                    resolvedCorrelationId,
                    SpeechAssistantTraceScope.FormSelectionEffect,
                    SpeechAssistantTraceLevel.Error,
                    FailureMessage,
                    SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            await context.InteractionDependencies.SpeechInteractionSignals
                .ReportFailureAsync(SpeechErrorCode.FormSelectionDialogFailed, FailureMessage)
                .ConfigureAwait(false);
            await dispatcher.DispatchAsync(
                    new SpeechAssistantFormSelectionCancelledEvent(),
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
