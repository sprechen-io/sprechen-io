using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandSharedVariableAnnouncementServiceTests
{
    [Fact]
    public async Task SpeakSharedVariableAnnouncementAsync_WhenNoAdoptions_DoesNotSpeak()
    {
        var speechService = new RecordingSpeechService();
        var service = CreateService(speechService);
        var application = CreateApplication();
        var currentForm = CreateForm("part2", "Teil 2");
        var contract = CreateContract();

        await service.SpeakSharedVariableAnnouncementAsync(
            application,
            currentForm,
            contract,
            ImmutableArray<(string VariableId, string? SourceFormId)>.Empty,
            "de",
            new VoiceDialogSettings(),
            CancellationToken.None);

        Assert.Empty(speechService.Calls);
    }

    [Fact]
    public async Task SpeakSharedVariableAnnouncementAsync_WhenSourceAndLabelProvided_SpeaksAnnouncement()
    {
        var speechService = new RecordingSpeechService();
        var service = CreateService(speechService);
        var application = CreateApplication();
        var currentForm = CreateForm("part2", "Teil 2");
        var contract = CreateContract();
        var adoptions = ImmutableArray.Create<(string VariableId, string? SourceFormId)>(("email", "part1"));

        await service.SpeakSharedVariableAnnouncementAsync(
            application,
            currentForm,
            contract,
            adoptions,
            "de",
            new VoiceDialogSettings(),
            CancellationToken.None);

        Assert.Single(speechService.Calls);
        Assert.Equal("Ein Hinweis vorab. Ich habe E-Mail aus dem Formular Teil 1 übernommen.", speechService.Calls[0].Text);
        Assert.Equal("de", speechService.Calls[0].IsoCode);
    }

    [Fact]
    public async Task SpeakSharedVariableAnnouncementAsync_WhenPromptTemplateConfigured_UsesContractPrompt()
    {
        var speechService = new RecordingSpeechService();
        var service = CreateService(speechService);
        var application = CreateApplication();
        var currentForm = CreateForm("part2", "Teil 2");
        var contract = CreateContract(
            [
                new VoicePrompt
                {
                    PromptId = FormDialogPromptIds.SharedVariableAnnouncementSourceAndLabels,
                    Text = new LocalizedText
                    {
                        ["de"] = "MODELL: {{fieldLabels}} aus {{sourceFormName}}."
                    }
                }
            ]);
        var adoptions = ImmutableArray.Create<(string VariableId, string? SourceFormId)>(("email", "part1"));

        await service.SpeakSharedVariableAnnouncementAsync(
            application,
            currentForm,
            contract,
            adoptions,
            "de",
            new VoiceDialogSettings(),
            CancellationToken.None);

        Assert.Single(speechService.Calls);
        Assert.Equal("MODELL: E-Mail aus Teil 1.", speechService.Calls[0].Text);
    }

    [Fact]
    public async Task SpeakSharedVariableAnnouncementAsync_WithoutPromptTemplate_UsesLocalizedFrenchFallback()
    {
        var speechService = new RecordingSpeechService();
        var service = CreateService(speechService);
        var application = CreateApplication();
        var currentForm = CreateForm("part2", "Teil 2");
        var contract = CreateContract();
        var adoptions = ImmutableArray.Create<(string VariableId, string? SourceFormId)>(("email", "part1"));

        await service.SpeakSharedVariableAnnouncementAsync(
            application,
            currentForm,
            contract,
            adoptions,
            "fr",
            new VoiceDialogSettings(),
            CancellationToken.None);

        Assert.Single(speechService.Calls);
        Assert.StartsWith("Note :", speechService.Calls[0].Text, StringComparison.Ordinal);
    }

    private static ApplicationDescriptor CreateApplication()
    {
        return new ApplicationDescriptor
        {
            ApplicationId = "multi",
            DisplayName = new LocalizedText { ["de"] = "Multi" },
            Description = new LocalizedText { ["de"] = "Multi" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms =
            [
                CreateForm("part1", "Teil 1"),
                CreateForm("part2", "Teil 2")
            ]
        };
    }

    private static FormVoiceCommandSharedVariableAnnouncementService CreateService(
        IFormVoiceCommandSpeechService speechService)
        => new(
            speechService,
            new SharedVariableAnnouncementContentResolver(
                new SharedVariableAnnouncementLabelResolver(),
                new SharedVariableAnnouncementSourceResolver()),
            new SharedVariableAnnouncementPromptResolver());

    private static FormDescriptor CreateForm(string formId, string displayName)
    {
        return new FormDescriptor
        {
            FormId = FormId.From(formId),
            DisplayName = new LocalizedText { ["de"] = displayName },
            Description = new LocalizedText { ["de"] = displayName },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };
    }

    private static FormContract CreateContract(List<VoicePrompt>? prompts = null)
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "multi",
            FormId = FormId.From("part2"),
            DisplayName = new LocalizedText { ["de"] = "Teil 2" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("email"),
                    VariableId = VariableId.From("email"),
                    Label = new LocalizedText { ["de"] = "E-Mail" }
                }
            ],
            Projections = prompts is { Count: > 0 }
                ? new ContractProjections
                {
                    Voice = new VoiceProjection
                    {
                        Hints = new VoiceHintsDefinition
                        {
                            Prompts = prompts
                        }
                    }
                }
                : null
        };
    }

}

