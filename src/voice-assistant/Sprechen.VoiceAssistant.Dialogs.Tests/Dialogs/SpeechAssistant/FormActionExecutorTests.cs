using System.Text.Json;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormActionExecutorTests
{
    #region SetVisible - Field

    [Fact]
    public void ApplyAction_SetVisibleField_False_HidesField()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = CreateSetVisibleFieldAction("firstName", false);

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.False(state.IsFieldVisible("firstName"));
    }

    [Fact]
    public void ApplyAction_SetVisibleField_True_AfterHidden_ShowsField()
    {
        var state = CreateState();
        state.SetFieldVisible("firstName", false);
        var executor = CreateExecutor(state);
        var action = CreateSetVisibleFieldAction("firstName", true);

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.True(state.IsFieldVisible("firstName"));
    }

    [Fact]
    public void ApplyAction_SetVisibleField_SameValue_ReturnsFalse()
    {
        var state = CreateState();
        state.SetFieldVisible("firstName", true);
        var executor = CreateExecutor(state);
        var action = CreateSetVisibleFieldAction("firstName", true);

        var changed = executor.ApplyAction(action, []);

        Assert.False(changed);
    }

    [Fact]
    public void ApplyAction_SetVisibleField_MissingFieldId_ReturnsFalse()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = JsonDocument.Parse("""
        {
            "kind": "setVisible",
            "target": { "kind": "field" },
            "value": true
        }
        """).RootElement;

        var changed = executor.ApplyAction(action, []);

        Assert.False(changed);
    }

    #endregion

    #region SetVisible - FieldGroup

    [Fact]
    public void ApplyAction_SetVisibleFieldGroup_False_HidesFieldGroup()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = CreateSetVisibleFieldGroupAction("personalInfo", false);

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.False(state.IsFieldGroupVisible("personalInfo"));
    }

    [Fact]
    public void ApplyAction_SetVisibleFieldGroup_True_AfterHidden_ShowsFieldGroup()
    {
        var state = CreateState();
        state.SetFieldGroupVisible("personalInfo", false);
        var executor = CreateExecutor(state);
        var action = CreateSetVisibleFieldGroupAction("personalInfo", true);

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.True(state.IsFieldGroupVisible("personalInfo"));
    }

    #endregion

    #region SetEnabled

    [Fact]
    public void ApplyAction_SetEnabled_True_EnablesField()
    {
        var state = CreateState();
        state.SetFieldEnabled("email", false);
        var executor = CreateExecutor(state);
        var action = CreateSetEnabledAction("email", true);

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.True(state.IsFieldEnabled("email"));
    }

    [Fact]
    public void ApplyAction_SetEnabled_False_DisablesField()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = CreateSetEnabledAction("email", false);

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.False(state.IsFieldEnabled("email"));
    }

    [Fact]
    public void ApplyAction_SetEnabled_SameValue_ReturnsFalse()
    {
        var state = CreateState();
        state.SetFieldEnabled("email", true);
        var executor = CreateExecutor(state);
        var action = CreateSetEnabledAction("email", true);

        var changed = executor.ApplyAction(action, []);

        Assert.False(changed);
    }

    #endregion

    #region SetReadOnly

    [Fact]
    public void ApplyAction_SetReadOnly_True_SetsFieldReadOnly()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = CreateSetReadOnlyAction("computed", true);

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.True(state.IsFieldReadOnly("computed"));
    }

    [Fact]
    public void ApplyAction_SetReadOnly_False_SetsFieldEditable()
    {
        var state = CreateState();
        state.SetFieldReadOnly("computed", true);
        var executor = CreateExecutor(state);
        var action = CreateSetReadOnlyAction("computed", false);

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.False(state.IsFieldReadOnly("computed"));
    }

    #endregion

    #region SetValue

    [Fact]
    public void ApplyAction_SetValue_String_SetsVariableValue()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = CreateSetValueAction("greeting", "\"Hello\"");

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.Equal("Hello", state.GetVariableValue("greeting"));
    }

    [Fact]
    public void ApplyAction_SetValue_Integer_SetsVariableValue()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = CreateSetValueAction("count", "42");

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.Equal("42", state.GetVariableValue("count"));
    }

    [Fact]
    public void ApplyAction_SetValue_Boolean_SetsVariableValue()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = CreateSetValueAction("agreed", "true");

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.Equal("true", state.GetVariableValue("agreed"));
    }

    [Fact]
    public void ApplyAction_SetValue_SameValue_ReturnsFalse()
    {
        var state = CreateState();
        state.SetVariableValue("greeting", "Hello");
        var executor = CreateExecutor(state);
        var action = CreateSetValueAction("greeting", "\"Hello\"");

        var changed = executor.ApplyAction(action, []);

        Assert.False(changed);
    }

    #endregion

    #region ClearValue

    [Fact]
    public void ApplyAction_ClearValue_RemovesVariable()
    {
        var state = CreateState();
        state.SetVariableValue("temp", "value");
        var executor = CreateExecutor(state);
        var action = CreateClearValueAction("temp");

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.Null(state.GetVariableValue("temp"));
    }

    [Fact]
    public void ApplyAction_ClearValue_NonExistent_ReturnsFalse()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = CreateClearValueAction("nonexistent");

        var changed = executor.ApplyAction(action, []);

        Assert.False(changed);
    }

    #endregion

    #region CopyValue

    [Fact]
    public void ApplyAction_CopyValue_CopiesSourceToTarget()
    {
        var state = CreateState();
        state.SetVariableValue("source", "original");
        var executor = CreateExecutor(state);
        var action = CreateCopyValueAction("source", "target");

        var changed = executor.ApplyAction(action, []);

        Assert.True(changed);
        Assert.Equal("original", state.GetVariableValue("target"));
    }

    [Fact]
    public void ApplyAction_CopyValue_SourceNotExists_ReturnsFalse()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = CreateCopyValueAction("nonexistent", "target");

        var changed = executor.ApplyAction(action, []);

        Assert.False(changed);
    }

    [Fact]
    public void ApplyAction_CopyValue_SameValue_ReturnsFalse()
    {
        var state = CreateState();
        state.SetVariableValue("source", "value");
        state.SetVariableValue("target", "value");
        var executor = CreateExecutor(state);
        var action = CreateCopyValueAction("source", "target");

        var changed = executor.ApplyAction(action, []);

        Assert.False(changed);
    }

    #endregion

    #region Speak

    [Fact]
    public void ApplyAction_Speak_AddsPromptIdToList()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = CreateSpeakAction("welcome-prompt");
        var promptIds = new List<string>();

        var changed = executor.ApplyAction(action, promptIds);

        Assert.False(changed); // Die Ansage ändert den Formularzustand nicht.
        Assert.Single(promptIds);
        Assert.Equal("welcome-prompt", promptIds[0]);
    }

    [Fact]
    public void ApplyAction_Speak_MissingPromptId_ReturnsFalse()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = JsonDocument.Parse("""{ "kind": "speak" }""").RootElement;
        var promptIds = new List<string>();

        var changed = executor.ApplyAction(action, promptIds);

        Assert.False(changed);
        Assert.Empty(promptIds);
    }

    #endregion

    #region Invalid Actions

    [Fact]
    public void ApplyAction_InvalidKind_ReturnsFalse()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = JsonDocument.Parse("""{ "kind": "unknownAction" }""").RootElement;

        var changed = executor.ApplyAction(action, []);

        Assert.False(changed);
    }

    [Fact]
    public void ApplyAction_MissingKind_ReturnsFalse()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = JsonDocument.Parse("""{ "value": true }""").RootElement;

        var changed = executor.ApplyAction(action, []);

        Assert.False(changed);
    }

    [Fact]
    public void ApplyAction_NotObject_ReturnsFalse()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = JsonDocument.Parse("\"string\"").RootElement;

        var changed = executor.ApplyAction(action, []);

        Assert.False(changed);
    }

    #endregion

    #region Test Helpers

    private static TestFormState CreateState() => new();

    private static FormActionExecutor CreateExecutor(TestFormState state)
        => new(state, state, state, state);

    private static JsonElement CreateSetVisibleFieldAction(string fieldId, bool visible)
    {
        var json = $$"""
        {
            "kind": "setVisible",
            "target": { "kind": "field", "fieldId": "{{fieldId}}" },
            "value": {{(visible ? "true" : "false")}}
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement CreateSetVisibleFieldGroupAction(string fieldGroupId, bool visible)
    {
        var json = $$"""
        {
            "kind": "setVisible",
            "target": { "kind": "fieldGroup", "fieldGroupId": "{{fieldGroupId}}" },
            "value": {{(visible ? "true" : "false")}}
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement CreateSetEnabledAction(string fieldId, bool enabled)
    {
        var json = $$"""
        {
            "kind": "setEnabled",
            "fieldId": "{{fieldId}}",
            "value": {{(enabled ? "true" : "false")}}
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement CreateSetReadOnlyAction(string fieldId, bool readOnly)
    {
        var json = $$"""
        {
            "kind": "setReadOnly",
            "fieldId": "{{fieldId}}",
            "value": {{(readOnly ? "true" : "false")}}
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement CreateSetValueAction(string variableId, string valueJson)
    {
        var json = $$"""
        {
            "kind": "setValue",
            "variableId": "{{variableId}}",
            "value": {{valueJson}}
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement CreateClearValueAction(string variableId)
    {
        var json = $$"""
        {
            "kind": "clearValue",
            "variableId": "{{variableId}}"
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement CreateCopyValueAction(string fromVariableId, string toVariableId)
    {
        var json = $$"""
        {
            "kind": "copyValue",
            "fromVariableId": "{{fromVariableId}}",
            "toVariableId": "{{toVariableId}}"
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement CreateSpeakAction(string promptId)
    {
        var json = $$"""
        {
            "kind": "speak",
            "promptId": "{{promptId}}"
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement CreateNavigateToFieldGroupAction(string fieldGroupId) =>
        JsonDocument.Parse($$"""{"kind": "navigateToFieldGroup", "fieldGroupId": "{{fieldGroupId}}"}""").RootElement;

    private static JsonElement CreateFocusFieldAction(string fieldId) =>
        JsonDocument.Parse($$"""{"kind": "focusField", "fieldId": "{{fieldId}}"}""").RootElement;

    #endregion

    #region NavigateToFieldGroup Tests

    [Fact]
    public void ApplyAction_NavigateToFieldGroup_CallsStateMethod()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = CreateNavigateToFieldGroupAction("section1");
        var prompts = new List<string>();

        var result = executor.ApplyAction(action, prompts);

        Assert.True(result);
        Assert.Equal("section1", state.LastNavigatedFieldGroupId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ApplyAction_NavigateToFieldGroup_InvalidFieldGroupId_ReturnsFalse(string fieldGroupId)
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var json = $$"""{"kind": "navigateToFieldGroup", "fieldGroupId": "{{fieldGroupId}}"}""";
        var action = JsonDocument.Parse(json).RootElement;
        var prompts = new List<string>();

        var result = executor.ApplyAction(action, prompts);

        Assert.False(result);
        Assert.Null(state.LastNavigatedFieldGroupId);
    }

    [Fact]
    public void ApplyAction_NavigateToFieldGroup_MissingFieldGroupId_ReturnsFalse()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = JsonDocument.Parse("""{"kind": "navigateToFieldGroup"}""").RootElement;
        var prompts = new List<string>();

        var result = executor.ApplyAction(action, prompts);

        Assert.False(result);
    }

    #endregion

    #region FocusField Tests

    [Fact]
    public void ApplyAction_FocusField_CallsStateMethod()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = CreateFocusFieldAction("field1");
        var prompts = new List<string>();

        var result = executor.ApplyAction(action, prompts);

        Assert.True(result);
        Assert.Equal("field1", state.LastFocusedFieldId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ApplyAction_FocusField_InvalidFieldId_ReturnsFalse(string fieldId)
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var json = $$"""{"kind": "focusField", "fieldId": "{{fieldId}}"}""";
        var action = JsonDocument.Parse(json).RootElement;
        var prompts = new List<string>();

        var result = executor.ApplyAction(action, prompts);

        Assert.False(result);
        Assert.Null(state.LastFocusedFieldId);
    }

    [Fact]
    public void ApplyAction_FocusField_MissingFieldId_ReturnsFalse()
    {
        var state = CreateState();
        var executor = CreateExecutor(state);
        var action = JsonDocument.Parse("""{"kind": "focusField"}""").RootElement;
        var prompts = new List<string>();

        var result = executor.ApplyAction(action, prompts);

        Assert.False(result);
    }

    #endregion

}
