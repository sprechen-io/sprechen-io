using Sprechen.Forms.ViewComponents;

namespace Sprechen.Forms.Tests.ViewComponents;

public sealed class FormNavigationUrlBuilderTests
{
    [Fact]
    public void Build_PreservesRouteQueryNamesAndOmitsStudyParameters()
    {
        var builder = new FormNavigationUrlBuilder();

        var url = builder.Build(
            new FormNavigationModel
            {
                ApplicationId = "app with spaces",
                Language = "de",
                ParticipantId = "P001",
                TaskId = "T01",
                Condition = "B"
            },
            "form&special");


        // Die Studiennavigation wird getrennt im Client behandelt.
        Assert.Equal("/?app=app%20with%20spaces&form=form%26special&lang=de", url);
    }
}
