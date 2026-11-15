namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Liefert das sprachabhängige Einrichtungsszenario, damit der Ablauf
/// seine Ansagen nicht selbst aus Modellressourcen zusammensetzen muss.
/// </summary>
internal interface IWakeWordSetupScenarioFactory
{
    Task<WakeWordSetupScenario?> CreateAsync(string isoCode, CancellationToken cancellationToken);
}

