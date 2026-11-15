using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandPromptIdSpeakerTraceTests
{
    private const string SensitiveValue = "GEHEIM-4242";

    [Fact]
    public async Task SpeakAsync_TracesSystemPromptIdWithResolvedText()
    {
        var trace = new RecordingSpeechAssistantTrace();
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandPromptIdSpeaker(speechService, new FormVoiceCommandPromptTextResolver(), trace);

        await speaker.SpeakAsync(
            CreateContract(),
            ImmutableArray.Create("dialog.noErrors"),
            "de",
            new VoiceDialogSettings(),
            CancellationToken.None);

        Assert.Contains(
            trace.Entries,
            entry => entry.Message == "prompt spoken id=dialog.noErrors text=Keine Fehler vorhanden");
        Assert.Contains(speechService.Calls, call => call.Text == "Keine Fehler vorhanden");
    }

    [Fact]
    public async Task SpeakAsync_ForValueBearingPrompt_TracesIdOnly_AndNeverLeaksValue()
    {
        var trace = new RecordingSpeechAssistantTrace();
        var speechService = new RecordingSpeechService();
        var speaker = new FormVoiceCommandPromptIdSpeaker(speechService, new FormVoiceCommandPromptTextResolver(), trace);

        await speaker.SpeakAsync(
            CreateContract(),
            ImmutableArray.Create("dialog.summary.valueLine"),
            "de",
            new VoiceDialogSettings(),
            CancellationToken.None);

        // Die Diagnose erhält nur die Ansagekennung; der werttragende Text bleibt auf den Sprachausgabeaufruf beschränkt.
        Assert.Contains(trace.Entries, entry => entry.Message == "prompt spoken id=dialog.summary.valueLine");
        Assert.DoesNotContain(trace.Entries, entry => entry.Message.Contains(SensitiveValue, StringComparison.Ordinal));

        Assert.Contains(speechService.Calls, call => call.Text.Contains(SensitiveValue, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("dialog.summary.valueLine")]
    [InlineData("dialog.summary.storedValue")]
    [InlineData("dialog.digits.workflow.captured")]
    [InlineData("dialog.digits.currentReadback")]
    [InlineData("dialog.validation.invalidValue")]
    [InlineData("privacy.readback.sensitiveSuffix")]
    public async Task SpeakAsync_ForKnownValueBearingPromptIds_NeverAppendsResolvedText(string promptId)
    {
        var trace = new RecordingSpeechAssistantTrace();
        var speaker = new FormVoiceCommandPromptIdSpeaker(
            new RecordingSpeechService(),
            new FormVoiceCommandPromptTextResolver(),
            trace);

        await speaker.SpeakAsync(
            CreateContractWith(promptId, $"prefix {SensitiveValue}"),
            ImmutableArray.Create(promptId),
            "de",
            new VoiceDialogSettings(),
            CancellationToken.None);

        // Auch bei diesen Ansagearten darf der aufgelöste Werttext nicht in der Diagnose erscheinen.
        Assert.Contains(trace.Entries, entry => entry.Message == $"prompt spoken id={promptId}");
        Assert.DoesNotContain(trace.Entries, entry => entry.Message.Contains(SensitiveValue, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SpeakAsync_GroupsBatchUnderSingleCorrelationId()
    {
        var trace = new RecordingSpeechAssistantTrace();
        var speaker = new FormVoiceCommandPromptIdSpeaker(new RecordingSpeechService(), new FormVoiceCommandPromptTextResolver(), trace);

        await speaker.SpeakAsync(
            CreateContract(),
            ImmutableArray.Create("dialog.noErrors", "dialog.summary.valueLine"),
            "de",
            new VoiceDialogSettings(),
            CancellationToken.None);

        var correlationIds = trace.Entries.Select(entry => entry.CorrelationId).Distinct(StringComparer.Ordinal).ToArray();
        Assert.Single(correlationIds);
        Assert.All(trace.Entries, entry => Assert.Equal(SpeechAssistantTraceScope.VoiceCommandEffect, entry.Scope));
    }

    private static FormContract CreateContractWith(string promptId, string text)
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    Hints = new VoiceHintsDefinition
                    {
                        Prompts =
                        [
                            new VoicePrompt
                            {
                                PromptId = promptId,
                                Text = new LocalizedText { ["de"] = text }
                            }
                        ]
                    }
                }
            }
        };
    }

    private static FormContract CreateContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    Hints = new VoiceHintsDefinition
                    {
                        Prompts =
                        [
                            new VoicePrompt
                            {
                                PromptId = "dialog.noErrors",
                                Text = new LocalizedText { ["de"] = "Keine Fehler vorhanden" }
                            },
                            new VoicePrompt
                            {
                                PromptId = "dialog.summary.valueLine",
                                Text = new LocalizedText { ["de"] = $"Gespeicherter Wert: {SensitiveValue}" }
                            }
                        ]
                    }
                }
            }
        };
    }
}
