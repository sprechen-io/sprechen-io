using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandAllowBargeInHandlerTests
{
    private const string EnabledDe = "Unterbrechung ist jetzt aktiv. Sie können mich beim Sprechen unterbrechen.";
    private const string DisabledDe = "Unterbrechung ist jetzt aus. Ich spreche meine Ansagen zu Ende.";
    private const string AlreadyEnabledDe = "Unterbrechung ist bereits aktiv.";
    private const string AlreadyDisabledDe = "Unterbrechung ist bereits aus.";
    private const string EnabledEn = "Interruption is now on. You can interrupt me while I am speaking.";

    [Fact]
    public async Task HandleAsync_WhenOnCommand_PersistsDeviceOverrideAndAnnouncesEnabled()
    {
        var store = new Mock<IVoiceDialogSettingsOverrideStore>(MockBehavior.Strict);
        store
            .Setup(s => s.SetDeviceAllowBargeInOverrideAsync(true, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(EnabledDe, "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FormVoiceCommandAllowBargeInHandler(store.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.AllowBargeInOn, "de", new VoiceDialogSettings { AllowBargeIn = false });

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        store.VerifyAll();
        speechService.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenOffCommand_PersistsDeviceOverrideAndAnnouncesDisabled()
    {
        var store = new Mock<IVoiceDialogSettingsOverrideStore>(MockBehavior.Strict);
        store
            .Setup(s => s.SetDeviceAllowBargeInOverrideAsync(false, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(DisabledDe, "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FormVoiceCommandAllowBargeInHandler(store.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.AllowBargeInOff, "de", new VoiceDialogSettings { AllowBargeIn = true });

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        store.VerifyAll();
        speechService.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyEnabled_AcknowledgesAndDoesNotRewrite()
    {
        var store = new Mock<IVoiceDialogSettingsOverrideStore>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(AlreadyEnabledDe, "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FormVoiceCommandAllowBargeInHandler(store.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.AllowBargeInOn, "de", new VoiceDialogSettings { AllowBargeIn = true });

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        speechService.VerifyAll();
        store.Verify(
            s => s.SetDeviceAllowBargeInOverrideAsync(It.IsAny<bool?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyDisabled_AcknowledgesAndDoesNotRewrite()
    {
        var store = new Mock<IVoiceDialogSettingsOverrideStore>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(AlreadyDisabledDe, "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FormVoiceCommandAllowBargeInHandler(store.Object, speechService.Object);
        // Ohne gesetzten Wert gilt Unterbrechung als ausgeschaltet.
        var context = CreateContext(GlobalCommandIds.AllowBargeInOff, "de", new VoiceDialogSettings());

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        speechService.VerifyAll();
        store.Verify(
            s => s.SetDeviceAllowBargeInOverrideAsync(It.IsAny<bool?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenOnCommandInEnglish_AnnouncesEnabledInEnglish()
    {
        var store = new Mock<IVoiceDialogSettingsOverrideStore>(MockBehavior.Strict);
        store
            .Setup(s => s.SetDeviceAllowBargeInOverrideAsync(true, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(EnabledEn, "en", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FormVoiceCommandAllowBargeInHandler(store.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.AllowBargeInOn, "en", new VoiceDialogSettings { AllowBargeIn = false });

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        store.VerifyAll();
        speechService.VerifyAll();
    }

    private static FormVoiceCommandGlobalCommandContext CreateContext(
        string commandId,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        var contract = CreateContract(isoCode);
        var interpreter = new FormInterpreterRuntimeOwner(contract, requestedIsoCode: isoCode, ImmutableDictionary<string, string>.Empty);

        var form = new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { [isoCode] = "Formular" },
            Description = new LocalizedText { [isoCode] = "Formular" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { [isoCode] = "Anwendung" },
            Description = new LocalizedText { [isoCode] = "Anwendung" },
            SupportedLanguages = [isoCode],
            DefaultLanguage = isoCode,
            Forms = [form]
        };

        return new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: commandId,
                IsoCode: isoCode,
                Argument: null),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: voiceSettings,
                Application: application,
                CompletedFormIds: ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter,
                Session: new EvaluationSession("session", EvaluationChannel.Voice, "test-app", "form-1"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));
    }

    private static FormContract CreateContract(string isoCode)
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { [isoCode] = "Formular" },
            SupportedLanguages = [isoCode],
            DefaultLanguage = isoCode,
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { [isoCode] = "Name" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { [isoCode] = "Hauptteil" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }]
                }
            ]
        };
    }
}
