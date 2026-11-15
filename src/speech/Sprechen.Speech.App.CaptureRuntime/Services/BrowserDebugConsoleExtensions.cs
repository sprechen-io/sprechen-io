namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Schreibt optionale Browserdiagnosen, ohne auf ihren Abschluss zu warten,
/// und fängt ihre Fehler ab, damit die Aufnahme fortgesetzt werden kann.
/// </summary>
internal static class BrowserDebugConsoleExtensions
{
    internal static void WriteOptional(
        this IBrowserDebugConsole browserDebugConsole,
        string scope,
        string message)
    {
        ArgumentNullException.ThrowIfNull(browserDebugConsole);

        try
        {
            var write = browserDebugConsole.WriteAsync(scope, message, CancellationToken.None);
            if (!write.IsCompletedSuccessfully)
            {
                _ = ObserveAsync(write);
            }
        }
        catch
        {
            // Ein Fehler der optionalen Browserdiagnose darf die Aufnahme nicht unterbrechen.
        }
    }

    private static async Task ObserveAsync(ValueTask write)
    {
        try
        {
            await write.ConfigureAwait(false);
        }
        catch
        {
            // Optionale Diagnosefehler beobachten und verwerfen.
        }
    }
}
