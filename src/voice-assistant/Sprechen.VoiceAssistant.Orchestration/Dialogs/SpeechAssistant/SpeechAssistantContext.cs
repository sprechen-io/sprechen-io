using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.Speech.App.Contracts.Evaluation;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Interaktions-, Einrichtungs- und Dialogdienste sowie die Telemetrie
/// zur Auswertung als Ausführungskontext der Assistenteneffekte.
/// </summary>
internal sealed class SpeechAssistantContext
{
    [ActivatorUtilitiesConstructor]
    public SpeechAssistantContext(
        SpeechAssistantInteractionDependencies interactionServices,
        SpeechAssistantSetupDependencies setupServices,
        SpeechAssistantDialogDependencies dialogServices,
        IEvaluationSessionTelemetry evaluationSessionTelemetry)
    {
        InteractionDependencies = interactionServices ?? throw new ArgumentNullException(nameof(interactionServices));
        SetupDependencies = setupServices ?? throw new ArgumentNullException(nameof(setupServices));
        DialogDependencies = dialogServices ?? throw new ArgumentNullException(nameof(dialogServices));
        EvaluationSessionTelemetry = evaluationSessionTelemetry ?? throw new ArgumentNullException(nameof(evaluationSessionTelemetry));
    }

    public SpeechAssistantInteractionDependencies InteractionDependencies { get; }

    public SpeechAssistantSetupDependencies SetupDependencies { get; }

    public SpeechAssistantDialogDependencies DialogDependencies { get; }

    public IEvaluationSessionTelemetry EvaluationSessionTelemetry { get; }
}
