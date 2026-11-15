using System.Collections.Immutable;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormNavigatorTests
{
    #region TryMoveFocusNext Tests

    [Fact]
    public void TryMoveFocusNext_FromFirstField_MovesToSecond()
    {
        var state = CreateStateWithFields("field1", "field2", "field3");
        state.SetCurrentFocus("field1");
        var navigator = new FormNavigator(state);

        var result = navigator.TryMoveFocusNext();

        Assert.True(result);
        Assert.Equal("field2", state.CurrentFocusedFieldId);
    }

    [Fact]
    public void TryMoveFocusNext_FromLastField_ReturnsFalse()
    {
        var state = CreateStateWithFields("field1", "field2", "field3");
        state.SetCurrentFocus("field3");
        var navigator = new FormNavigator(state);

        var result = navigator.TryMoveFocusNext();

        Assert.False(result);
        Assert.Equal("field3", state.CurrentFocusedFieldId);
    }

    [Fact]
    public void TryMoveFocusNext_SkipsHiddenFields()
    {
        var state = CreateStateWithFields("field1", "field2", "field3");
        state.SetCurrentFocus("field1");
        state.SetFieldAvailable("field2", false);
        var navigator = new FormNavigator(state);

        var result = navigator.TryMoveFocusNext();

        Assert.True(result);
        Assert.Equal("field3", state.CurrentFocusedFieldId);
    }

    [Fact]
    public void TryMoveFocusNext_NoFocus_ReturnsFalse()
    {
        var state = CreateStateWithFields("field1", "field2");
        var navigator = new FormNavigator(state);

        var result = navigator.TryMoveFocusNext();

        Assert.False(result);
    }

    #endregion

    #region TryMoveFocusBack Tests

    [Fact]
    public void TryMoveFocusBack_FromSecondField_MovesToFirst()
    {
        var state = CreateStateWithFields("field1", "field2", "field3");
        state.SetCurrentFocus("field2");
        var navigator = new FormNavigator(state);

        var result = navigator.TryMoveFocusBack();

        Assert.True(result);
        Assert.Equal("field1", state.CurrentFocusedFieldId);
    }

    [Fact]
    public void TryMoveFocusBack_FromFirstField_ReturnsFalse()
    {
        var state = CreateStateWithFields("field1", "field2");
        state.SetCurrentFocus("field1");
        var navigator = new FormNavigator(state);

        var result = navigator.TryMoveFocusBack();

        Assert.False(result);
        Assert.Equal("field1", state.CurrentFocusedFieldId);
    }

    [Fact]
    public void TryMoveFocusBack_SkipsHiddenFields()
    {
        var state = CreateStateWithFields("field1", "field2", "field3");
        state.SetCurrentFocus("field3");
        state.SetFieldAvailable("field2", false);
        var navigator = new FormNavigator(state);

        var result = navigator.TryMoveFocusBack();

        Assert.True(result);
        Assert.Equal("field1", state.CurrentFocusedFieldId);
    }

    #endregion

    #region TrySetFocus Tests

    [Fact]
    public void TrySetFocus_ValidField_SetsFocus()
    {
        var state = CreateStateWithFields("field1", "field2");
        var navigator = new FormNavigator(state);

        var result = navigator.TrySetFocus("field2");

        Assert.True(result);
        Assert.Equal("field2", state.CurrentFocusedFieldId);
    }

    [Fact]
    public void TrySetFocus_UnknownField_ReturnsFalse()
    {
        var state = CreateStateWithFields("field1", "field2");
        var navigator = new FormNavigator(state);

        var result = navigator.TrySetFocus("unknown");

        Assert.False(result);
    }

    [Fact]
    public void TrySetFocus_HiddenField_ReturnsFalse()
    {
        var state = CreateStateWithFields("field1", "field2");
        state.SetFieldAvailable("field2", false);
        var navigator = new FormNavigator(state);

        var result = navigator.TrySetFocus("field2");

        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TrySetFocus_InvalidFieldId_ReturnsFalse(string? fieldId)
    {
        var state = CreateStateWithFields("field1");
        var navigator = new FormNavigator(state);

        var result = navigator.TrySetFocus(fieldId!);

        Assert.False(result);
    }

    #endregion

    #region TryNavigateToFieldGroup Tests

    [Fact]
    public void TryNavigateToFieldGroup_ValidFieldGroup_FocusesFirstFieldInFieldGroup()
    {
        var state = CreateStateWithFieldGroups(
            ("section1", ["field1", "field2"]),
            ("section2", ["field3", "field4"]));
        state.SetCurrentFocus("field1");
        var navigator = new FormNavigator(state);

        var result = navigator.TryNavigateToFieldGroup("section2");

        Assert.True(result);
        Assert.Equal("field3", state.CurrentFocusedFieldId);
    }

    [Fact]
    public void TryNavigateToFieldGroup_UnknownFieldGroup_ReturnsFalse()
    {
        var state = CreateStateWithFieldGroups(("section1", ["field1"]));
        var navigator = new FormNavigator(state);

        var result = navigator.TryNavigateToFieldGroup("unknown");

        Assert.False(result);
    }

    [Fact]
    public void TryNavigateToFieldGroup_AllFieldsInFieldGroupHidden_ReturnsFalse()
    {
        var state = CreateStateWithFieldGroups(
            ("section1", ["field1"]),
            ("section2", ["field2", "field3"]));
        state.SetFieldAvailable("field2", false);
        state.SetFieldAvailable("field3", false);
        var navigator = new FormNavigator(state);

        var result = navigator.TryNavigateToFieldGroup("section2");

        Assert.False(result);
    }

    #endregion

    #region EnsureFocusIsValid Tests

    [Fact]
    public void EnsureFocusIsValid_FocusOnHiddenField_MovesToNextAvailable()
    {
        var state = CreateStateWithFields("field1", "field2", "field3");
        state.SetCurrentFocus("field2");
        state.SetFieldAvailable("field2", false);
        var navigator = new FormNavigator(state);

        navigator.EnsureFocusIsValid();

        Assert.Equal("field3", state.CurrentFocusedFieldId);
    }

    [Fact]
    public void EnsureFocusIsValid_FocusOnValidField_NoChange()
    {
        var state = CreateStateWithFields("field1", "field2");
        state.SetCurrentFocus("field1");
        var navigator = new FormNavigator(state);

        navigator.EnsureFocusIsValid();

        Assert.Equal("field1", state.CurrentFocusedFieldId);
    }

    [Fact]
    public void EnsureFocusIsValid_NoFocus_SetsToFirstAvailable()
    {
        var state = CreateStateWithFields("field1", "field2");
        var navigator = new FormNavigator(state);

        navigator.EnsureFocusIsValid();

        Assert.Equal("field1", state.CurrentFocusedFieldId);
    }

    #endregion

    #region Helper Classes and Methods

    private static TestNavigatorState CreateStateWithFields(params string[] fieldIds)
    {
        return new TestNavigatorState(
            fieldIds.ToImmutableArray(),
            fieldIds.Select((f, i) => (f, i)).ToImmutableDictionary(x => x.f, x => x.i, StringComparer.Ordinal),
            fieldIds.ToImmutableDictionary(f => f, f => "section1", StringComparer.Ordinal),
            ImmutableDictionary<string, string?>.Empty.Add("section1", null));
    }

    private static TestNavigatorState CreateStateWithFieldGroups(params (string fieldGroupId, string[] fieldIds)[] fieldGroups)
    {
        var allFields = fieldGroups.SelectMany(fieldGroup => fieldGroup.fieldIds).ToImmutableArray();
        var fieldIndexById = allFields.Select((f, i) => (f, i)).ToImmutableDictionary(x => x.f, x => x.i, StringComparer.Ordinal);
        var fieldToFieldGroupId = fieldGroups
            .SelectMany(fieldGroup => fieldGroup.fieldIds.Select(fieldId => (fieldId, fieldGroup.fieldGroupId)))
            .ToImmutableDictionary(x => x.fieldId, x => x.fieldGroupId, StringComparer.Ordinal);
        var parentByFieldGroupId = fieldGroups
            .Select(fieldGroup => fieldGroup.fieldGroupId)
            .ToImmutableDictionary(fieldGroupId => fieldGroupId, _ => (string?)null, StringComparer.Ordinal);

        return new TestNavigatorState(allFields, fieldIndexById, fieldToFieldGroupId, parentByFieldGroupId);
    }

    #endregion
}


