using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Formatiert strukturierte Dialogentscheidungen für die Gerätespur,
/// damit Änderungen am lesbaren Text die strukturierten Diagnoseempfänger nicht beeinflussen.
/// </summary>
/// <remarks>
/// Die sprachliche Darstellung bleibt hier. Andere Beteiligte verarbeiten den Datensatz.
/// Ein Darstellungsfehler darf den Dialogzug nicht unterbrechen.
/// </remarks>
internal sealed class SpeechAssistantTraceDialogDecisionSink : IDialogDecisionSink
{
    private readonly ISpeechAssistantTrace _trace;
    private readonly string _correlationId;

    public SpeechAssistantTraceDialogDecisionSink(ISpeechAssistantTrace trace, string correlationId)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        _correlationId = correlationId;
    }

    public async ValueTask RecordAsync(DialogDecision decision, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);

        var fields = string.Join(' ', decision.Fields.Select(field => $"{field.Name}={field.Value}"));

        try
        {
            await _trace
                .WriteAsync(
                    _correlationId,
                    SpeechAssistantTraceScope.VoiceCommandEffect,
                    SpeechAssistantTraceLevel.Information,
                    $"decision {decision.Name} {fields}",
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Ein Diagnosefehler darf den Dialogzug nicht unterbrechen.
        }
    }
}
