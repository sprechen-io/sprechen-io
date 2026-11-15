namespace Sprechen.Speech.Runtime.Tests.Testing;

internal static class SpeechInputTestTimeouts
{
    // Der zeitliche Spielraum soll kurze Schedulerverzögerungen bei parallelen Tests abfangen.
    public static readonly TimeSpan SchedulerSafeNoInputTimeout = TimeSpan.FromMilliseconds(250);
}

