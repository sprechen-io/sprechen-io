namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Stabile Fehlercodes für deterministische Störungen des Formularinterpreters.
/// Die Bedeutung ist im Modell festgelegt. Sprach- und HTML-Kanal verwenden
/// dieselben Kennungen.
/// </summary>
public static class RuntimeErrorCodes
{
    /// <summary>
    /// Die Regelausführung hat ihr Iterationsbudget überschritten und den Zustand vor
    /// dem Ereignis wiederhergestellt. Der Fehler wird hörbar und über seinen festen
    /// Code in der Telemetrie gemeldet. Feldwerte gehören nicht in diese Meldung.
    /// </summary>
    public const string RuleLoopMaxIterationsExceeded = "E_RULE_LOOP_MAX_ITERS";
}
