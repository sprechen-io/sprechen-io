using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Verteilt jede Dialogentscheidung an alle Diagnosesenken, damit der Ausfall
/// einer Senke weder die übrigen Empfänger noch den Dialogzug unterbricht.
/// </summary>
internal sealed class CompositeDialogDecisionSink : IDialogDecisionSink
{
    private readonly IDialogDecisionSink[] _sinks;

    public CompositeDialogDecisionSink(params IDialogDecisionSink[] sinks)
    {
        ArgumentNullException.ThrowIfNull(sinks);
        if (sinks.Length == 0)
        {
            throw new ArgumentException("At least one sink is required.", nameof(sinks));
        }

        _sinks = sinks;
    }

    public async ValueTask RecordAsync(DialogDecision decision, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);

        foreach (var sink in _sinks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await sink.RecordAsync(decision, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Ein Diagnosefehler darf weder den Dialogzug noch die übrigen Diagnosesenken unterbrechen.
            }
        }
    }
}
