using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Korrelationskennung, Aktivierungswortschatz und Kombination der
/// Erkennungsergebnisse. Liefert deren Eingabeform für die weitere Verarbeitung.
/// </summary>
internal sealed class WakeArbitrationInputGuard
{
    private readonly WakeArbitrationInputShapeReader _shapeReader;
    private readonly WakeArbitrationInputConflictValidator _conflictValidator;
    private readonly AcousticWakeWordEvidenceGuard _acousticEvidenceGuard;

    public WakeArbitrationInputGuard()
        : this(
            new WakeArbitrationInputShapeReader(),
            new WakeArbitrationInputConflictValidator(),
            new AcousticWakeWordEvidenceGuard())
    {
    }

    internal WakeArbitrationInputGuard(
        WakeArbitrationInputShapeReader shapeReader,
        WakeArbitrationInputConflictValidator conflictValidator,
        AcousticWakeWordEvidenceGuard acousticEvidenceGuard)
    {
        _shapeReader = shapeReader ?? throw new ArgumentNullException(nameof(shapeReader));
        _conflictValidator = conflictValidator ?? throw new ArgumentNullException(nameof(conflictValidator));
        _acousticEvidenceGuard = acousticEvidenceGuard ?? throw new ArgumentNullException(nameof(acousticEvidenceGuard));
    }

    public WakeArbitrationInputShape Validate(WakeArbitrationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.CorrelationId))
        {
            throw new ArgumentException(
                "WakeArbitrationInput.CorrelationId must be a non-empty string.",
                nameof(input));
        }

        ArgumentNullException.ThrowIfNull(input.Inventory);

        var shape = _shapeReader.Read(input);
        _conflictValidator.Validate(shape);
        _acousticEvidenceGuard.Validate(input, shape);
        return shape;
    }
}
