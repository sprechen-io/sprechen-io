namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt die aktuelle UTC-Zeit über einen austauschbaren Vertrag für zeitabhängige Abläufe bereit.
/// </summary>
public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}



