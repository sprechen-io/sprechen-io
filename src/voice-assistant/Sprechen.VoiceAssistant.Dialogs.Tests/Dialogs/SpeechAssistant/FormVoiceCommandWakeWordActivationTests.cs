using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandWakeWordActivationTests
{
    [Fact]
    public void TryExtractActivatedCommandTranscript_WhenLeadingWakeWordExists_ReturnsCommandText()
    {
        var context = CreateContext(["Okay"]);

        var result = FormVoiceCommandWakeWordActivation.TryExtractActivatedCommandTranscript(
            context,
            "Okay welche feldgruppen",
            out var commandTranscript);

        Assert.True(result);
        Assert.Equal("welche feldgruppen", commandTranscript);
    }

    [Fact]
    public void TryExtractActivatedCommandTranscript_WhenWakeWordIsMissing_ReturnsFalse()
    {
        var context = CreateContext(["Okay"]);

        var result = FormVoiceCommandWakeWordActivation.TryExtractActivatedCommandTranscript(
            context,
            "welche feldgruppen",
            out var commandTranscript);

        Assert.False(result);
        Assert.Equal(string.Empty, commandTranscript);
    }

    [Fact]
    public void TryExtractActivatedCommandTranscript_WhenNoWakeWordsAreConfigured_ReturnsFalse()
    {
        var context = CreateContext([]);

        var result = FormVoiceCommandWakeWordActivation.TryExtractActivatedCommandTranscript(
            context,
            "Okay welche feldgruppen",
            out var commandTranscript);

        Assert.False(result);
        Assert.Equal(string.Empty, commandTranscript);
    }

    [Fact]
    public void TryExtractActivatedCommandTranscript_WhenRecognitionOnlyActivationMatches_ReturnsCommandText()
    {
        var context = CreateContext(
            ["Okay"],
            wakeWordRecognitionOnlyActivationForms: ["Ok", "OK"]);

        var result = FormVoiceCommandWakeWordActivation.TryExtractActivatedCommandTranscript(
            context,
            "OK welche feldgruppen",
            out var commandTranscript);

        Assert.True(result);
        Assert.Equal("welche feldgruppen", commandTranscript);
    }

    [Fact]
    public void TryExtractActivatedCommandTranscript_WhenCustomMultiTokenActivationFormMatches_ReturnsCommandText()
    {
        var context = CreateContext(
            ["Okay Alpha"]);

        var result = FormVoiceCommandWakeWordActivation.TryExtractActivatedCommandTranscript(
            context,
            "Okay Alpha welche feldgruppen",
            out var commandTranscript);

        Assert.True(result);
        Assert.Equal("welche feldgruppen", commandTranscript);
    }

    [Fact]
    public void TryExtractActivatedCommandTranscript_WhenLeadingActivationRepeats_ReturnsCommandAfterSequence()
    {
        var context = CreateContext(["Okay Alex", "Hey Alex"]);

        var result = FormVoiceCommandWakeWordActivation.TryExtractActivatedCommandTranscript(
            context,
            "Hey Alex. Okay Alex. welche feldgruppen",
            out var commandTranscript);

        Assert.True(result);
        Assert.Equal("welche feldgruppen", commandTranscript);
    }

    private static FormVoiceCommandContext CreateContext(
        ImmutableArray<string> wakeWordActivationForms,
        ImmutableArray<string>? wakeWordRecognitionOnlyActivationForms = null)
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            Description = new LocalizedText { ["de"] = "Teil 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        var sessionIdentity = new FormVoiceCommandSessionIdentity(
            IsoCode: "de",
            ApplicationId: "app",
            FormId: "current",
            WakeWordActivationForms: wakeWordActivationForms)
        {
            WakeWordRecognitionOnlyActivationForms = wakeWordRecognitionOnlyActivationForms ?? []
        };

        return new FormVoiceCommandContext(
            SessionIdentity: sessionIdentity,
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: new FormContract
                {
                    SchemaVersion = "1",
                    ContractVersion = "1",
                    ApplicationId = "app",
                    FormId = FormId.From("current"),
                    DisplayName = new LocalizedText { ["de"] = "Teil 1" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    StrictLocalization = true,
                    EntryFieldGroupId = FieldGroupId.From("main"),
                    Fields = [],
                    FieldGroups = []
                }),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }
}
