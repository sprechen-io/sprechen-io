using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

    internal sealed class FormClosingReviewTurnFlowTestHarness
    {
        private readonly Queue<string?> _transcripts;
        private readonly List<string> _capturePrompts = [];
        private readonly string _isoCode;

        public FormClosingReviewTurnFlowTestHarness(params string?[] transcripts)
            : this("de", transcripts)
        {
        }

        private FormClosingReviewTurnFlowTestHarness(string isoCode, IEnumerable<string?> transcripts)
        {
            _isoCode = isoCode;
            _transcripts = new Queue<string?>(transcripts);
            Speech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
            Speech
                .Setup(s => s.CaptureTranscriptAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<FormVoiceCommandCaptureSettings>(),
                    It.IsAny<Action>(),
                    It.IsAny<SpeechCaptureTelemetryContext?>(),
                    It.IsAny<RecognitionProfile>(),
                    It.IsAny<SpeechKeyterms?>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                    (prompt, _, _, onStartListening, _, _, _, _) =>
                    {
                        _capturePrompts.Add(prompt);
                        CaptureCount++;
                        onStartListening?.Invoke();
                        var transcript = _transcripts.Count > 0 ? _transcripts.Dequeue() : null;
                        if (string.Equals(transcript, "<silence>", StringComparison.Ordinal))
                        {
                            throw new NoSpeechDetectedException();
                        }

                        return Task.FromResult(new SpeechCaptureResult(transcript, null));
                    });
            Speech
                .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Loose);
            Earcon = new Mock<IEarconPlayer>(MockBehavior.Loose);
            Earcon
                .Setup(e => e.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
            Interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
            // Standardmäßig ist beim Abschluss kein Feld ausgewählt. Einzelne Tests simulieren
            // ein weiterhin ausgewähltes letztes Feld oder einen Feldwechsel durch einen Sprung.
            Interpreter.Setup(i => i.GetFocusedFieldIdAsync()).ReturnsAsync((string?)null);
            _session = new EvaluationSession("closing-session", EvaluationChannel.Voice, "app", "app/current");
            StateTracker = new FormVoiceCommandStateTracker(Mock.Of<IEvaluationTelemetry>(), _session);
            StateTracker.BeginFieldPrompt("main");
            CommandTelemetry = new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose);

            _flow = new FormClosingReviewTurnFlow(
                Speech.Object,
                new FormVoiceCommandGlobalCommandExecutionService(CommandTelemetry.Object, Router.Object));
        }

        public static FormClosingReviewTurnFlowTestHarness ForLanguage(string isoCode, params string?[] transcripts)
            => new(isoCode, transcripts);

        public Mock<IFormVoiceCommandSpeechService> Speech { get; }

        public Mock<IFormVoiceCommandGlobalCommandRouter> Router { get; }

        public Mock<IEvaluationCommandTelemetry> CommandTelemetry { get; }

        public Mock<IEarconPlayer> Earcon { get; }

        public Mock<IFormDialogRuntime> Interpreter { get; }

        public FormVoiceCommandStateTracker StateTracker { get; }

        public IReadOnlyList<string> CapturePrompts => _capturePrompts;

        public int CaptureCount { get; private set; }

        private readonly EvaluationSession _session;
        private readonly FormClosingReviewTurnFlow _flow;

        public Task<FormClosingReviewTurnResult> RunAsync(string? committedValueReadbackPrompt = null)
            => _flow.RunAsync(CreateContext(), Interpreter.Object, _session, StateTracker, committedValueReadbackPrompt, CancellationToken.None);

        public void RouteSubmitAsSubmitted()
            => Router
                .Setup(r => r.TryHandleAsync(
                    It.Is<CommandId>(c => c.Value == "submit"),
                    It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                    "app",
                    "current",
                    ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        public void RouteSummaryAsContinue()
            => Router
                .Setup(r => r.TryHandleAsync(
                    It.Is<CommandId>(c => c.Value == "summary"),
                    It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((VoiceCommandDialogResult?)null);

        public void RouteJumpAsContinue()
            => Router
                .Setup(r => r.TryHandleAsync(
                    It.Is<CommandId>(c => c.Value == "jumpTo"),
                    It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((VoiceCommandDialogResult?)null);

        public void NoFocusedField()
            => Interpreter.Setup(i => i.GetFocusedFieldIdAsync()).ReturnsAsync((string?)null);

        public void FocusedFieldAfterJump()
            => Interpreter
                .SetupSequence(i => i.GetFocusedFieldIdAsync())
                .ReturnsAsync((string?)null)
                .ReturnsAsync("favoriteColor");

        public void FocusedLastFieldThenPrevious()
            => Interpreter
                .SetupSequence(i => i.GetFocusedFieldIdAsync())
                .ReturnsAsync("lastField")
                .ReturnsAsync("previousField");

        public void VerifySubmitRouted(Times times)
            => Router.Verify(
                r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value == "submit"), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()),
                times);

        public void VerifyNoCommandRoutedOtherThan(string commandId)
            => Router.Verify(
                r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value != commandId), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()),
                Times.Never);

        public void VerifyCommandRouted(string commandId, Times times)
            => Router.Verify(
                r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value == commandId), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()),
                times);

        public void VerifyCommandRecognized(string commandId, Times times)
            => CommandTelemetry.Verify(
                t => t.TrackGlobalCommand(_session, commandId),
                times);

        public void RouteBackAsContinue()
            => Router
                .Setup(r => r.TryHandleAsync(
                    It.Is<CommandId>(c => c.Value == "back"),
                    It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((VoiceCommandDialogResult?)null);

    private FormVoiceCommandContext CreateContext()
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { [_isoCode] = "Teil 1" },
            Description = new LocalizedText { [_isoCode] = "Teil 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { [_isoCode] = "App" },
            Description = new LocalizedText { [_isoCode] = "App" },
            SupportedLanguages = [_isoCode],
            DefaultLanguage = _isoCode,
            Forms = [form]
        };

        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { [_isoCode] = "Teil 1" },
            SupportedLanguages = [_isoCode],
            DefaultLanguage = _isoCode,
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables = [],
            Fields = [],
            FieldGroups = []
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: _isoCode,
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: []),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }
    }
