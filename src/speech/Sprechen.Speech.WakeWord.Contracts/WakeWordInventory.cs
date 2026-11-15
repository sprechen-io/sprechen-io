namespace Sprechen.Speech.WakeWord.Contracts;

using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Bündelt Aktivierungsformen, Erkennungsvarianten, Rückmeldesignale und Äußerungsfenster, damit Erkennung und hörbare Reaktion denselben Modellbestand nutzen.
/// </summary>
public sealed class WakeWordInventory
{
    public WakeWordInventory(
        IReadOnlyList<string> activationForms,
        IReadOnlyList<string> recognitionOnlyActivationForms)
        : this(activationForms, recognitionOnlyActivationForms, notRecognizedEarconId: null, wakeUtteranceWindow: null)
    {
    }

    public WakeWordInventory(
        IReadOnlyList<string> activationForms,
        IReadOnlyList<string> recognitionOnlyActivationForms,
        EarconId? notRecognizedEarconId)
        : this(activationForms, recognitionOnlyActivationForms, notRecognizedEarconId, wakeUtteranceWindow: null)
    {
    }

    public WakeWordInventory(
        IReadOnlyList<string> activationForms,
        IReadOnlyList<string> recognitionOnlyActivationForms,
        EarconId? notRecognizedEarconId,
        WakeUtteranceWindow? wakeUtteranceWindow)
    {
        ArgumentNullException.ThrowIfNull(activationForms);
        ArgumentNullException.ThrowIfNull(recognitionOnlyActivationForms);

        ActivationForms = activationForms;
        RecognitionOnlyActivationForms = recognitionOnlyActivationForms;
        NotRecognizedEarconId = notRecognizedEarconId;
        WakeUtteranceWindow = wakeUtteranceWindow;
    }

    public IReadOnlyList<string> ActivationForms { get; }

    public IReadOnlyList<string> RecognitionOnlyActivationForms { get; }

    public EarconId? NotRecognizedEarconId { get; }

    /// <summary>
    /// Optionaler Ton zur Bestätigung einer erfolgreichen Aktivierung.
    /// Ohne modelliertes Signal entfällt dieser Aktivierungston.
    /// </summary>
    public EarconId? ActivatedEarconId { get; init; }

    /// <summary>
    /// Optionales Modellfenster zum Zusammenfügen aufeinanderfolgender Aufnahmezyklen.
    /// </summary>
    public WakeUtteranceWindow? WakeUtteranceWindow { get; }
}
