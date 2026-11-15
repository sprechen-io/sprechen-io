namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Kapselt die Verarbeitung einer Druckaktion für Aufrufer der Aufnahmeoberfläche.
/// </summary>
public interface ISpeechCaptureSurfacePressController
{
    ValueTask HandlePressAsync(CancellationToken cancellationToken);
}
