namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Feldnamen des Telemetrieumschlags aus telemetry-fragments.schema.json#/$defs/BaseEvent.
/// <see cref="TelemetryPayloadProperties"/> beschreibt dagegen Namen innerhalb von data.
/// Gemeinsame Konstanten halten Erzeugung und Empfang auf denselben Feldnamen.
/// </summary>
public static class TelemetryEnvelopeFieldNames
{
    /// <summary>
    /// Umschlagfeld der unveränderlichen Transport-Ereignis-ID.
    /// </summary>
    public const string EventId = "event_id";

    /// <summary>
    /// Umschlagfeld der Sitzungskennung einer tatsächlich begonnenen Aufgabe.
    /// </summary>
    public const string SessionId = "session_id";

    /// <summary>
    /// Umschlagfeld der monotonen Clientmillisekunden seit Aufgabenbeginn.
    /// </summary>
    public const string ClientOffsetMs = "client_offset_ms";

    /// <summary>
    /// Umschlagfeld der Teilnehmenden-ID.
    /// </summary>
    public const string ParticipantId = "participant_id";

    /// <summary>
    /// Umschlagfeld der Studienbedingung.
    /// </summary>
    public const string Condition = "condition";

    /// <summary>
    /// Umschlagfeld der Anwendungs-ID.
    /// </summary>
    public const string ApplicationId = "application_id";

    /// <summary>
    /// Umschlagfeld der Aufgaben-ID.
    /// </summary>
    public const string TaskId = "task_id";

    /// <summary>
    /// Umschlagfeld der Datensatz-Herkunftsklasse.
    /// </summary>
    public const string RecordProvenance = "record_provenance";

    /// <summary>
    /// Umschlagfeld des Ereignisnamens.
    /// </summary>
    public const string Event = "event";

    /// <summary>
    /// Umschlagfeld der monotonen Zustandsversion.
    /// </summary>
    public const string StateVersion = "state_version";

    /// <summary>
    /// Umschlagfeld des ereignisspezifischen Nutzlast-Objekts.
    /// </summary>
    public const string Data = "data";

    /// <summary>
    /// Alle Umschlagfeldnamen (<c>BaseEvent</c>) als Menge mit ordinalem Zeichenvergleich.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        EventId,
        SessionId,
        ClientOffsetMs,
        ParticipantId,
        Condition,
        ApplicationId,
        TaskId,
        RecordProvenance,
        Event,
        StateVersion,
        Data,
    };

    /// <summary>
    /// Pflicht-Umschlagfeldnamen aus <c>BaseEvent.required</c> mit ordinalem Zeichenvergleich.
    /// <c>data</c>, <c>session_id</c> und <c>client_offset_ms</c> sind keine allgemeinen Pflichtfelder.
    /// </summary>
    public static IReadOnlySet<string> Required { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        EventId,
        ParticipantId,
        Condition,
        ApplicationId,
        TaskId,
        RecordProvenance,
        Event,
        StateVersion,
    };
}
