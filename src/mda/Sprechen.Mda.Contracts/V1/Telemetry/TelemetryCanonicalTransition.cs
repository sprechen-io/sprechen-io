using System.Globalization;

namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Eine kanonische fachliche Zustandsänderung. Die Objektkennung ist eine vorab vereinbarte Feld- oder Feldgruppen-Id und trägt keinen Inhalt: keine Beschriftungen, keine DOM-Selektoren, keine eingegebenen oder erwarteten Werte, keine Transkripte. Diese Beschränkung hält Inhaltsdaten aus der Schrittaufzeichnung heraus.
/// </summary>
/// <param name="CanonicalClass">Welche der fünf Änderungsklassen das ist.</param>
/// <param name="Scope">Ob die Änderung ein Feld oder eine Feldgruppe betrifft.</param>
/// <param name="ObjectId">Die vereinbarte Kennung des Feldes oder der Feldgruppe.</param>
/// <param name="ResultingState">Der Zustand, in dem die Änderung das Objekt lässt.</param>
public sealed record TelemetryCanonicalTransition(
    string CanonicalClass,
    string Scope,
    string ObjectId,
    string ResultingState)
{
    // Der Einheitentrenner muss in allen Schlüsselbestandteilen ausgeschlossen sein,
    // damit unterschiedliche Übergänge nicht denselben zusammengesetzten Schlüssel erhalten.
    private const char KeySeparator = '\u001F';

    /// <summary>
    /// Zusammengesetzter Schlüssel zur Deduplizierung eines Übergangs. Gleiche Schlüssel
    /// werden bei der Zählung zusammengefasst. Die Zustandsversion unterscheidet eine spätere
    /// Rückkehr zum selben Ziel von einem erneut aufgezeichneten Übergang.
    /// </summary>
    public static string BuildDeduplicationKey(
        string participantId,
        string applicationId,
        string condition,
        string taskId,
        int stateVersion,
        TelemetryCanonicalTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);

        return string.Join(
            KeySeparator,
            participantId,
            applicationId,
            condition,
            taskId,
            stateVersion.ToString(CultureInfo.InvariantCulture),
            transition.CanonicalClass,
            transition.Scope,
            transition.ObjectId,
            transition.ResultingState);
    }
}
