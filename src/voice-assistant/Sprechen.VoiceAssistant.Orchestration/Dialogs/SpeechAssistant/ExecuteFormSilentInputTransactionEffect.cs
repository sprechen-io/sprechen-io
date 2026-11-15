using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Führt eine stille Feldtransaktion aus und meldet ihr Ergebnis mit der Auftragskennung zurück,
/// damit der Reducer verspätete Ergebnisse zuordnen kann.
/// </summary>
internal sealed class ExecuteFormSilentInputTransactionEffect
    : IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>
{
    private readonly string _operationId;
    private readonly FormSilentInputTransactionRequest _request;

    public ExecuteFormSilentInputTransactionEffect(
        string operationId,
        FormSilentInputTransactionRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        _operationId = operationId;
        _request = request ?? throw new ArgumentNullException(nameof(request));
    }

    public async ValueTask ExecuteAsync(
        SpeechAssistantContext context,
        IEventDispatcher<SpeechAssistantEvent> dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatcher);

        var result = await context.DialogDependencies.SilentInputTransaction
            .ExecuteAsync(_request, cancellationToken)
            .ConfigureAwait(false);
        await dispatcher
            .DispatchAsync(
                new SpeechAssistantSilentInputTransactionCompletedEvent(_operationId, result),
                DispatchMode.Enqueue,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
