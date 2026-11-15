using Xunit;
using Sprechen.Forms.ViewComponents;

namespace Sprechen.Forms.Tests.ViewComponents;

public sealed class FormNavigationModelTests
{
    [Fact]
    public void CurrentIndex_WhenFormInSequence_ReturnsCorrectIndex()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form2",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.Equal(1, model.CurrentIndex);
    }

    [Fact]
    public void CurrentIndex_WhenFormNotInSequence_ReturnsMinusOne()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "unknown",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.Equal(-1, model.CurrentIndex);
    }

    [Fact]
    public void CurrentIndex_WhenSequenceEmpty_ReturnsMinusOne()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form1",
            FormSequence = Array.Empty<string>()
        };

        Assert.Equal(-1, model.CurrentIndex);
    }

    [Fact]
    public void CurrentFormNumber_ReturnsOneBasedIndex()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form2",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.Equal(2, model.CurrentFormNumber);
    }

    [Fact]
    public void HasPrevious_WhenFirstForm_ReturnsFalse()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form1",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.False(model.HasPrevious);
    }

    [Fact]
    public void HasPrevious_WhenNotFirstForm_ReturnsTrue()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form2",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.True(model.HasPrevious);
    }

    [Fact]
    public void HasNext_WhenLastForm_ReturnsFalse()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form3",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.False(model.HasNext);
    }

    [Fact]
    public void HasNext_WhenNotLastForm_ReturnsTrue()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form2",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.True(model.HasNext);
    }

    [Fact]
    public void IsLastForm_WhenLastForm_ReturnsTrue()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form3",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.True(model.IsLastForm);
    }

    [Fact]
    public void IsLastForm_WhenNotLastForm_ReturnsFalse()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form1",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.False(model.IsLastForm);
    }

    [Fact]
    public void IsFirstForm_WhenFirstForm_ReturnsTrue()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form1",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.True(model.IsFirstForm);
    }

    [Fact]
    public void PreviousFormId_WhenHasPrevious_ReturnsPreviousId()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form2",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.Equal("form1", model.PreviousFormId);
    }

    [Fact]
    public void PreviousFormId_WhenNoPrevious_ReturnsNull()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form1",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.Null(model.PreviousFormId);
    }

    [Fact]
    public void NextFormId_WhenHasNext_ReturnsNextId()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form2",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.Equal("form3", model.NextFormId);
    }

    [Fact]
    public void NextFormId_WhenNoNext_ReturnsNull()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form3",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.Null(model.NextFormId);
    }

    [Fact]
    public void BuildFormUrl_IncludesAllRequiredParameters()
    {
        var model = new FormNavigationModel
        {
            ApplicationId = "demo-app",
            Language = "de",
            FormSequence = new[] { "form1" }
        };

        var url = model.BuildFormUrl("target-form");

        Assert.Contains("app=demo-app", url);
        Assert.Contains("form=target-form", url);
        Assert.Contains("lang=de", url);
    }

    [Fact]
    public void BuildFormUrl_OmitsStudyParametersEvenWhenProvided()
    {
        // Auch befüllte Studienangaben dürfen pid, tid, cond oder var nicht zur URL hinzufügen.
        // Clientnavigation und Eingangsprotokolle werden durch diesen Test nicht geprüft.
        var model = new FormNavigationModel
        {
            ApplicationId = "demo-app",
            Language = "de",
            ParticipantId = "P001",
            TaskId = "T01",
            Condition = "A",
            FormSequence = new[] { "form1" }
        };

        var url = model.BuildFormUrl("target-form");

        Assert.DoesNotContain("pid=", url);
        Assert.DoesNotContain("tid=", url);
        Assert.DoesNotContain("cond=", url);
        Assert.DoesNotContain("var=", url);
    }

    [Fact]
    public void BuildFormUrl_OmitsEmptyStudyParameters()
    {
        var model = new FormNavigationModel
        {
            ApplicationId = "demo-app",
            Language = "de",
            ParticipantId = "",
            TaskId = "",
            Condition = "",
            FormSequence = new[] { "form1" }
        };

        var url = model.BuildFormUrl("target-form");

        Assert.DoesNotContain("pid=", url);
        Assert.DoesNotContain("tid=", url);
        Assert.DoesNotContain("cond=", url);
        Assert.DoesNotContain("var=", url);
    }

    [Fact]
    public void BuildFormUrl_EncodesSpecialCharacters()
    {
        var model = new FormNavigationModel
        {
            ApplicationId = "app with spaces",
            Language = "de",
            FormSequence = new[] { "form1" }
        };

        var url = model.BuildFormUrl("form&special");

        Assert.Contains("app=app%20with%20spaces", url);
        Assert.Contains("form=form%26special", url);
    }

    [Fact]
    public void PreviousFormUrl_WhenHasPrevious_ReturnsValidUrl()
    {
        var model = new FormNavigationModel
        {
            ApplicationId = "demo-app",
            CurrentFormId = "form2",
            Language = "de",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.NotNull(model.PreviousFormUrl);
        Assert.Contains("form=form1", model.PreviousFormUrl);
    }

    [Fact]
    public void NextFormUrl_WhenHasNext_ReturnsValidUrl()
    {
        var model = new FormNavigationModel
        {
            ApplicationId = "demo-app",
            CurrentFormId = "form2",
            Language = "de",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.NotNull(model.NextFormUrl);
        Assert.Contains("form=form3", model.NextFormUrl);
    }

    [Fact]
    public void ProgressPercent_SingleForm_Returns100()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form1",
            FormSequence = new[] { "form1" }
        };

        Assert.Equal(100, model.ProgressPercent);
    }

    [Fact]
    public void ProgressPercent_MiddleOfThreeForms_Returns67()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form2",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.Equal(67, model.ProgressPercent);
    }

    [Fact]
    public void ProgressPercent_FirstOfThreeForms_Returns33()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form1",
            FormSequence = new[] { "form1", "form2", "form3" }
        };

        Assert.Equal(33, model.ProgressPercent);
    }

    [Fact]
    public void ProgressPercent_EmptySequence_Returns0()
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = "form1",
            FormSequence = Array.Empty<string>()
        };

        Assert.Equal(0, model.ProgressPercent);
    }

    [Fact]
    public void TotalForms_ReturnsSequenceCount()
    {
        var model = new FormNavigationModel
        {
            FormSequence = new[] { "form1", "form2", "form3", "form4" }
        };

        Assert.Equal(4, model.TotalForms);
    }

    [Theory]
    [InlineData("FORM1", "form1", true)]
    [InlineData("Form1", "form1", true)]
    [InlineData("form1", "FORM1", true)]
    public void CurrentIndex_IsCaseInsensitive(string currentFormId, string sequenceFormId, bool shouldMatch)
    {
        var model = new FormNavigationModel
        {
            CurrentFormId = currentFormId,
            FormSequence = new[] { sequenceFormId, "form2" }
        };

        if (shouldMatch)
        {
            Assert.Equal(0, model.CurrentIndex);
        }
        else
        {
            Assert.Equal(-1, model.CurrentIndex);
        }
    }
}
