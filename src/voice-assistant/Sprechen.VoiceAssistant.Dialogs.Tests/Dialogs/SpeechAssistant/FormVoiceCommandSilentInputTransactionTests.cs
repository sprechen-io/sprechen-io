using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.Infrastructure;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandSilentInputTransactionTests
{
    [Fact]
    public async Task ExecuteAsync_Replace_AppliesOnFreshRuntimeAndMergesSession()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var otherForm = new SpeechAssistantFormSessionState(
            ImmutableDictionary.Create<string, string>(StringComparer.Ordinal).Add("kept", "yes"),
            IsSubmitted: false);
        var session = new SpeechAssistantApplicationSessionState(
            ImmutableDictionary.Create<string, SpeechAssistantFormSessionState>(StringComparer.OrdinalIgnoreCase)
                .Add("other", otherForm));
        var request = FormSilentInputTransactionRequest.Replace(
            "de",
            "app",
            "form",
            "nameField",
            SpeechAssistantSilentInputControlKind.Text,
            "Ada",
            session,
            ImmutableDictionary<string, string>.Empty);

        var transaction = CreateTransaction(interpreter, FormDialogRuntimeTestFixture.CreateContract());
        var result = await transaction.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(FormSilentInputResultKind.Applied, result.Kind);
        Assert.Equal("Ada", result.ApplicationSession!.FormsById["form"].ValuesByVariableId["name"]);
        Assert.Equal("yes", result.ApplicationSession.FormsById["other"].ValuesByVariableId["kept"]);
        Assert.Equal("Ada", (await interpreter.GetValuesSnapshotAsync())["name"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFormCannotBeLoaded_ReturnsStateChanged()
    {
        var loader = new Mock<IFormVoiceCommandContextLoader>(MockBehavior.Strict);
        loader
            .Setup(service => service.LoadAsync(
                "de",
                "app",
                "form",
                It.IsAny<SpeechAssistantApplicationSessionState>(),
                It.IsAny<ImmutableDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormVoiceCommandContextLoadResult.FromAbandoned(
                VoiceCommandDialogResult.AbandonedWithoutContext(ImmutableDictionary<string, string>.Empty)));
        var transaction = new FormVoiceCommandSilentInputTransaction(
            loader.Object,
            Mock.Of<IFormDialogRuntimeFactory>());
        var request = FormSilentInputTransactionRequest.Replace(
            "de",
            "app",
            "form",
            "nameField",
            SpeechAssistantSilentInputControlKind.Text,
            "Ada",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary<string, string>.Empty);

        var result = await transaction.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(FormSilentInputResultKind.StateChanged, result.Kind);
        Assert.NotNull(result.Snapshot);
        Assert.Null(result.Eligibility);
    }

    private static FormVoiceCommandSilentInputTransaction CreateTransaction(
        IFormDialogRuntime runtime,
        FormContract contract)
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            Description = new LocalizedText { ["de"] = "Formular" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };
        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "Anwendung" },
            Description = new LocalizedText { ["de"] = "Anwendung" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };
        var context = new FormVoiceCommandContext(
            new FormVoiceCommandSessionIdentity("de", "app", "form", []),
            new FormVoiceCommandSelectionContext(
                new VoiceDialogSettings(),
                application,
                ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase),
                form,
                contract),
            new FormVoiceCommandValueSnapshot(
                ImmutableDictionary<string, string>.Empty,
                []));

        var loader = new Mock<IFormVoiceCommandContextLoader>(MockBehavior.Strict);
        loader
            .Setup(service => service.LoadAsync(
                "de",
                "app",
                "form",
                It.IsAny<SpeechAssistantApplicationSessionState>(),
                It.IsAny<ImmutableDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormVoiceCommandContextLoadResult.FromContext(context));

        var factory = new Mock<IFormDialogRuntimeFactory>(MockBehavior.Strict);
        factory
            .Setup(service => service.Create(
                context,
                It.Is<string>(sessionId => sessionId.StartsWith("silent-input:", StringComparison.Ordinal)),
                It.IsAny<IFormRuleLoopGuardFeedback>()))
            .Returns(runtime);

        return new FormVoiceCommandSilentInputTransaction(loader.Object, factory.Object);
    }
}
