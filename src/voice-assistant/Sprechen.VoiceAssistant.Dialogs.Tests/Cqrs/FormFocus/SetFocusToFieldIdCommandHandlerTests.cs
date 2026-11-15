using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.Infrastructure;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormFocus;

public sealed class SetFocusToFieldIdCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_KnownField_FocusesField()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new SetFocusToFieldIdCommandHandler(interpreter.Services.Navigator);

        var result = await handler.HandleAsync(new SetFocusToFieldIdCommand(FieldId.From("cityField")));

        Assert.True(result);
        Assert.Equal("cityField", (await interpreter.GetFocusedFieldAsync()).FieldId);
    }

    [Fact]
    public async Task HandleAsync_UnknownField_ReturnsFalse()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new SetFocusToFieldIdCommandHandler(interpreter.Services.Navigator);

        var result = await handler.HandleAsync(new SetFocusToFieldIdCommand(FieldId.From("unknownField")));

        Assert.False(result);
    }

    [Fact]
    public async Task SetFocusToFieldId_BlankFieldId_ReturnsFalse()
    {
        // Ein leeres Sprungziel wird vor dem Erzeugen einer typisierten Feldkennung abgewiesen.
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();

        var result = await interpreter.TrySetFocusToFieldIdAsync("   ");

        Assert.False(result);
    }
}
