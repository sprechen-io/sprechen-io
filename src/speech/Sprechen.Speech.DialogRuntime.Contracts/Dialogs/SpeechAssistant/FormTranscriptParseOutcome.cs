namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ausgang eines Transkript-Parses: kanonischer Wert plus optionale angenommene Konvention,
/// oder ein typisierter Fehlschlag, damit Erfolg und Fehler nicht dieselbe leere Form teilen.
/// </summary>
public sealed record FormTranscriptParseOutcome
{
    private FormTranscriptParseOutcome(
        bool success,
        string? canonicalValue,
        string? assumedInterpretation,
        FormTranscriptParseFailure? failure)
    {
        if (success)
        {
            ArgumentException.ThrowIfNullOrEmpty(canonicalValue);
            if (failure is not null)
            {
                throw new ArgumentException("A successful parse outcome must not carry a failure.", nameof(failure));
            }
        }
        else
        {
            ArgumentNullException.ThrowIfNull(failure);
            if (canonicalValue is not null)
            {
                throw new ArgumentException("A failed parse outcome must not carry a canonical value.", nameof(canonicalValue));
            }

            if (assumedInterpretation is not null)
            {
                throw new ArgumentException(
                    "A failed parse outcome must not carry an assumed interpretation.",
                    nameof(assumedInterpretation));
            }
        }

        Success = success;
        CanonicalValue = canonicalValue;
        AssumedInterpretation = assumedInterpretation;
        Failure = failure;
    }

    public bool Success { get; }

    public string? CanonicalValue { get; }

    // Gesetzt, wenn ein erfolgreicher Parse eine deterministische Konvention anwenden musste, die
    // nicht ausdrücklich genannt wurde, etwa halb drei als 02:30. Eine knappe
    // maschinenlesbare Notiz, aus der die Rückmeldeschicht einen Hinweis formuliert.
    public string? AssumedInterpretation { get; }

    public FormTranscriptParseFailure? Failure { get; }

    public static FormTranscriptParseOutcome Succeeded(string canonicalValue) => Succeeded(canonicalValue, null);

    public static FormTranscriptParseOutcome Succeeded(string canonicalValue, string? assumedInterpretation)
        => new(true, canonicalValue, assumedInterpretation, null);

    public static FormTranscriptParseOutcome Failed(FormTranscriptParseFailure failure) => new(false, null, null, failure);
}
