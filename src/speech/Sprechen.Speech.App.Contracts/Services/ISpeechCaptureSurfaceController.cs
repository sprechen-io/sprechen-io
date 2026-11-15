namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt die Druckaktion der Aufnahmeoberfläche als Einstieg in die Aufnahmesteuerung bereit.
/// </summary>
public interface ISpeechCaptureSurfaceController
{
    ValueTask HandlePressAsync(CancellationToken cancellationToken);
}
