namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Nach der Datenschutzentscheidung aufbereitete Sprechform eines gespeicherten kanonischen Werts. Ansagen bauen
/// Sätze aus dieser Nutzlast. Die Bestätigung darf <see cref="SpokenValue"/> danach
/// phonetisch formatieren.
/// </summary>
/// <param name="Status">Der vollständige Projektionsstatus.</param>
/// <param name="SpokenValue">
/// Wörter zum Sprechen für <see cref="FormSpokenValueProjectionStatus.FullValue"/>,
/// <see cref="FormSpokenValueProjectionStatus.Empty"/>,
/// <see cref="FormSpokenValueProjectionStatus.Masked"/> und
/// <see cref="FormSpokenValueProjectionStatus.Unavailable"/>. <c>null</c> wenn zurückgehalten.
/// </param>
/// <param name="ValueResolvedFromOption">
/// Wahr, wenn jedes gespeicherte Codesegment zu einer Wahloption aufgelöst wurde. Die
/// Bestätigung nutzt das, um das phonetische Buchstabieren von Optionslabels zu überspringen.
/// </param>
public sealed record FormSpokenValueProjection(
    FormSpokenValueProjectionStatus Status,
    string? SpokenValue,
    bool ValueResolvedFromOption);
