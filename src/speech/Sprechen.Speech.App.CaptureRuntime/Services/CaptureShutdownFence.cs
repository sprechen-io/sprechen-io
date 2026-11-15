namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Begrenzt beim Freigeben die Wartezeit auf laufende Aufnahmen,
/// damit ausbleibende Browserrückrufe das Beenden der Sitzung nicht blockieren.
/// </summary>
internal static class CaptureShutdownFence
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    internal static TimeSpan ResolveTimeout(TimeSpan configuredTimeout)
        => configuredTimeout > TimeSpan.Zero ? configuredTimeout : DefaultTimeout;

    internal static async Task JoinAsync(Task[] captures, TimeSpan timeout)
    {
        try
        {
            await Task.WhenAll(captures).WaitAsync(timeout).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Der Abschluss jeder Aufnahme bleibt über ihre eigene Task beobachtbar.
            // Hier wird nur die gemeinsame Wartezeit beim Freigeben begrenzt.
        }
    }
}
