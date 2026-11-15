using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal static class FormMutableStateStoreTestFactory
{
    public static FormMutableStateStore Create(params string[] identifiers)
    {
        var ids = identifiers.Length > 0
            ? identifiers
            : [
                "name",
                "variable",
                "missing",
                "known",
                "var1",
                "var2",
                "visibleVar",
                "hiddenVar",
                "sharedVar",
                "field",
                "f1",
                "field1",
                "ibanField",
                "nameField",
                "visibleField",
                "hiddenField",
                "fieldA",
                "fieldB",
                "f2",
                "other",
                "city",
                "profile",
                "unrelated",
                "group",
                "s1",
                "main",
                "group1"
            ];

        return new FormMutableStateStore(
            ids.Select(VariableId.From).ToHashSet(),
            ids.Select(FieldId.From).ToHashSet(),
            ids.Select(FieldGroupId.From).ToHashSet());
    }
}
