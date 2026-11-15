using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Sammelt Überschreitungen der Regeliterationsgrenze während einer noch nicht übernommenen Transaktion. Meldungen werden erst nach der Übernahme weitergereicht, damit verworfene Zwischenzustände keine Rückmeldung auslösen.
/// </summary>
internal sealed class DeferredFormRuleLoopGuardFeedback : IFormRuleLoopGuardFeedback
{
    private readonly IFormRuleLoopGuardFeedback _publishedFeedback;
    private int _pendingBreachCount;

    internal DeferredFormRuleLoopGuardFeedback(IFormRuleLoopGuardFeedback publishedFeedback)
    {
        _publishedFeedback = publishedFeedback ?? throw new ArgumentNullException(nameof(publishedFeedback));
    }

    internal int PendingBreachCount => _pendingBreachCount;

    public void OnRuleLoopGuardBreach() => _pendingBreachCount++;

    /// <summary>
    /// Leert zuerst den Zähler und reicht die gesammelten Meldungen weiter. Ein erneuter Aufruf wiederholt sie nicht.
    /// </summary>
    internal void ReplayPendingBreaches()
    {
        var pending = _pendingBreachCount;
        _pendingBreachCount = 0;
        for (var breach = 0; breach < pending; breach++)
        {
            _publishedFeedback.OnRuleLoopGuardBreach();
        }
    }
}
