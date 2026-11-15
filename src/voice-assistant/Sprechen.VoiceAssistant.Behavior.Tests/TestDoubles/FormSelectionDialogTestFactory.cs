using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal static class FormSelectionDialogTestFactory
{
    public static FormSelectionDialog Create(
        ISpeechInteractionSignals interactionSignals,
        ISpeechInputClient speechInputClient,
        ITextToSpeechPlayer textToSpeechPlayer,
        IFormsCatalogDataClient formsCatalogDataClient,
        IFormsCatalogIntentParser intentParser,
        IFormSelectionMatcher matcher,
        IUserLanguageState userLanguageState,
        ISpeechAssistantTrace? speechAssistantTrace = null)
    {
        ArgumentNullException.ThrowIfNull(interactionSignals);
        ArgumentNullException.ThrowIfNull(speechInputClient);
        ArgumentNullException.ThrowIfNull(textToSpeechPlayer);
        ArgumentNullException.ThrowIfNull(formsCatalogDataClient);
        ArgumentNullException.ThrowIfNull(intentParser);
        ArgumentNullException.ThrowIfNull(matcher);
        ArgumentNullException.ThrowIfNull(userLanguageState);

        var modeAwareSpeechCaptureClient = speechInputClient as IModeAwareSpeechCaptureClient
            ?? throw new InvalidOperationException("FormSelectionDialog tests require a mode-aware speech input double.");

        return new FormSelectionDialog(
            new SelectionDialogSpeechTurnRunner(
                interactionSignals,
                speechInputClient,
                modeAwareSpeechCaptureClient,
                new SpeechRecognitionWorker(speechInputClient),
                textToSpeechPlayer,
                NullLogger<SelectionDialogSpeechTurnRunner>.Instance),
            formsCatalogDataClient,
            intentParser,
            matcher,
            userLanguageState,
            speechAssistantTrace ?? NullSpeechAssistantTrace.Instance);
    }
}
