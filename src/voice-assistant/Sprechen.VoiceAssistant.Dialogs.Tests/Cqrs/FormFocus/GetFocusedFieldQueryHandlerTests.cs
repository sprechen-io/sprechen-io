using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.Infrastructure;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormFocus;

public sealed class GetFocusedFieldQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsSnapshotForCurrentlyFocusedField()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new GetFocusedFieldQueryHandler(interpreter.Services.FocusedFieldSnapshotBuilder);

        var result = await handler.HandleAsync(new GetFocusedFieldQuery());

        Assert.Equal("nameField", result.FieldId);
        Assert.Equal("name", result.VariableId);
    }
}
