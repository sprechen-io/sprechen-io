namespace Sprechen.ServiceDefaults;

/// <summary>
/// Stellt den Versionsstand der laufenden Anwendung bereit, damit Endpunkt und
/// Header unabhängig von dessen Auflösung darauf zugreifen können.
/// </summary>
public interface IApplicationVersionInfoProvider
{
    ApplicationVersionInfo GetCurrent();
}
