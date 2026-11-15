namespace Sprechen.Speech.State.Tests.DialogHostExamples;

internal sealed record WizardFieldDefinition(
    WizardFieldId FieldId,
    IReadOnlyDictionary<string, string> Labels);


