namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

internal sealed class NullScope : IDisposable
{
    public static NullScope Instance { get; } = new();

    public void Dispose()
    {
    }
}

