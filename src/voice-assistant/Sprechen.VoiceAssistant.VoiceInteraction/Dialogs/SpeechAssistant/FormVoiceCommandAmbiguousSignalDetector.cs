namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, ob eine Mehrdeutigkeitsrückfrage zulässig ist, und ermittelt dafür
/// die Befehlsbezeichnung. Die aktuelle Standardprüfung lässt keine solche
/// Rückfrage zu.
/// </summary>
internal class FormVoiceCommandAmbiguousSignalDetector
{
    private readonly FormVoiceCommandAmbiguitySignalEligibility _eligibility;
    private readonly FormVoiceCommandAmbiguityLabelResolver _labelResolver;

    public FormVoiceCommandAmbiguousSignalDetector()
        : this(
            new FormVoiceCommandAmbiguitySignalEligibility(),
            new FormVoiceCommandAmbiguityLabelResolver())
    {
    }

    public FormVoiceCommandAmbiguousSignalDetector(
        FormVoiceCommandAmbiguitySignalEligibility eligibility,
        FormVoiceCommandAmbiguityLabelResolver labelResolver)
    {
        _eligibility = eligibility ?? throw new ArgumentNullException(nameof(eligibility));
        _labelResolver = labelResolver ?? throw new ArgumentNullException(nameof(labelResolver));
    }

    public virtual bool TryDetect(
        FormVoiceCommandContext context,
        FormVoiceCommandTranscriptInterpretation interpretation,
        out FormVoiceCommandAmbiguitySignal signal)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interpretation);

        signal = new FormVoiceCommandAmbiguitySignal(string.Empty);

        if (!_eligibility.IsEligible(context, interpretation))
        {
            return false;
        }

        signal = new FormVoiceCommandAmbiguitySignal(_labelResolver.Resolve(context, interpretation));
        return true;
    }
}
