using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Erkannter Wert für ein anderes Feld, das die Person während des Fokus auf einem
/// fremden Feld ausdrücklich genannt hat. Wird getrennt zum freiwilligen Übernehmen
/// angeboten und nie still automatisch geschrieben.
/// </summary>
/// <param name="FieldId">Typisierte Kennung des nicht fokussierten Felds, für das der Wert genannt wurde.</param>
/// <param name="FieldLabel">Aufgelöste, sichtbare Beschriftung dieses Felds.</param>
/// <param name="ValueSegment">Gesprochener Werttext, der zum Werttyp des Felds parst und beim Annehmen den geprüften Feldpfad läuft.</param>
/// <param name="CanonicalValue">Geparste Speicherform von <paramref name="ValueSegment"/>.</param>
/// <param name="ValueTypeKind">Gebundener Werttyp, aus dem das Angebot projiziert wird.</param>
/// <param name="Options">Auswahloptionen bei Aufzählung oder Menge, sonst leer.</param>
public sealed record FormAdditionalFieldValueCandidate(
    FieldId FieldId,
    string FieldLabel,
    string ValueSegment,
    string CanonicalValue,
    string ValueTypeKind,
    ImmutableArray<FormChoiceOption> Options);
