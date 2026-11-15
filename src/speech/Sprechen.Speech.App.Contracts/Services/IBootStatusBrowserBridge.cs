namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Meldet dem Browser die Bereitschaft der Anwendung zum Abschluss der Startanzeige.
/// </summary>
public interface IBootStatusBrowserBridge
{
    Task MarkApplicationReadyAsync();
}
