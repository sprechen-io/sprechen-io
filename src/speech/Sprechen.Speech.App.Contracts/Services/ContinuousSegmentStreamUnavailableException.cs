namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Kennzeichnet einen nicht verfügbaren kontinuierlichen Segmentstrom als technischen Fehler der Aufnahmequelle.
/// </summary>
public sealed class ContinuousSegmentStreamUnavailableException : Exception
{
    public ContinuousSegmentStreamUnavailableException(string message)
        : base(message)
    {
    }

    public ContinuousSegmentStreamUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
