using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Optionale Laufzeitfähigkeit, die Werte für nicht fokussierte Felder in einer Äußerung
/// erkennt und ein einzelnes Angebot in zwei Schritten abwickelt: vorbereiten, dann übernehmen
/// oder verwerfen. Die strenge 1:1-Bindung am fokussierten Feld bleibt unberührt, und der Fokus
/// wandert durch diesen Weg nicht.
/// </summary>
public interface IFormDialogAdditionalFieldProposalRuntime
{
    /// <summary>
    /// Liefert ausdrücklich genannte Werte für andere als das fokussierte Feld.
    /// Ohne eindeutige Nennung eines zusätzlichen Feldes bleibt das Ergebnis leer.
    /// </summary>
    Task<ImmutableArray<FormAdditionalFieldValueCandidate>> ExtractAdditionalFieldCandidatesAsync(string transcript);

    /// <summary>
    /// Prüft Ziel, Datenschutz und Wert ohne jede Zustandsänderung und liefert bei Erfolg den
    /// sprechbaren Angebotstext samt Einmalberechtigung.
    /// </summary>
    Task<FormAdditionalFieldProposalPreparation> PrepareAdditionalFieldProposalAsync(
        FormAdditionalFieldValueCandidate candidate);

    /// <summary>
    /// Verbraucht die Berechtigung, prüft alle Voraussetzungen erneut und rechnet auf einer
    /// privaten Kopie. Nur ein gültiges Ergebnis wird als Ganzes veröffentlicht. Jeder andere
    /// Ausgang lässt Werte, Fokus und Zustandsstand unverändert.
    /// </summary>
    Task<FormAdditionalFieldValueAdoptionResult> CommitAdditionalFieldProposalAsync(
        FormAdditionalFieldProposalCommitToken commitToken);

    /// <summary>
    /// Verbraucht die Berechtigung ohne Übernahme. Der Aufruf ist wiederholbar und ändert
    /// weder Werte noch Zustandsstand, damit ein abgebrochener Dialog keine gültige
    /// Berechtigung zurücklässt.
    /// </summary>
    Task AbandonAdditionalFieldProposalAsync(FormAdditionalFieldProposalCommitToken commitToken);
}
