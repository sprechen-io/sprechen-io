namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Erfasst die Aktivierungsphase vor Beginn einer Formularsitzung.
/// Die Ereignisse verwenden den modellierten Kontext für diese Phase und benötigen
/// keine <see cref="EvaluationSession"/>.
/// </summary>
public interface IEvaluationWakeWordTelemetry
{
    /// <summary>
    /// Erfasst eine Phase eines Aktivierungsversuchs samt Fehlschlag oder Wiederherstellung.
    /// Mehrere Ereignisse können deshalb zu demselben Spracheingabeversuch gehören.
    /// </summary>
    void TrackWakeWordMiss(EvaluationWakeWordMiss miss);
}
