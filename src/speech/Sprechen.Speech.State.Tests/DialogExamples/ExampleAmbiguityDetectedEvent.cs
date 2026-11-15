namespace Sprechen.Speech.State.Tests.DialogExamples;

internal sealed record ExampleAmbiguityDetectedEvent(
    ExampleFieldId Field,
    string OptionOne,
    string OptionTwo) : ExampleDialogEvent;


