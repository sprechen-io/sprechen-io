namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Zeigt an, dass ein anderer Browserkontext dieselbe Evaluationsaufgabe bereits beansprucht.
/// </summary>
public sealed class EvaluationSessionAlreadyActiveException : Exception
{
    public EvaluationSessionAlreadyActiveException()
        : base("Another browser context already owns this evaluation task identity.")
    {
    }
}
