using Xunit;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Formularauswahl und Formularanzahl in Assistentenzustand, Katalogergebnis und Auswahlereignis.
/// </summary>
public sealed class MultiFormNavigationTests
{
    #region SpeechAssistantState Tests

    [Fact]
    public void SpeechAssistantState_Initial_HasNoSelectedFormId()
    {
        var state = SpeechAssistantState.Initial;

        Assert.Null(state.SelectedFormId);
        Assert.False(state.HasSelectedForm);
    }

    [Fact]
    public void SpeechAssistantState_WithSelectedFormId_TracksForm()
    {
        var state = SpeechAssistantState.Initial with
        {
            SelectedApplicationId = "test-app",
            SelectedFormId = "basic-data"
        };

        Assert.Equal("basic-data", state.SelectedFormId);
        Assert.True(state.HasSelectedForm);
    }

    [Fact]
    public void SpeechAssistantState_HasSelectedForm_FalseWhenNull()
    {
        var state = SpeechAssistantState.Initial with
        {
            SelectedApplicationId = "test-app",
            SelectedFormId = null
        };

        Assert.False(state.HasSelectedForm);
    }

    [Fact]
    public void SpeechAssistantState_HasSelectedForm_FalseWhenWhitespace()
    {
        var state = SpeechAssistantState.Initial with
        {
            SelectedApplicationId = "test-app",
            SelectedFormId = "   "
        };

        Assert.False(state.HasSelectedForm);
    }

    #endregion

    #region FormsCatalogDialogResult Tests

    [Fact]
    public void FormsCatalogDialogResult_Selected_IncludesFormCountAndDefaultForm()
    {
        var result = FormsCatalogDialogResult.Selected(
            applicationId: "sample.application",
            applicationName: "Sample Application",
            formCount: 3,
            defaultFormId: "basic-data");

        Assert.False(result.WasCancelled);
        Assert.Equal("sample.application", result.SelectedApplicationId);
        Assert.Equal("Sample Application", result.SelectedApplicationName);
        Assert.Equal(3, result.FormCount);
        Assert.Equal("basic-data", result.DefaultFormId);
    }

    [Fact]
    public void FormsCatalogDialogResult_Selected_SingleFormAppHasFormCount1()
    {
        var result = FormsCatalogDialogResult.Selected(
            applicationId: "single-form-app",
            applicationName: "Single Form",
            formCount: 1,
            defaultFormId: "main");

        Assert.Equal(1, result.FormCount);
        Assert.Equal("main", result.DefaultFormId);
    }

    [Fact]
    public void FormsCatalogDialogResult_HasMultipleForms_TrueWhenFormCountGreaterThan1()
    {
        var result = FormsCatalogDialogResult.Selected(
            applicationId: "multi-form-app",
            applicationName: "Multi Form",
            formCount: 3,
            defaultFormId: "basic-data");

        Assert.True(result.HasMultipleForms);
    }

    [Fact]
    public void FormsCatalogDialogResult_HasMultipleForms_FalseWhenFormCount1()
    {
        var result = FormsCatalogDialogResult.Selected(
            applicationId: "single-form-app",
            applicationName: "Single Form",
            formCount: 1,
            defaultFormId: "main");

        Assert.False(result.HasMultipleForms);
    }

    [Fact]
    public void FormsCatalogDialogResult_Cancelled_HasNoFormInfo()
    {
        var result = FormsCatalogDialogResult.Cancelled(DialogCancellationReason.UserCancelled);

        Assert.True(result.WasCancelled);
        Assert.Equal(DialogCancellationReason.UserCancelled, result.CancellationReason);
        Assert.Null(result.FormCount);
        Assert.Null(result.DefaultFormId);
        Assert.False(result.HasMultipleForms);
    }

    #endregion

    #region SpeechAssistantApplicationSelectedEvent Tests

    [Fact]
    public void ApplicationSelectedEvent_SingleForm_HasMultipleFormsFalse()
    {
        var evt = new SpeechAssistantApplicationSelectedEvent("app-id", "App Name", 1, null, null);

        Assert.False(evt.HasMultipleForms);
    }

    [Fact]
    public void ApplicationSelectedEvent_MultipleForms_HasMultipleFormsTrue()
    {
        var evt = new SpeechAssistantApplicationSelectedEvent("app-id", "App Name", 3, "basic-data", null);

        Assert.True(evt.HasMultipleForms);
        Assert.Equal(3, evt.FormCount);
        Assert.Equal("basic-data", evt.DefaultFormId);
    }

    #endregion
}


