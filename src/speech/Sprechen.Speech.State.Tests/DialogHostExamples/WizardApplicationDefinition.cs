namespace Sprechen.Speech.State.Tests.DialogHostExamples;

internal sealed record WizardApplicationDefinition(
    string Id,
    IReadOnlyDictionary<string, string> Names,
    IReadOnlyList<WizardFieldDefinition> Fields);


