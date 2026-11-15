using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal static class FormsCatalogDialogTestFactory
{
    public static FormsCatalogDialog Create(
        ISpeechInteractionSignals interactionSignals,
        ISpeechInputClient speechInputClient,
        ITextToSpeechPlayer textToSpeechPlayer,
        IFormsCatalogDataClient formsCatalogDataClient,
        IFormsCatalogIntentParser intentParser,
        IUserLanguageState userLanguageState)
    {
        ArgumentNullException.ThrowIfNull(interactionSignals);
        ArgumentNullException.ThrowIfNull(speechInputClient);
        ArgumentNullException.ThrowIfNull(textToSpeechPlayer);
        ArgumentNullException.ThrowIfNull(formsCatalogDataClient);
        ArgumentNullException.ThrowIfNull(intentParser);
        ArgumentNullException.ThrowIfNull(userLanguageState);

        var modeAwareSpeechCaptureClient = speechInputClient as IModeAwareSpeechCaptureClient
            ?? throw new InvalidOperationException("FormsCatalogDialog tests require a mode-aware speech input double.");

        return new FormsCatalogDialog(
            new SelectionDialogSpeechTurnRunner(
                interactionSignals,
                speechInputClient,
                modeAwareSpeechCaptureClient,
                new SpeechRecognitionWorker(speechInputClient),
                textToSpeechPlayer,
                NullLogger<SelectionDialogSpeechTurnRunner>.Instance),
            formsCatalogDataClient,
            intentParser,
            new FormsCatalogLanguageChangeWorkflow(
                CreateUserLanguageSelectionWriter(userLanguageState),
                new LanguageChangeTargetResolver(VoiceInterfaceLanguageCatalog.CreateDefault())),
            userLanguageState,
            Mock.Of<ISpeechAssistantTrace>());
    }

    private static IUserLanguageSelectionWriter CreateUserLanguageSelectionWriter(IUserLanguageState userLanguageState)
    {
        ArgumentNullException.ThrowIfNull(userLanguageState);

        return new InMemoryUserLanguageSettingsWorkflow(
            userLanguageState,
            new InMemoryUserLanguageSettingsStore());
    }
}
