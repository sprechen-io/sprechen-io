using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandFormMatcherTests
{
    private readonly VoiceDialogSettings _voiceSettings = new();

    [Fact]
    public void TryResolveFormByArgument_WhenArgumentIsNumber_ReturnsFormAtPosition()
    {
        var application = CreateApplication();
        var forms = application.Forms.ToImmutableArray();
        var matcher = new FormVoiceCommandFormMatcher();

        var matched = matcher.TryResolveFormByArgument(
            application,
            forms,
            "de",
            _voiceSettings,
            "2",
            out var form,
            out var formName);

        Assert.True(matched);
        Assert.Equal("tax", form.FormId);
        Assert.Equal("Steuern", formName);
    }

    [Fact]
    public void TryMatchNamed_WhenArgumentIsLocalizedName_ReturnsMatchingForm()
    {
        var application = CreateApplication();
        var forms = application.Forms.ToImmutableArray();
        var matcher = new FormVoiceCommandNamedFormArgumentMatcher(new FormVoiceCommandFormDisplayNameResolver());

        var matched = matcher.TryMatch(
            application,
            forms,
            "de",
            _voiceSettings,
            "steuern",
            out var form,
            out var formName);

        Assert.True(matched);
        Assert.Equal("tax", form.FormId);
        Assert.Equal("Steuern", formName);
    }

    [Fact]
    public void TryMatchNumeric_WhenArgumentIsOutOfRange_ReturnsFalse()
    {
        var application = CreateApplication();
        var forms = application.Forms.ToImmutableArray();
        var matcher = new FormVoiceCommandNumericFormArgumentMatcher(new FormVoiceCommandFormDisplayNameResolver());

        var matched = matcher.TryMatch(
            application,
            forms,
            "de",
            _voiceSettings,
            "9",
            out _,
            out _);

        Assert.False(matched);
    }

    [Fact]
    public void TryResolveFormByArgument_WhenArgumentIsEmpty_ReturnsFalse()
    {
        var application = CreateApplication();
        var forms = application.Forms.ToImmutableArray();
        var matcher = new FormVoiceCommandFormMatcher();

        var matched = matcher.TryResolveFormByArgument(
            application,
            forms,
            "de",
            _voiceSettings,
            " ",
            out _,
            out _);

        Assert.False(matched);
    }

    private static ApplicationDescriptor CreateApplication()
    {
        return new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("profile"),
                    DisplayName = new LocalizedText { ["de"] = "Profil" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                },
                new FormDescriptor
                {
                    FormId = FormId.From("tax"),
                    DisplayName = new LocalizedText { ["de"] = "Steuern" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };
    }
}
