using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.Infrastructure;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormSilentInputApplicatorTests
{
    [Fact]
    public async Task ApplySilentInputAsync_Replace_CommitsValueAndReturnsSameRuntimeObservation()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var request = FormSilentInputRequest.Replace(
            "de",
            "app",
            "form",
            "nameField",
            SpeechAssistantSilentInputControlKind.Text,
            "Ada");

        var result = await interpreter.SilentInput.ApplySilentInputAsync(request, CancellationToken.None);

        Assert.Equal(FormSilentInputResultKind.Applied, result.Kind);
        Assert.False(result.CanonicalSame);
        Assert.Equal("Ada", result.ApplicationSession!.FormsById["form"].ValuesByVariableId["name"]);
        Assert.Equal("Ada", (await interpreter.GetValuesSnapshotAsync())["name"]);
        Assert.NotNull(result.Snapshot);
        Assert.Equal("nameField", result.Eligibility!.FieldId);
        Assert.Equal(SpeechAssistantSilentInputControlKind.Text, result.Eligibility.ControlKind);
    }

    [Fact]
    public async Task ApplySilentInputAsync_ReplaceSameValue_MarksCanonicalSame()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter(
            ImmutableDictionary.Create<string, string>(StringComparer.Ordinal).Add("name", "Ada"));
        var request = FormSilentInputRequest.Replace(
            "de",
            "app",
            "form",
            "nameField",
            SpeechAssistantSilentInputControlKind.Text,
            "Ada");

        var result = await interpreter.SilentInput.ApplySilentInputAsync(request, CancellationToken.None);

        Assert.Equal(FormSilentInputResultKind.Applied, result.Kind);
        Assert.True(result.CanonicalSame);
    }

    [Fact]
    public async Task ApplySilentInputAsync_Clear_RemovesStoredValue()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter(
            ImmutableDictionary.Create<string, string>(StringComparer.Ordinal).Add("name", "Ada"));
        var request = FormSilentInputRequest.Clear(
            "de",
            "app",
            "form",
            "nameField",
            SpeechAssistantSilentInputControlKind.Text);

        var result = await interpreter.SilentInput.ApplySilentInputAsync(request, CancellationToken.None);

        Assert.Equal(FormSilentInputResultKind.Applied, result.Kind);
        Assert.False(result.ApplicationSession!.FormsById["form"].ValuesByVariableId.ContainsKey("name"));
        Assert.False((await interpreter.GetValuesSnapshotAsync()).ContainsKey("name"));
    }

    [Fact]
    public async Task ApplySilentInputAsync_WrongField_ReturnsStateChangedWithoutWriting()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var request = FormSilentInputRequest.Replace(
            "de",
            "app",
            "form",
            "cityField",
            SpeechAssistantSilentInputControlKind.Text,
            "Berlin");

        var result = await interpreter.SilentInput.ApplySilentInputAsync(request, CancellationToken.None);

        Assert.Equal(FormSilentInputResultKind.StateChanged, result.Kind);
        Assert.NotNull(result.Snapshot);
        Assert.False((await interpreter.GetValuesSnapshotAsync()).ContainsKey("city"));
        Assert.False((await interpreter.GetValuesSnapshotAsync()).ContainsKey("name"));
    }

    [Fact]
    public async Task ApplySilentInputAsync_RequiredViolation_RejectsAndRestoresPreviousValue()
    {
        var contract = FormDialogRuntimeTestFixture.CreateContract();
        contract.Constraints =
        [
            new ConstraintDefinition
            {
                ConstraintId = "name-required",
                Kind = ConstraintKind.Required,
                Code = "required",
                Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("nameField") }
            }
        ];
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.Ordinal).Add("name", "Ada"));
        var request = FormSilentInputRequest.Clear(
            "de",
            "app",
            "form",
            "nameField",
            SpeechAssistantSilentInputControlKind.Text);

        var result = await interpreter.SilentInput.ApplySilentInputAsync(request, CancellationToken.None);

        Assert.Equal(FormSilentInputResultKind.Rejected, result.Kind);
        Assert.Equal(FormSilentInputRepairKind.Required, result.RepairKind);
        Assert.Equal("Ada", (await interpreter.GetValuesSnapshotAsync())["name"]);
    }

    [Fact]
    public void Replace_WithoutValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => FormSilentInputRequest.Replace(
            "de",
            "app",
            "form",
            "nameField",
            SpeechAssistantSilentInputControlKind.Text,
            "   "));
    }
}
