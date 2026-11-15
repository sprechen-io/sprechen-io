
namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Sendet Auswertungsereignisse der Studien- und Auswertungstelemetrie.
/// </summary>
public interface IEvaluationTelemetry :
    IEvaluationSessionTelemetry,
    IEvaluationFormTelemetry,
    IEvaluationCommandTelemetry
{
}

