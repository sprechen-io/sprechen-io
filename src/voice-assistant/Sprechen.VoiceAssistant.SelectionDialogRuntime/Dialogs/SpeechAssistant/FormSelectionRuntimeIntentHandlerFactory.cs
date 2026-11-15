namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bindet Formularvergleich, modellierte Ansagen und Auswahlübergang an die gemeinsame Intent-
/// Behandlung. Befehle müssen dadurch nicht nochmals formularspezifisch implementiert werden.
/// </summary>
internal sealed class FormSelectionRuntimeIntentHandlerFactory
{
    private readonly IFormSelectionMatcher _matcher;
    private readonly FormSelectionTransitionPolicy _transitionPolicy;

    public FormSelectionRuntimeIntentHandlerFactory(
        IFormSelectionMatcher matcher,
        FormSelectionTransitionPolicy transitionPolicy)
    {
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
        _transitionPolicy = transitionPolicy ?? throw new ArgumentNullException(nameof(transitionPolicy));
    }

    public SelectionDialogCommonIntentHandler<FormInfo> Create(FormSelectionDialogSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new SelectionDialogCommonIntentHandler<FormInfo>(
            matcher: _matcher.CandidateMatcher,
            buildListPrompt: candidates => FormSelectionPromptBuilder.GetListPrompt(
                session.PromptContext,
                candidates,
                session.IsoCode,
                session.PolitenessLevel),
            buildAmbiguityPrompt: candidates => FormSelectionPromptBuilder.GetAmbiguityPrompt(
                session.PromptContext,
                candidates,
                session.IsoCode,
                session.PolitenessLevel),
            buildCancelPrompt: () => FormSelectionPromptBuilder.GetCancelPrompt(
                session.PromptContext,
                session.IsoCode,
                session.PolitenessLevel),
            buildHelpPrompt: () => FormSelectionPromptBuilder.GetHelpPrompt(
                session.PromptContext,
                session.IsoCode,
                session.PolitenessLevel),
            buildInvalidNumberPrompt: max => FormSelectionPromptBuilder.GetInvalidNumberPrompt(
                session.PromptContext,
                max,
                session.IsoCode,
                session.PolitenessLevel),
            buildNoMatchPrompt: name => FormSelectionPromptBuilder.GetNoMatchPrompt(
                session.PromptContext,
                name,
                session.IsoCode,
                session.PolitenessLevel),
            transitionAfterSelectionAsync: (selected, resolutionKind, ct) =>
                _transitionPolicy.EvaluateAsync(selected, resolutionKind, session.TransitionContext, ct));
    }
}
