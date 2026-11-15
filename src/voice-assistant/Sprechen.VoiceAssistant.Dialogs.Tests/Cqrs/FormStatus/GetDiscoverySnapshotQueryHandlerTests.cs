using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.Infrastructure;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormStatus;

public sealed class GetDiscoverySnapshotQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsDiscoverySnapshotForCurrentForm()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new GetDiscoverySnapshotQueryHandler(
            interpreter.Services.DiscoveryProjectionService,
            interpreter.Services.FocusedFieldSnapshotBuilder,
            interpreter.Services.VisibilityService);

        var result = await handler.HandleAsync(new GetDiscoverySnapshotQuery());

        Assert.NotNull(result);
        Assert.Equal("main", result.CurrentFieldGroupId);
        Assert.NotEmpty(result.FieldGroups);
        Assert.NotEmpty(result.CurrentFieldGroupFields);
    }

    [Fact]
    public async Task HandleAsync_FieldsContainFocusedAndNonFocused()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new GetDiscoverySnapshotQueryHandler(
            interpreter.Services.DiscoveryProjectionService,
            interpreter.Services.FocusedFieldSnapshotBuilder,
            interpreter.Services.VisibilityService);

        var result = await handler.HandleAsync(new GetDiscoverySnapshotQuery());

        Assert.Contains(result.CurrentFieldGroupFields, f => f.FieldId == "nameField");
        Assert.Contains(result.CurrentFieldGroupFields, f => f.FieldId == "cityField");
    }
}
