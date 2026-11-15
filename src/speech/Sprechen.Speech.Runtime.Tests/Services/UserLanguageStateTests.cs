using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class UserLanguageStateTests
{
    [Fact]
    public void HasSelection_WhenNotSet_IsFalse()
    {
        var state = new UserLanguageState();

        Assert.False(state.HasSelection);
        Assert.Null(state.SelectedIsoCode);
    }

    [Fact]
    public void SetSelectedIsoCode_WhenChanged_NormalizesAndRaisesEvent()
    {
        var state = new UserLanguageState();

        string? observed = null;
        state.SelectedLanguageChanged += iso => observed = iso;

        state.SetSelectedIsoCode(" DE ");

        Assert.True(state.HasSelection);
        Assert.Equal("de", state.SelectedIsoCode);
        Assert.Equal("de", observed);
    }

    [Fact]
    public void SetSelectedIsoCode_WhenSameValue_DoesNotRaiseEvent()
    {
        var state = new UserLanguageState();

        var raised = 0;
        state.SelectedLanguageChanged += _ => raised++;

        state.SetSelectedIsoCode("de");
        state.SetSelectedIsoCode(" DE ");

        Assert.Equal(1, raised);
        Assert.Equal("de", state.SelectedIsoCode);
    }

    [Fact]
    public void SetSelectedPolitenessLevel_WhenChanged_UpdatesSelection()
    {
        var state = new UserLanguageState();

        state.SetSelectedPolitenessLevel(PolitenessLevel.Informal);

        Assert.Equal(PolitenessLevel.Informal, state.SelectedPolitenessLevel);
    }

    [Fact]
    public void SetSelectedPolitenessLevel_WhenCleared_SetsNull()
    {
        var state = new UserLanguageState();
        state.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        state.SetSelectedPolitenessLevel(null);

        Assert.Null(state.SelectedPolitenessLevel);
    }
}


