namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Gruppiert Rückmeldungsklänge nach ihrer Funktion für eine einheitliche Zuordnung und Anzeige.
/// </summary>
public enum EarconFamily
{
    Flow = 0,
    ValidationSuccess = 1,
    ValidationNotOk = 2,
    Structure = 3,
    Mode = 4,
}
