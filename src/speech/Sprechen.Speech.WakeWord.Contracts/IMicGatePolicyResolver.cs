namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Löst die Mikrofonpolitik aus dem Aufnahmeprofil auf. Fehlt die Angabe,
/// wird <see cref="MicGatePolicy.Heuristic"/> verwendet.
/// </summary>
/// <remarks>
/// Die Modellauflösung liegt außerhalb der Vertragsassembly, damit Aufrufer
/// die Mikrofonpolitik ohne Abhängigkeit von deren Datenquelle abfragen können.
/// </remarks>
public interface IMicGatePolicyResolver
{
    MicGatePolicy GetPolicy();
}
