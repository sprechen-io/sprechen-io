using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Führt die Anwendungsauswahl mit einem optionalen vorliegenden Transkript aus und meldet Auswahl
/// oder Abbruch an den Reducer zurück.
/// </summary>
internal sealed class RunFormsCatalogDialogEffect : IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>
{
    private const string FailureMessage = "Application catalog dialog failed.";
    private readonly string? _correlationId;
    private readonly SpeechTranscript? _initialTranscript;
    private readonly string? _selectedIsoCode;

    public RunFormsCatalogDialogEffect(string? selectedIsoCode, string? correlationId = null)
    {
        _correlationId = NormalizeOptional(correlationId);
        _selectedIsoCode = IsoCode.TryNormalizeIso6391(selectedIsoCode);
    }

    public RunFormsCatalogDialogEffect(string? selectedIsoCode, string initialTranscript, string? correlationId = null)
        : this(selectedIsoCode, new SpeechTranscript(initialTranscript, ConfidencePercent: null), correlationId)
    {
    }

    public RunFormsCatalogDialogEffect(
        string? selectedIsoCode,
        SpeechTranscript initialTranscript,
        string? correlationId = null)
    {
        _correlationId = NormalizeOptional(correlationId);
        _selectedIsoCode = IsoCode.TryNormalizeIso6391(selectedIsoCode);
        _initialTranscript = NormalizeInitialTranscript(initialTranscript);
    }

    /// <summary>
    /// Gibt an, ob ein Anfangstranskript vorliegt. Die Einstiegsregel entscheidet über
    /// seine Verwendung. Ohne angehängten Befehl startet die Aktivierung den Katalogdialog
    /// ohne ein solches Transkript.
    /// </summary>
    internal bool HasInitialTranscript => !string.IsNullOrWhiteSpace(_initialTranscript?.Text);

    internal int? InitialTranscriptConfidencePercent => _initialTranscript?.ConfidencePercent;

    public async ValueTask ExecuteAsync(
        SpeechAssistantContext context,
        IEventDispatcher<SpeechAssistantEvent> dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatcher);

        var correlationId = ResolveCorrelationId();
        await context.InteractionDependencies.SpeechAssistantTrace
            .WriteIfConfiguredAsync(
                correlationId,
                SpeechAssistantTraceScope.FormsCatalogEffect,
                SpeechAssistantTraceLevel.Information,
                _initialTranscript is null
                    ? "Starting application catalog dialog."
                    : "Starting application catalog dialog with prefetched command transcript.",
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await SpeechAssistantVoiceInputPauseTerminal.CompleteExpectedAsync(async _ =>
            {
                if (string.IsNullOrWhiteSpace(_selectedIsoCode))
                {
                    throw new InvalidOperationException("Application catalog dialog requires a valid selected ISO code.");
                }

                var result = _initialTranscript is null
                    ? await context.DialogDependencies.FormsCatalogDialog.ExecuteAsync(_selectedIsoCode, correlationId, cancellationToken)
                        .ConfigureAwait(false)
                    : await context.DialogDependencies.FormsCatalogDialog.ExecuteAsync(
                            _selectedIsoCode,
                            correlationId,
                            _initialTranscript,
                            cancellationToken)
                        .ConfigureAwait(false);

                if (result.WasCancelled || string.IsNullOrWhiteSpace(result.SelectedApplicationId))
                {
                    await context.InteractionDependencies.SpeechAssistantTrace
                        .WriteIfConfiguredAsync(
                            correlationId,
                            SpeechAssistantTraceScope.FormsCatalogEffect,
                            SpeechAssistantTraceLevel.Information,
                            $"Application catalog dialog cancelled. reason={result.CancellationReason?.ToString() ?? "<missing>"}",
                            cancellationToken)
                        .ConfigureAwait(false);
                    await dispatcher.DispatchAsync(
                            new SpeechAssistantApplicationSelectionCancelledEvent(result.EffectiveIsoCode),
                            DispatchMode.Enqueue,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await context.InteractionDependencies.SpeechAssistantTrace
                        .WriteIfConfiguredAsync(
                            correlationId,
                            SpeechAssistantTraceScope.FormsCatalogEffect,
                            SpeechAssistantTraceLevel.Information,
                            $"Application catalog dialog selected applicationId={result.SelectedApplicationId} formCount={result.FormCount?.ToString() ?? "1"}",
                            cancellationToken)
                        .ConfigureAwait(false);
                    await dispatcher.DispatchAsync(
                        new SpeechAssistantApplicationSelectedEvent(
                            result.SelectedApplicationId,
                            result.SelectedApplicationName,
                            result.FormCount ?? 1,
                            result.DefaultFormId,
                            result.EffectiveIsoCode),
                        DispatchMode.Enqueue,
                        cancellationToken).ConfigureAwait(false);
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
                    correlationId,
                    SpeechAssistantTraceScope.FormsCatalogEffect,
                    SpeechAssistantTraceLevel.Warning,
                    "Application catalog dialog was interrupted before completion.",
                    SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            await dispatcher.DispatchAsync(
                    new SpeechAssistantApplicationSelectionCancelledEvent(),
                    DispatchMode.Enqueue,
                    SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            context.InteractionDependencies.Logger.LogDebug(ex, FailureMessage);
            await context.InteractionDependencies.SpeechAssistantTrace
                .WriteIfConfiguredAsync(
                    correlationId,
                    SpeechAssistantTraceScope.FormsCatalogEffect,
                    SpeechAssistantTraceLevel.Error,
                    FailureMessage,
                    SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            await context.InteractionDependencies.SpeechInteractionSignals
                .ReportFailureAsync(SpeechErrorCode.ApplicationCatalogDialogFailed, FailureMessage)
                .ConfigureAwait(false);
            await dispatcher.DispatchAsync(
                    new SpeechAssistantApplicationSelectionCancelledEvent(),
                    DispatchMode.Enqueue,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static SpeechTranscript NormalizeInitialTranscript(SpeechTranscript transcript)
    {
        ArgumentNullException.ThrowIfNull(transcript);
        if (string.IsNullOrWhiteSpace(transcript.Text))
        {
            throw new ArgumentException("Initial transcript is required.", nameof(transcript));
        }

        return transcript with { Text = transcript.Text.Trim() };
    }

    private string ResolveCorrelationId()
        => string.IsNullOrWhiteSpace(_correlationId)
            ? Guid.NewGuid().ToString("N")
            : _correlationId;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
