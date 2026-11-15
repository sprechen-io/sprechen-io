namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ausgang der Vorbereitung eines Übernahmeangebots. Nur <see cref="Prepared"/> darf
/// angeboten werden. Die übrigen Werte benennen, warum kein Angebot entsteht, damit der
/// Dialog den Grund sagen kann statt zu schweigen.
/// </summary>
public enum FormAdditionalFieldProposalPreparationStatus
{
    /// <summary>Das Feld ist beschreibbar, der Wert lesbar und das Angebot sprechbar.</summary>
    Prepared,

    /// <summary>Das Feld nimmt derzeit keine Spracheingabe an, etwa verborgen, gesperrt oder schreibgeschützt.</summary>
    Unavailable,

    /// <summary>Der genannte Wert ergibt keinen Wert des gebundenen Feldtyps.</summary>
    Invalid,

    /// <summary>Der Wert ist lesbar, verletzt aber eine Bedingung des Formulars.</summary>
    BlockedByValidation
}
