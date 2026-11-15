using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormSpokenValueProjectorTests
{
    [Theory]
    [InlineData("de", "true", "ja")]
    [InlineData("de", "false", "nein")]
    [InlineData("en", "true", "yes")]
    [InlineData("fr", "true", "oui")]
    public void Project_Boolean_SpeaksPerLanguageYesNoWord(string isoCode, string canonical, string expected)
    {
        var projection = Create(isoCode).Project(
            canonical,
            ValueTypeKinds.Boolean,
            FormSpokenValueSpeakDisposition.SpeakFull,
            options: [],
            maskedWording: null);

        Assert.Equal(FormSpokenValueProjectionStatus.FullValue, projection.Status);
        Assert.Equal(expected, projection.SpokenValue);
    }

    [Theory]
    [InlineData("de", "achter Januar neunzehnhundertachtzig")]
    [InlineData("en", "January 8, nineteen eighty")]
    public void Project_Date_SpeaksLocalizedDateNotIsoLiteral(string isoCode, string expected)
    {
        var projection = Create(isoCode).Project(
            "1980-01-08",
            ValueTypeKinds.Date,
            FormSpokenValueSpeakDisposition.SpeakFull,
            options: [],
            maskedWording: null);

        Assert.Equal(FormSpokenValueProjectionStatus.FullValue, projection.Status);
        Assert.Equal(expected, projection.SpokenValue);
        Assert.DoesNotContain("1980-01-08", projection.SpokenValue, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_Enum_SpeaksDisplayNameNotCode()
    {
        var options = ImmutableArray.Create(new FormChoiceOption("green", "grün"));
        var projection = Create("de").Project(
            "green",
            ValueTypeKinds.Enum,
            FormSpokenValueSpeakDisposition.SpeakFull,
            options,
            maskedWording: null);

        Assert.Equal(FormSpokenValueProjectionStatus.FullValue, projection.Status);
        Assert.Equal("grün", projection.SpokenValue);
        Assert.True(projection.ValueResolvedFromOption);
    }

    [Theory]
    [InlineData("de", "keine")]
    [InlineData("en", "none")]
    public void Project_EmptySet_SpeaksCatalogWord(string isoCode, string expected)
    {
        var projection = Create(isoCode).Project(
            string.Empty,
            ValueTypeKinds.Set,
            FormSpokenValueSpeakDisposition.SpeakFull,
            options: ImmutableArray.Create(new FormChoiceOption("email", "E-Mail")),
            maskedWording: null);

        Assert.Equal(FormSpokenValueProjectionStatus.Empty, projection.Status);
        Assert.Equal(expected, projection.SpokenValue);
    }

    [Fact]
    public void Project_MaskedEmptySet_StaysValueFree()
    {
        var projection = Create("de").Project(
            string.Empty,
            ValueTypeKinds.Set,
            FormSpokenValueSpeakDisposition.Mask,
            options: ImmutableArray.Create(new FormChoiceOption("email", "E-Mail")),
            maskedWording: "maskiert");

        Assert.Equal(FormSpokenValueProjectionStatus.Masked, projection.Status);
        Assert.Equal(string.Empty, projection.SpokenValue);
        Assert.DoesNotContain("keine", projection.SpokenValue, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_Withhold_HasNullSpokenValue()
    {
        var projection = Create("de").Project(
            "secret",
            ValueTypeKinds.String,
            FormSpokenValueSpeakDisposition.Withhold,
            options: [],
            maskedWording: null);

        Assert.Equal(FormSpokenValueProjectionStatus.Withheld, projection.Status);
        Assert.Null(projection.SpokenValue);
    }

    [Fact]
    public void Project_UnresolvedChoice_IsUnavailable()
    {
        var options = ImmutableArray.Create(new FormChoiceOption("green", "grün"));
        var projection = Create("de").Project(
            "not-an-option",
            ValueTypeKinds.Enum,
            FormSpokenValueSpeakDisposition.SpeakFull,
            options,
            maskedWording: null);

        Assert.Equal(FormSpokenValueProjectionStatus.Unavailable, projection.Status);
        Assert.Equal("Der Wert kann nicht gesprochen werden.", projection.SpokenValue);
    }

    [Fact]
    public void Project_MalformedDate_IsUnavailable()
    {
        var projection = Create("de").Project(
            "08.01.1980",
            ValueTypeKinds.Date,
            FormSpokenValueSpeakDisposition.SpeakFull,
            options: [],
            maskedWording: null);

        Assert.Equal(FormSpokenValueProjectionStatus.Unavailable, projection.Status);
    }

    [Fact]
    public void Project_MalformedBoolean_IsUnavailable()
    {
        var projection = Create("de").Project(
            "ja",
            ValueTypeKinds.Boolean,
            FormSpokenValueSpeakDisposition.SpeakFull,
            options: [],
            maskedWording: null);

        Assert.Equal(FormSpokenValueProjectionStatus.Unavailable, projection.Status);
    }

    [Fact]
    public void Project_EmptyNonSet_IsEmptyWithoutWord()
    {
        var projection = Create("de").Project(
            string.Empty,
            ValueTypeKinds.String,
            FormSpokenValueSpeakDisposition.SpeakFull,
            options: [],
            maskedWording: null);

        Assert.Equal(FormSpokenValueProjectionStatus.Empty, projection.Status);
        Assert.Equal(string.Empty, projection.SpokenValue);
    }

    [Fact]
    public void Project_MaskWithoutWording_DoesNotLeakCanonical()
    {
        var projection = Create("de").Project(
            "DE44500105175407324931",
            ValueTypeKinds.String,
            FormSpokenValueSpeakDisposition.Mask,
            options: [],
            maskedWording: null);

        Assert.Equal(FormSpokenValueProjectionStatus.Masked, projection.Status);
        Assert.Equal(string.Empty, projection.SpokenValue);
    }

    [Fact]
    public void Project_MaskWithWording_UsesPrivacyWording()
    {
        var projection = Create("de").Project(
            "DE44500105175407324931",
            ValueTypeKinds.String,
            FormSpokenValueSpeakDisposition.Mask,
            options: [],
            maskedWording: "endet auf 4931");

        Assert.Equal(FormSpokenValueProjectionStatus.Masked, projection.Status);
        Assert.Equal("endet auf 4931", projection.SpokenValue);
    }

    [Fact]
    public void SpokenValueUnavailableCatalogKey_IsPresent()
    {
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(
            FormDialogPromptIds.SpokenValueUnavailable,
            "de",
            out var word));
        Assert.Equal("Der Wert kann nicht gesprochen werden.", word);
    }

    private static FormSpokenValueProjector Create(string isoCode)
        => new(isoCode);
}
