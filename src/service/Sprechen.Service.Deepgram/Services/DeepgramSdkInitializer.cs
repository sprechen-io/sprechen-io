using Deepgram;
using Deepgram.Logger;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Startet die SDK-Initialisierung höchstens einmal und deaktiviert dabei das SDK-Logging.
/// </summary>
internal static class DeepgramSdkInitializer
{
    private static int _initialized;

    public static void EnsureInitialized()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0)
        {
            return;
        }

        Library.Initialize(LogLevel.Disable, filename: null);
    }
}