namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Zum Vorlesen vorbereiteter Inhalt eines schreibgeschützten Anzeigefelds.
/// <c>FormReadOnlyFieldReadbackProjector</c> bereitet ihn in der Formularlaufzeit
/// nach den Datenschutzregeln auf, damit die Ausgabe keinen direkten Laufzeitzugriff benötigt.
/// </summary>
/// <param name="Label">Lokalisierte Feldbezeichnung.</param>
/// <param name="SpokenValue">Nach den Datenschutzregeln aufbereiteter Werttext.
/// Ist die Ausgabe im aktuellen Modus verboten, bleibt er null, etwa bei der automatischen
/// Ankündigung sensibler oder geheimer Felder. Rohe sensible oder geheime Werte dürfen
/// nicht in diesen Vertrag übernommen werden.</param>
/// <param name="Status">Vollständiger Projektionsstatus. Darf nicht auf eine bloße
/// Aussage wie <c>HasValue</c> reduziert werden.</param>
public sealed record FormReadOnlyFieldReadback(
    string Label,
    string? SpokenValue,
    FormSpokenValueProjectionStatus Status);
