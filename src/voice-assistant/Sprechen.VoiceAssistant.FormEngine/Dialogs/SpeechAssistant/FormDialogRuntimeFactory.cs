using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.App.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Erzeugt einen Formularlauf aus Vertragskontext, Sitzungskennung und Transkriptparser.
/// Übernimmt außerdem die Rückmeldung für den Schutz vor endlosen Regelabläufen.
/// </summary>
internal sealed class FormDialogRuntimeFactory : IFormDialogRuntimeFactory
{
    private readonly IFormTranscriptParser _transcriptParser;

    public FormDialogRuntimeFactory(IFormTranscriptParser transcriptParser)
    {
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
    }

    public IFormDialogRuntime Create(
        IFormDialogRuntimeContext context,
        string sessionId,
        IFormRuleLoopGuardFeedback loopGuardFeedback)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(loopGuardFeedback);

        return new FormInterpreterRuntimeOwner(
            context.Contract,
            context.IsoCode,
            context.ValuesByVariableId,
            _transcriptParser,
            context.VoiceSettings.GetPolitenessLevel(),
            sessionId.Trim(),
            loopGuardFeedback);
    }
}


