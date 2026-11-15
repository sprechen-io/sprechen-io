using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class ApplicationSelectionDialogPreflightTests
{
    [Fact]
    public void Validate_WhenPromptIdsDifferOnlyByCase_ReturnsDuplicateError()
    {
        var model = CreateValidModel();
        model.Prompts.Add(new CatalogVoicePrompt
        {
            PromptId = "APP.HELP",
            Text = new LocalizedText { ["de"] = "hilfe erneut" }
        });

        var result = ApplicationSelectionDialogPreflight.Validate(model);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error =>
            error.Code == PreflightCodes.ErrorIdDuplicate
            && error.Id == "APP.HELP"
            && error.Path == "$.prompts[5].promptId");
    }

    [Fact]
    public void Validate_WhenIntentIdIsSharedAcrossDifferentMatchModes_ReturnsOk()
    {
        var model = CreateValidModel();
        model.Intents =
        [
            CreateIntent(CatalogVoiceIntentId.Search, CatalogVoiceIntentMatchMode.Phrase, "suche"),
            CreateIntent(CatalogVoiceIntentId.Search, CatalogVoiceIntentMatchMode.Prefix, "suche nach")
        ];

        var result = ApplicationSelectionDialogPreflight.Validate(model);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenIntentIdAndMatchModeAreDuplicated_ReturnsError()
    {
        var model = CreateValidModel();
        model.Intents =
        [
            CreateIntent(CatalogVoiceIntentId.Search, CatalogVoiceIntentMatchMode.Prefix, "suche"),
            CreateIntent(CatalogVoiceIntentId.Search, CatalogVoiceIntentMatchMode.Prefix, "suche nach")
        ];

        var result = ApplicationSelectionDialogPreflight.Validate(model);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error =>
            error.Code == PreflightCodes.ErrorIdDuplicate
            && error.Id == CatalogVoiceIntentId.Search
            && error.Path == "$.intents[1]");
    }

    [Theory]
    [InlineData(ApplicationSelectionRequiredPromptId.AppConfirmationRepeat)]
    [InlineData(ApplicationSelectionRequiredPromptId.FormConfirmationRepeat)]
    [InlineData(ApplicationSelectionRequiredPromptId.AppReListen)]
    [InlineData(ApplicationSelectionRequiredPromptId.FormReListen)]
    public void Validate_WhenRequiredConfirmationRepeatPromptIsMissing_ReturnsReferenceError(
        string missingPromptId)
    {
        var model = CreateValidModel();
        var prompt = Assert.Single(model.Prompts, candidate =>
            string.Equals(candidate.PromptId, missingPromptId, StringComparison.Ordinal));
        Assert.True(model.Prompts.Remove(prompt));

        var result = ApplicationSelectionDialogPreflight.Validate(model);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error =>
            error.Code == PreflightCodes.ErrorRefMissing
            && error.Id == missingPromptId
            && error.Path == "$.prompts");
    }

    [Theory]
    [InlineData(ApplicationSelectionRequiredPromptId.AppConfirmationRepeat)]
    [InlineData(ApplicationSelectionRequiredPromptId.FormConfirmationRepeat)]
    [InlineData(ApplicationSelectionRequiredPromptId.AppReListen)]
    [InlineData(ApplicationSelectionRequiredPromptId.FormReListen)]
    public void Validate_WhenRequiredConfirmationRepeatPromptHasWrongCase_ReturnsReferenceError(
        string requiredPromptId)
    {
        var model = CreateValidModel();
        var prompt = Assert.Single(model.Prompts, candidate =>
            string.Equals(candidate.PromptId, requiredPromptId, StringComparison.Ordinal));
        prompt.PromptId = requiredPromptId.ToUpperInvariant();

        var result = ApplicationSelectionDialogPreflight.Validate(model);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error =>
            error.Code == PreflightCodes.ErrorRefMissing
            && error.Id == requiredPromptId
            && error.Path == "$.prompts");
    }

    private static ApplicationSelectionDialog CreateValidModel()
    {
        return new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "1",
            SelectionDialogId = "default.selectionDialog",
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Prompts =
            [
                new CatalogVoicePrompt
                {
                    PromptId = "app.help",
                    Text = new LocalizedText { ["de"] = "hilfe" }
                },
                CreatePrompt(ApplicationSelectionRequiredPromptId.AppConfirmationRepeat),
                CreatePrompt(ApplicationSelectionRequiredPromptId.FormConfirmationRepeat),
                CreatePrompt(ApplicationSelectionRequiredPromptId.AppReListen),
                CreatePrompt(ApplicationSelectionRequiredPromptId.FormReListen)
            ],
            Intents =
            [
                CreateIntent(CatalogVoiceIntentId.Help, CatalogVoiceIntentMatchMode.Phrase, "hilfe")
            ],
            NumberWords =
            [
                new CatalogVoiceNumberWord
                {
                    Value = 1,
                    Utterances = [new LocalizedText { ["de"] = "eins" }]
                }
            ]
        };
    }

    private static CatalogVoiceIntent CreateIntent(string intentId, string matchMode, string utterance)
    {
        return new CatalogVoiceIntent
        {
            IntentId = intentId,
            MatchMode = matchMode,
            Utterances = [new LocalizedText { ["de"] = utterance }]
        };
    }

    private static CatalogVoicePrompt CreatePrompt(string promptId)
    {
        return new CatalogVoicePrompt
        {
            PromptId = promptId,
            Text = new LocalizedText
            {
                ["de@formal"] = "Bitte sagen Sie ja oder nein.",
                ["de@informal"] = "Bitte sag ja oder nein."
            }
        };
    }
}
