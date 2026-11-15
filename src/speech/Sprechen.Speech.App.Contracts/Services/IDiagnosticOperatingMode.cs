namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Sitzungsbezogener Diagnosemodus mit Sitzungs- und Korrelationskennung.</summary>
/// <remarks>
/// Der Modus wird aus der Route bestimmt und nicht dauerhaft auf dem Gerät gespeichert.
/// Ein Studienpfad allein aktiviert ihn nicht. Die Aufzeichnung der Studientelemetrie
/// wird davon getrennt gesteuert.
/// </remarks>
public interface IDiagnosticOperatingMode
{
    bool IsDiagnostic { get; }

    string SessionId { get; }

    string ClientCorrelationId { get; }

    string OperatingModeMarker { get; }

    void Activate(bool isDiagnostic);
}
