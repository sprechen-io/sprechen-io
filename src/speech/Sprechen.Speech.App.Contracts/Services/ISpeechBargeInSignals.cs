namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verteilt Signale einer Sprachunterbrechung an asynchron reagierende Beobachter.
/// </summary>
public interface ISpeechBargeInSignals
{
    ValueTask PublishBargeInAsync();

    IDisposable SubscribeToBargeIn(Func<ValueTask> handler);
}
