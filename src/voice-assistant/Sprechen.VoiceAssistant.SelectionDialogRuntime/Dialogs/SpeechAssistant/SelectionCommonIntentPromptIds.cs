namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet gemeinsamen Auswahlansagen ihre Modellschlüssel zu. Alle Eigenschaften
/// müssen beim Initialisieren zugewiesen werden. Beim Erzeugen der gemeinsamen
/// Ansagefunktionen werden zusätzlich leere Werte abgewiesen.
/// </summary>
internal sealed record SelectionCommonIntentPromptIds
{
    internal required string Confirmation { get; init; }

    internal required string LowConfidenceConfirm { get; init; }

    internal required string ExplicitAuthorization { get; init; }

    internal required string VerificationUnavailable { get; init; }

    internal required string InitialConfirmationHelp { get; init; }

    internal required string ExplicitAuthorizationHelp { get; init; }

    internal required string ConfidenceFaultRetryHelp { get; init; }

    internal required string NotOpenedReselect { get; init; }

    internal required string VerificationFailedTerminal { get; init; }

    internal required string RepairExhaustedTerminal { get; init; }

    internal required string CaptureFailedTerminal { get; init; }

    internal required string ConfirmationRepeat { get; init; }

    internal required string ListIntro { get; init; }

    internal required string ListItem { get; init; }

    internal required string Launch { get; init; }

    internal required string Help { get; init; }

    internal required string Cancelled { get; init; }

    internal required string Unrecognized { get; init; }

    internal required string NoInput { get; init; }

    internal required string NoItems { get; init; }

    internal required string NoMatch { get; init; }

    internal required string InvalidNumber { get; init; }

    /// <summary>
    /// Weist leere Prompt-Schlüssel beim Erzeugen des Prompt-Bausteins ab. <c>required</c>
    /// erzwingt die Zuweisung im Quelltext, prüft aber weder ihren Inhalt noch das Vorhandensein
    /// der Vorlage im Modell.
    /// </summary>
    internal void RequireEveryId()
    {
        foreach (var (slot, id) in Slots())
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException($"Prompt-Id für '{slot}' fehlt oder ist leer.", slot);
            }
        }
    }

    private IEnumerable<(string Slot, string Id)> Slots()
    {
        yield return (nameof(Confirmation), Confirmation);
        yield return (nameof(LowConfidenceConfirm), LowConfidenceConfirm);
        yield return (nameof(ExplicitAuthorization), ExplicitAuthorization);
        yield return (nameof(VerificationUnavailable), VerificationUnavailable);
        yield return (nameof(InitialConfirmationHelp), InitialConfirmationHelp);
        yield return (nameof(ExplicitAuthorizationHelp), ExplicitAuthorizationHelp);
        yield return (nameof(ConfidenceFaultRetryHelp), ConfidenceFaultRetryHelp);
        yield return (nameof(NotOpenedReselect), NotOpenedReselect);
        yield return (nameof(VerificationFailedTerminal), VerificationFailedTerminal);
        yield return (nameof(RepairExhaustedTerminal), RepairExhaustedTerminal);
        yield return (nameof(CaptureFailedTerminal), CaptureFailedTerminal);
        yield return (nameof(ConfirmationRepeat), ConfirmationRepeat);
        yield return (nameof(ListIntro), ListIntro);
        yield return (nameof(ListItem), ListItem);
        yield return (nameof(Launch), Launch);
        yield return (nameof(Help), Help);
        yield return (nameof(Cancelled), Cancelled);
        yield return (nameof(Unrecognized), Unrecognized);
        yield return (nameof(NoInput), NoInput);
        yield return (nameof(NoItems), NoItems);
        yield return (nameof(NoMatch), NoMatch);
        yield return (nameof(InvalidNumber), InvalidNumber);
    }
}
