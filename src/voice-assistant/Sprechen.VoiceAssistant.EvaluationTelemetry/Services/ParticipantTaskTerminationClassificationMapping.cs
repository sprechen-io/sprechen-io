using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Evaluation;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Übersetzt die vier Abschlussarten eines Teilnehmendenlaufs zwischen Laufzeit-Aufzählung und Modellwerten.
/// </summary>
internal static class ParticipantTaskTerminationClassificationMapping
{
    internal static string ToModelValue(this ParticipantTaskTerminationClassification classification)
        => classification switch
        {
            ParticipantTaskTerminationClassification.Completed =>
                TelemetryTaskTerminationClassifications.Completed,
            ParticipantTaskTerminationClassification.ParticipantCancelled =>
                TelemetryTaskTerminationClassifications.ParticipantCancelled,
            ParticipantTaskTerminationClassification.SessionAbandoned =>
                TelemetryTaskTerminationClassifications.SessionAbandoned,
            ParticipantTaskTerminationClassification.TechnicalFailure =>
                TelemetryTaskTerminationClassifications.TechnicalFailure,
            _ => throw new ArgumentOutOfRangeException(
                nameof(classification),
                classification,
                "Unsupported participant task termination classification."),
        };

    internal static ParticipantTaskTerminationClassification ParseParticipantTaskTerminationClassification(
        this string modelValue)
        => modelValue switch
        {
            TelemetryTaskTerminationClassifications.Completed =>
                ParticipantTaskTerminationClassification.Completed,
            TelemetryTaskTerminationClassifications.ParticipantCancelled =>
                ParticipantTaskTerminationClassification.ParticipantCancelled,
            TelemetryTaskTerminationClassifications.SessionAbandoned =>
                ParticipantTaskTerminationClassification.SessionAbandoned,
            TelemetryTaskTerminationClassifications.TechnicalFailure =>
                ParticipantTaskTerminationClassification.TechnicalFailure,
            _ => throw new ArgumentOutOfRangeException(
                nameof(modelValue),
                modelValue,
                "The model value is not a participant task termination classification."),
        };
}
