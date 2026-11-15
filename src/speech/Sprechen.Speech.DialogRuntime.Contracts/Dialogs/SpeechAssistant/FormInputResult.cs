using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>Ergebnis der Anwendung eines Transkripts auf das fokussierte Feld.</summary>
/// <remarks>
/// Bei <c>ValueCaptured: false</c> soll <see cref="NotCapturedReason"/> den Grund angeben,
/// damit eine fehlgeschlagene Übernahme nicht nur als allgemeines Nein sichtbar wird.
/// Ohne ausdrückliche Angabe gilt <see cref="FormInputNotCapturedReason.None"/>.
/// </remarks>
public sealed record FormInputResult(
    bool ValueCaptured,
    ImmutableArray<FormConstraintViolation> Violations,
    FormTranscriptParseFailure? ParseFailure = null,
    FormInputNotCapturedReason NotCapturedReason = FormInputNotCapturedReason.None);



