namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

internal sealed class FakeWakeArbitrator : IWakeArbitrator
{
    private readonly Func<WakeArbitrationInput, WakeArbitrationOutcome> _decide;

    public FakeWakeArbitrator(Func<WakeArbitrationInput, WakeArbitrationOutcome> decide)
    {
        _decide = decide ?? throw new ArgumentNullException(nameof(decide));
    }

    public WakeArbitrationOutcome Arbitrate(WakeArbitrationInput input) => _decide(input);
}
