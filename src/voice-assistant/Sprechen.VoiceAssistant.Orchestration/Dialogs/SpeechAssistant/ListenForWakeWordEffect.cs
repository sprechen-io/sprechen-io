using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Startet die Erkennung des Aktivierungsworts in der ausgewählten Sprache.
/// Aufbau, Fehlerbehandlung und Ausführung übernimmt die
/// <see cref="SpeechAssistantWakeWordRuntime"/>.
/// </summary>
internal sealed class ListenForWakeWordEffect(string? selectedIsoCode)
    : IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>
{
    private readonly string? _selectedIsoCode = IsoCode.TryNormalizeIso6391(selectedIsoCode);

    public async ValueTask ExecuteAsync(
        SpeechAssistantContext context,
        IEventDispatcher<SpeechAssistantEvent> dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (string.IsNullOrWhiteSpace(_selectedIsoCode))
        {
            return;
        }

        await SpeechAssistantVoiceInputPauseTerminal
            .CompleteExpectedAsync(
                async pauseCancellationToken =>
                    await context.InteractionDependencies.WakeWordRuntime
                        .RunAsync(_selectedIsoCode, context, dispatcher, pauseCancellationToken)
                        .ConfigureAwait(false),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
