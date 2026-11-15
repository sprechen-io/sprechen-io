namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis der Verfügbarkeitsprüfung eines Eingabemodus am Feld:
/// erlaubt oder mit typisiertem Grund nicht verfügbar.
/// </summary>
public sealed record FormFieldInputModeAvailability(
    FormFieldInputModeAvailabilityStatus Status,
    FormFieldInputModeUnavailabilityReason? UnavailabilityReason)
{
    public static readonly FormFieldInputModeAvailability Allowed =
        new(FormFieldInputModeAvailabilityStatus.Available, null);

    public static FormFieldInputModeAvailability Denied(FormFieldInputModeUnavailabilityReason reason)
        => new(FormFieldInputModeAvailabilityStatus.Unavailable, reason);
}
