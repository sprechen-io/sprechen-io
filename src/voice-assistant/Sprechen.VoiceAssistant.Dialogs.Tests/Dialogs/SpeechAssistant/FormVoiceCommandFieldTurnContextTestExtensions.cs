using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

internal static class FormVoiceCommandFieldTurnContextTestExtensions
{
    public static Task<FormVoiceCommandFieldHandlingResult> HandleAsync(
        this FormVoiceCommandFieldInteractionHandler handler,
        FormVoiceCommandContext context,
        IFormDialogRuntime interpreter,
        FormFieldSnapshot field,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandAutoHelpTracker autoHelp,
        CancellationToken cancellationToken)
    {
        return handler.HandleAsync(
            CreateTurnContext(context, interpreter, field, session, stateTracker, autoHelp),
            cancellationToken);
    }

    public static Task<FormVoiceCommandFieldOrchestrationResult> OrchestrateAsync(
        this FormVoiceCommandFieldTranscriptOrchestrator orchestrator,
        FormVoiceCommandContext context,
        IFormDialogRuntime interpreter,
        FormFieldSnapshot field,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandAutoHelpTracker autoHelp,
        InputMode expectedInputMode,
        CancellationToken cancellationToken)
    {
        return orchestrator.OrchestrateAsync(
            CreateTurnContext(context, interpreter, field, session, stateTracker, autoHelp),
            expectedInputMode,
            announceReadOnlyFieldGroupContent: false,
            cancellationToken);
    }

    public static Task<FormVoiceCommandFieldHandlingResult> ApplyAsync(
        this FormVoiceCommandFieldApplyService service,
        FormVoiceCommandContext context,
        IFormDialogRuntime interpreter,
        FormFieldSnapshot field,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandAutoHelpTracker autoHelp,
        InputMode expectedInputMode,
        string transcriptForApply,
        AmbiguityTelemetryData? ambiguityTelemetry,
        int? sttConfidencePercent,
        CancellationToken cancellationToken)
    {
        return service.ApplyAsync(
            CreateTurnContext(context, interpreter, field, session, stateTracker, autoHelp),
            new FormVoiceCommandFieldApplyRequest(
                expectedInputMode,
                transcriptForApply,
                ambiguityTelemetry,
                sttConfidencePercent),
            cancellationToken);
    }

    public static Task<FormVoiceCommandFieldHandlingResult> HandleCaptureFailureAsync(
        this FormVoiceCommandFieldErrorHandlingService service,
        EvaluationSession session,
        FormFieldSnapshot field,
        FormVoiceCommandStateTracker stateTracker,
        string expectedInputMode,
        string issueCode,
        FormVoiceCommandAutoHelpTracker autoHelp,
        CancellationToken cancellationToken,
        bool autoReplayEligible = true)
    {
        return service.HandleCaptureFailureAsync(
            CreateTurnContext(CreateContext(), Mock.Of<IFormDialogRuntime>(), field, session, stateTracker, autoHelp),
            expectedInputMode,
            issueCode,
            autoReplayEligible,
            cancellationToken);
    }

    public static Task<FormVoiceCommandFieldHandlingResult> HandlePromptedFailureAsync(
        this FormVoiceCommandFieldErrorHandlingService service,
        FormVoiceCommandContext context,
        EvaluationSession session,
        FormFieldSnapshot field,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandAutoHelpTracker autoHelp,
        string issueCode,
        string prompt,
        string trigger,
        CancellationToken cancellationToken,
        bool trackFieldAttempt = false,
        string? expectedInputMode = null)
    {
        return service.HandlePromptedFailureAsync(
            CreateTurnContext(context, Mock.Of<IFormDialogRuntime>(), field, session, stateTracker, autoHelp),
            issueCode,
            prompt,
            trigger,
            cancellationToken,
            trackFieldAttempt,
            expectedInputMode);
    }

    public static Task<FormVoiceCommandFieldHandlingResult> HandleManualCorrectionRequestedAsync(
        this FormVoiceCommandFieldErrorHandlingService service,
        FormVoiceCommandContext context,
        EvaluationSession session,
        FormFieldSnapshot field,
        FormVoiceCommandStateTracker stateTracker,
        CancellationToken cancellationToken)
    {
        return service.HandleManualCorrectionRequestedAsync(
            CreateTurnContext(
                context,
                Mock.Of<IFormDialogRuntime>(),
                field,
                session,
                stateTracker,
                new FormVoiceCommandAutoHelpTracker(
                    context.VoiceSettings,
                    Mock.Of<IFormVoiceCommandPromptService>(),
                    Mock.Of<IFormVoiceCommandDialogStateTracker>(),
                    Mock.Of<IFormDialogRuntime>(),
                    context.IsoCode)),
            cancellationToken);
    }

    public static Task<FormVoiceCommandFieldHandlingResult> HandleValidationViolationAsync(
        this FormVoiceCommandFieldErrorHandlingService service,
        FormVoiceCommandContext context,
        EvaluationSession session,
        FormFieldSnapshot field,
        FormVoiceCommandStateTracker stateTracker,
        string repairCode,
        string violationPrompt,
        CancellationToken cancellationToken)
    {
        return service.HandleValidationViolationAsync(
            CreateTurnContext(
                context,
                Mock.Of<IFormDialogRuntime>(),
                field,
                session,
                stateTracker,
                new FormVoiceCommandAutoHelpTracker(
                    context.VoiceSettings,
                    Mock.Of<IFormVoiceCommandPromptService>(),
                    Mock.Of<IFormVoiceCommandDialogStateTracker>(),
                    Mock.Of<IFormDialogRuntime>(),
                    context.IsoCode)),
            repairCode,
            violationPrompt,
            cancellationToken);
    }

    private static FormVoiceCommandFieldTurnContext CreateTurnContext(
        FormVoiceCommandContext context,
        IFormDialogRuntime interpreter,
        FormFieldSnapshot field,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandAutoHelpTracker autoHelp)
        => new(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            interpreter as IFormDialogPrivacyReadbackPolicy ?? Mock.Of<IFormDialogPrivacyReadbackPolicy>());

    private static FormVoiceCommandContext CreateContext()
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Form" },
            Description = new LocalizedText { ["de"] = "Form" },
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
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables = [],
            Fields = [],
            FieldGroups = []
        };

        return new FormVoiceCommandContext(
            new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "form",
                WakeWordActivationForms: []),
            new FormVoiceCommandSelectionContext(
                new VoiceDialogSettings(),
                application,
                ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                form,
                contract),
            new FormVoiceCommandValueSnapshot(
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                []));
    }
}
