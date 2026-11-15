namespace Sprechen.Speech.App.Contracts.PromptTemplates;

/// <summary>
/// Kennungen der modellierten Ansagen für Eingabe, Bestätigung,
/// Navigation und Rückmeldung im Formulardialog.
/// </summary>
public static class FormDialogPromptIds
{
    public const string UnrecognizedInput = "dialog.unrecognizedInput";

    // Die erste Nachfrage nennt das erkannte Transkript, damit die Person die
    // Erkennung prüfen kann. Ohne Transkript wird die allgemeine Variante verwendet.
    public const string UnrecognizedInputWithTranscript = "dialog.unrecognizedInput.withTranscript";

    public const string UnrecognizedInputSecond = "dialog.unrecognizedInput.second";
    public const string UnrecognizedInputThird = "dialog.unrecognizedInput.third";

    // Kündigt die unmittelbar folgende automatische Hilfe an, statt erst zu ihr aufzufordern.
    public const string UnrecognizedInputThirdBeforeHelp = "dialog.unrecognizedInput.third.beforeHelp";

    // Nennt bei geschlossenen Auswahlfeldern die lokalisierten Optionen und den
    // Hinweis zur Auswahl per Nummer, damit die nächste mögliche Antwort hörbar wird.
    public const string UnrecognizedInputConstrainedOptions = "dialog.unrecognizedInput.constrainedOptions";
    public const string CaptureRecoverableFailure = "dialog.capture.recoverableFailure";

    /// <summary>
    /// Meldet technische Nichtverfügbarkeit der Erkennung. Im Unterschied zu
    /// <see cref="CaptureRecoverableFailure"/> wird damit keine unverständliche
    /// Eingabe behauptet, wenn die Erkennung gar kein Ergebnis liefern konnte.
    /// </summary>
    public const string CaptureRecognizerUnavailable = "dialog.capture.recognizerUnavailable";
    /// <summary>
    /// Bietet die bisher ermittelte Sitzungssprache in dieser Sprache zur Bestätigung an.
    /// Stille gilt nicht als Zustimmung. Anschließend kann eine weitere Sprache angeboten werden.
    /// </summary>
    public const string SessionEntryKeepQuestion = "dialog.session.entry.keepQuestion";

    /// <summary>
    /// Bietet eine Sprache mit einer Frage in genau dieser Sprache an.
    /// Zur Auswahl genügt die zugehörige Ja-Antwort, ohne einen Sprachnamen nennen zu müssen.
    /// </summary>
    public const string SessionEntryLanguageQuestion = "dialog.session.entry.languageQuestion";

    /// <summary>
    /// Bestätigt die ausgewählte Sprache hörbar in dieser Sprache.
    /// Die Ansage teilt die Übernahme mit und verlangt keine weitere Antwort.
    /// </summary>
    public const string SessionEntryAccepted = "dialog.session.entry.accepted";

    /// <summary>
    /// Bestätigt den abgebrochenen Sitzungseinstieg und nennt den Weg zum erneuten Start.
    /// So ist der Abbruch auch ohne Blick auf den Bildschirm erkennbar.
    /// </summary>
    public const string SessionEntryCancelled = "dialog.session.entry.cancelled";

    public const string CaptureNoInput = "dialog.capture.noInput";
    public const string CaptureReplayLeadIn = "dialog.capture.replayLeadIn";
    public const string EmptySummary = "dialog.emptySummary";
    public const string SummaryValueLine = "dialog.summary.valueLine";
    public const string SummaryStoredValue = "dialog.summary.storedValue";
    public const string NoErrors = "dialog.noErrors";
    public const string AutoAdvanceManualContinuation = "dialog.autoAdvance.manualContinuation";
    public const string AutoAdvanceManualContinuationShort = "dialog.autoAdvance.manualContinuation.short";
    public const string CommitAccepted = "dialog.commit.accepted";
    public const string CommitAcceptedNamingValue = "dialog.commit.acceptedNamingValue";

    // Meldet nach einer Wertübernahme die geänderte Zahl beantwortbarer Felder,
    // damit die neue Gesamtzahl nicht erst in der nächsten Positionsansage erscheint.
    public const string ProgressAnswerableTotalChanged = "dialog.progress.answerableTotalChanged";
    public const string NextOpenRequiredFieldMoved = "dialog.nextOpenRequiredField.moved";
    public const string NextOpenRequiredFieldWrapped = "dialog.nextOpenRequiredField.wrapped";
    public const string NextOpenRequiredFieldSoleTarget = "dialog.nextOpenRequiredField.soleTarget";
    public const string NextOpenRequiredFieldNone = "dialog.nextOpenRequiredField.none";
    public const string CommandListSummary = "dialog.commandList.summary";
    public const string CommandListSummaryTruncated = "dialog.commandList.summaryTruncated";
    public const string CommandListNone = "dialog.commandList.none";
    public const string CommandListGroupNavigation = "dialog.commandList.group.navigation";
    public const string CommandListGroupMonitoring = "dialog.commandList.group.monitoring";
    public const string CommandListGroupDiscovery = "dialog.commandList.group.discovery";
    public const string CommandListGroupInputMode = "dialog.commandList.group.inputMode";
    public const string CommandListGroupControl = "dialog.commandList.group.control";
    public const string HelpCommandsPointer = "dialog.help.commandsPointer";
    public const string CommandAnchorHint = "dialog.command.anchorHint";

    // Neutraler Wartehinweis nach abgelehnter Spracheingabe. Er nennt die dort
    // verfügbaren Hilfe- und Abbruchkommandos, statt einen Erkennungsfehler zu behaupten.
    public const string CommandParkWaiting = "dialog.commandPark.waiting";
    public const string JumpToMissingArgument = "dialog.jumpTo.missingArgument";
    public const string JumpToNotFound = "dialog.jumpTo.notFound";

    // Meldet eine nicht verfügbare Rückwärtsnavigation. Da der Vertrag nur einen
    // Erfolgswert liefert, nennt die Ansage keinen ungesicherten Grund.
    public const string BackUnavailable = "dialog.back.unavailable";
    public const string CancelConfirmation = "dialog.cancel.confirmation";
    public const string SingleFormOnly = "dialog.singleFormOnly";
    public const string FormNotFound = "dialog.form.notFound";
    public const string AlreadyOnForm = "dialog.form.alreadyOn";
    public const string FormSubmitted = "dialog.form.submitted";
    public const string EvaluationDeliveryWaiting = "dialog.evaluation.delivery.waiting";
    public const string EvaluationOtherTab = "dialog.evaluation.delivery.otherTab";
    public const string EvaluationPermanentFailure = "dialog.evaluation.delivery.permanentFailure";
    public const string EvaluationLocalCommitWaiting = "dialog.evaluation.delivery.localCommitWaiting";
    public const string EvaluationTaskCancelled = "dialog.evaluation.delivery.taskCancelled";
    public const string EvaluationTechnicalFailure = "dialog.evaluation.delivery.technicalFailure";
    public const string EvaluationRestoredAbandoned = "dialog.evaluation.delivery.restoredAbandoned";
    public const string FormClosingReview = "dialog.form.closingReview";
    public const string SwitchFormConfirmation = "dialog.form.switch.confirmation";
    public const string PrerequisitesNotMetWithoutList = "dialog.form.prerequisites.notMet";
    public const string PrerequisitesNotMetWithList = "dialog.form.prerequisites.notMetWithList";
    public const string ChangeLanguageMissingArgument = "dialog.changeLanguage.missingArgument";
    public const string ChangeLanguageUnsupported = "dialog.changeLanguage.unsupported";
    public const string ChangeLanguageAlreadySelected = "dialog.changeLanguage.alreadySelected";
    public const string ChangeLanguageConfirmation = "dialog.changeLanguage.confirmation";
    public const string ChangeLanguageKept = "dialog.changeLanguage.kept";
    public const string ChangeLanguageWakeWordHint = "dialog.changeLanguage.wakeWordHint";
    public const string SessionWakeWordIntro = "dialog.session.wakeWordIntro";
    public const string StudyEntryStudyAnnouncement = "dialog.study.entry.studyAnnouncement";
    public const string SessionWakeWordRecoveryHint = "dialog.session.wakeWordRecoveryHint";
    public const string SessionWakeWordRecoveryHintAlternatives = "dialog.session.wakeWordRecoveryHintAlternatives";
    public const string SessionMicrophoneUnavailableDeviceNotFound = "dialog.session.microphoneUnavailable.deviceNotFound";
    public const string SessionMicrophoneUnavailablePermissionDenied = "dialog.session.microphoneUnavailable.permissionDenied";
    public const string SessionMicrophoneUnavailableTechnical = "dialog.session.microphoneUnavailable.technical";
    public const string SessionRepeatHint = "dialog.session.repeatHint";
    public const string ChangePolitenessUnavailable = "dialog.changePoliteness.unavailable";
    public const string ChangePolitenessMissingArgument = "dialog.changePoliteness.missingArgument";
    public const string ChangePolitenessUnsupported = "dialog.changePoliteness.unsupported";
    public const string ChangePolitenessAlreadySelected = "dialog.changePoliteness.alreadySelected";
    public const string ChangePolitenessConfirmation = "dialog.changePoliteness.confirmation";
    public const string AllowBargeInEnabled = "dialog.allowBargeIn.enabled";
    public const string AllowBargeInDisabled = "dialog.allowBargeIn.disabled";
    public const string AllowBargeInAlreadyEnabled = "dialog.allowBargeIn.alreadyEnabled";
    public const string AllowBargeInAlreadyDisabled = "dialog.allowBargeIn.alreadyDisabled";
    public const string InputModeChangedVoice = "dialog.inputMode.changed.voice";
    public const string InputModeChangedSpelling = "dialog.inputMode.changed.spelling";
    public const string InputModeChangedSpellingShort = "dialog.inputMode.changed.spelling.short";
    public const string InputModeChangedDigits = "dialog.inputMode.changed.digits";
    public const string InputModeChangedKeyboard = "dialog.inputMode.changed.keyboard";
    public const string SilentInputReady = "dialog.silentInput.ready";
    public const string SilentInputUnavailable = "dialog.silentInput.unavailable";
    public const string SilentInputStateChanged = "dialog.silentInput.stateChanged";
    public const string InputModeChangedLiteral = "dialog.inputMode.changed.literal";
    public const string InputModeUnavailable = "dialog.inputMode.unavailable";
    public const string LiteralModeCommandBlocked = "dialog.literalMode.commandBlocked";
    public const string VoiceInputForbiddenDefault = "dialog.voiceInput.forbidden.default";
    public const string VoiceInputForbiddenWithFallback = "dialog.voiceInput.forbidden.withFallback";
    public const string ValidationInvalidValue = "dialog.validation.invalidValue";
    public const string ValidationContext = "dialog.validation.context";
    public const string ValidationContextWithoutValue = "dialog.validation.context.withoutValue";

    // Ergänzt Validierungsmeldungen ohne eigene Abhilfe um einen Reparaturhinweis.
    // Der invalidValue-Fallback enthält ihn bereits und darf ihn nicht doppelt erhalten.
    public const string ValidationRepairPrompt = "dialog.validation.repairPrompt";
    public const string ReadbackOptIn = "dialog.readback.optIn";
    // Angebot zur Übernahme eines zusätzlich genannten Feldwerts und Rückmeldungen
    // zu Übernahme, Nichtverfügbarkeit, ungültigem Wert und Überspringen. Die separate
    // Unverändert-Ansage ergänzt Fehlertexte, die dies nicht bereits ausdrücken.
    public const string AdditionalFieldProposalOffer = "dialog.additionalField.proposal.offer";
    public const string AdditionalFieldProposalApplied = "dialog.additionalField.proposal.applied";
    public const string AdditionalFieldProposalUnavailable = "dialog.additionalField.proposal.unavailable";
    public const string AdditionalFieldProposalInvalid = "dialog.additionalField.proposal.invalid";
    public const string AdditionalFieldProposalSkipped = "dialog.additionalField.proposal.skipped";
    public const string AdditionalFieldProposalUnchanged = "dialog.additionalField.proposal.unchanged";
    public const string ReparableConfirmation = "dialog.confirmation.reparable";
    public const string ReparableShortConfirmation = "dialog.confirmation.reparable.short";
    public const string ReparableConfirmationNoSpell = "dialog.confirmation.reparable.noSpell";
    public const string ReparableShortConfirmationNoSpell = "dialog.confirmation.reparable.short.noSpell";
    public const string ReparableSpellBackConfirmation = "dialog.confirmation.reparable.spellBack";

    // Unterscheidet bei der erneuten Bestätigungsfrage eine fehlende von einer
    // unpassenden Antwort. Die kurze Frage folgt in derselben Ansage.
    public const string ConfirmationReaskSilence = "dialog.confirmation.reask.silence";
    public const string ConfirmationReaskUnrecognized = "dialog.confirmation.reask.unrecognized";

    // Sprachabhängiger Anfang der Bestätigung, der Feld und gegebenenfalls Wert
    // benennt. Eine eigene Textressource erlaubt dafür einen natürlichen Satz je Sprache.
    public const string ConfirmationPrefixWithValue = "dialog.confirmation.prefix.withValue";
    public const string ConfirmationPrefixLabelOnly = "dialog.confirmation.prefix.labelOnly";
    public const string CorrectionRequested = "dialog.correction.requested";
    public const string SilentInputOfferDefault = "dialog.silentInput.offer.default";
    public const string SilentInputOfferWithFallback = "dialog.silentInput.offer.withFallback";
    public const string AmbiguousInputVsCommandWithLabel = "dialog.ambiguity.inputVsCommand.withLabel";
    public const string AmbiguousInputVsCommandWithoutLabel = "dialog.ambiguity.inputVsCommand.withoutLabel";
    public const string DigitsWorkflowEmpty = "dialog.digits.workflow.empty";
    public const string DigitsWorkflowCaptured = "dialog.digits.workflow.captured";
    public const string DigitsNoValue = "dialog.digits.noValue";
    public const string DigitsLastDigitRemoved = "dialog.digits.lastDigitRemoved";
    public const string DigitsLastBlockRemoved = "dialog.digits.lastBlockRemoved";
    public const string DigitsCurrentReadback = "dialog.digits.currentReadback";
    public const string DigitsRedictateBlock = "dialog.digits.redictateBlock";
    public const string DigitsContinueOrDone = "dialog.digits.continueOrDone";
    public const string SetSelectionConfirm = "dialog.set.selectionConfirm";
    public const string SetEmptySelectionValue = "dialog.set.emptySelectionValue";
    public const string SpokenValueUnavailable = "dialog.spokenValue.unavailable";
    public const string SetNoneSelectedYet = "dialog.set.noneSelectedYet";
    public const string SetSelectionHint = "dialog.set.selectionHint";
    public const string SetAccumulationStructure = "dialog.field.setAccumulationStructure";
    public const string SpellingWorkflowEmpty = "dialog.spelling.workflow.empty";
    public const string SpellingWorkflowCaptured = "dialog.spelling.workflow.captured";
    public const string SpellingNoValue = "dialog.spelling.noValue";
    public const string SpellingLastCharacterRemoved = "dialog.spelling.lastCharacterRemoved";
    public const string SpellingLastBlockRemoved = "dialog.spelling.lastBlockRemoved";
    public const string SpellingBufferCleared = "dialog.spelling.bufferCleared";
    public const string SpellingCurrentReadback = "dialog.spelling.currentReadback";
    public const string SpellingReadbackUnavailable = "dialog.spelling.readbackUnavailable";
    public const string SpellingContinueOrDone = "dialog.spelling.continueOrDone";
    public const string SpellingContinueOrDoneProtected = "dialog.spelling.continueOrDone.protected";

    // Bietet bei einem abgelehnten Buchstabierergebnis Korrektur oder erneutes
    // Buchstabieren an. Eine Übernahme des abgelehnten Kandidaten wird nicht angeboten.
    public const string SpellingLowConfidenceRepair = "dialog.spelling.lowConfidenceRepair";
    public const string SilentInputLabelKeyboard = "dialog.silentInput.label.keyboard";
    public const string SilentInputLabelBraille = "dialog.silentInput.label.braille";
    public const string CancelRefractory = "dialog.cancel.refractory";
    public const string OptionsNone = "dialog.options.none";
    public const string OptionsList = "dialog.options.list";
    public const string FormListItem = "dialog.status.formList.item";
    public const string FormListSummary = "dialog.status.formList.summary";
    public const string DiscoveryFieldGroupListItem = "dialog.discovery.fieldGroups.item";
    public const string DiscoveryFieldGroupListSummary = "dialog.discovery.fieldGroups.summary";
    public const string DiscoveryFieldGroupListNone = "dialog.discovery.fieldGroups.none";
    public const string DiscoveryFieldListItem = "dialog.discovery.fields.item";
    public const string DiscoveryFieldListSummary = "dialog.discovery.fields.summary";
    public const string DiscoveryFieldListNone = "dialog.discovery.fields.none";
    public const string FormStatusSingle = "dialog.status.form.single";
    public const string FormStatusMulti = "dialog.status.form.multi";
    public const string StatusSummary = "dialog.status.summary";
    public const string StatusProgressRequiredWithTotal = "dialog.status.progress.required.withTotal";
    public const string StatusProgressRequiredNone = "dialog.status.progress.required.none";

    /// <summary>
    /// Nennt optionale Felder zusätzlich zum Pflichtfeldfortschritt.
    /// Damit wird hörbar, warum die Gesamtzahl der Positionsansage von der
    /// Gesamtzahl der Pflichtfelder abweichen kann.
    /// </summary>
    public const string StatusProgressOptionalOne = "dialog.status.progress.optional.one";

    public const string StatusProgressOptionalMany = "dialog.status.progress.optional.many";
    public const string StatusProgressFieldGroupWithTotal = "dialog.status.progress.fieldGroup.withTotal";
    public const string StatusProgressFieldGroupNone = "dialog.status.progress.fieldGroup.none";
    public const string StatusProgressFieldWithTotal = "dialog.status.progress.field.withTotal";
    public const string StatusProgressFieldNone = "dialog.status.progress.field.none";
    public const string StatusFieldGroupWithIndex = "dialog.status.fieldGroup.withIndex";
    public const string StatusFieldGroupWithIndexNoTitle = "dialog.status.fieldGroup.withIndexNoTitle";
    public const string StatusFieldGroupWithoutIndex = "dialog.status.fieldGroup.withoutIndex";
    public const string StatusFieldGroupNone = "dialog.status.fieldGroup.none";
    public const string FieldGroupIntro = "dialog.fieldGroup.intro";
    public const string FieldPosition = "dialog.field.position";
    public const string DateFieldEntryExample = "dialog.field.dateExample";
    public const string DateFieldEntryDecompositionHint = "dialog.field.dateEntryDecompositionHint";
    public const string TimeFieldEntryExample = "dialog.field.timeExample";
    public const string TimeFieldEntryStructureHint = "dialog.field.timeStructure";
    public const string SpellingOverrideAvailable = "dialog.field.spellingOverrideAvailable";
    public const string DigitsModeAvailable = "dialog.field.digitsModeAvailable";

    // Ansagen für schreibgeschützte Felder: automatische Ankündigung, eine Variante
    // nur mit Feldbezeichnung bei nicht zum Vorlesen freigegebenen Werten,
    // Vorlesen auf Anfrage und Rückmeldung bei leerem Wert.
    public const string ReadOnlyFieldAnnouncement = "dialog.field.readOnly.announcement";
    public const string ReadOnlyFieldAnnouncementLabelOnly = "dialog.field.readOnly.announcement.labelOnly";
    public const string ReadOnlyFieldReadback = "dialog.field.readOnly.readback";
    public const string ReadOnlyFieldEmptyValue = "dialog.field.readOnly.emptyValue";

    // Fragt Tag, Monat und Jahr getrennt ab. Eigene Ansagen unterstützen die
    // erneute Nachfrage zu einer Komponente, den Abschluss und die Einführung des Modus.
    public const string DateComponentDayPrompt = "dialog.field.dateComponent.dayPrompt";
    public const string DateComponentMonthPrompt = "dialog.field.dateComponent.monthPrompt";
    public const string DateComponentYearPrompt = "dialog.field.dateComponent.yearPrompt";
    public const string DateComponentDayReask = "dialog.field.dateComponent.dayReask";
    public const string DateComponentMonthReask = "dialog.field.dateComponent.monthReask";
    public const string DateComponentDayNotRecognizedReask = "dialog.field.dateComponent.dayNotRecognizedReask";
    public const string DateComponentMonthNotRecognizedReask = "dialog.field.dateComponent.monthNotRecognizedReask";
    public const string DateComponentYearNotRecognizedReask = "dialog.field.dateComponent.yearNotRecognizedReask";
    public const string DateComponentYearFourDigitsReask = "dialog.field.dateComponent.yearFourDigitsReask";
    public const string DateComponentYearUncertainReask = "dialog.field.dateComponent.yearUncertainReask";
    public const string DateComponentImpossibleDateReask = "dialog.field.dateComponent.impossibleDateReask";
    public const string DateComponentExitCue = "dialog.field.dateComponent.exitCue";
    public const string DateComponentEntryAvailable = "dialog.field.dateComponentAvailable";

    public const string FormOrientation = "dialog.form.orientation";

    // Nennt am Formulareinstieg den Hilfe-Befehl. Die Erklärung des Befehlspräfixes
    // bleibt bei der Feldhilfe.
    public const string OrientationHelpExample = "dialog.form.orientation.helpExample";
    public const string StatusCurrentFieldNone = "dialog.status.currentField.none";
    public const string StatusAddressingPrefix = "dialog.status.addressingPrefix";
    public const string StatusFormPosition = "dialog.status.formPosition";
    public const string SharedVariableAnnouncementSourceAndLabels = "dialog.sharedVariables.announcement.sourceAndLabels";
    public const string SharedVariableAnnouncementSourceOnly = "dialog.sharedVariables.announcement.sourceOnly";
    public const string SharedVariableAnnouncementLabelsOnly = "dialog.sharedVariables.announcement.labelsOnly";
    public const string SharedVariableAnnouncementDefault = "dialog.sharedVariables.announcement.default";
    public const string PrivacyReadbackEmpty = "privacy.readback.empty";
    public const string PrivacyReadbackSecretLength = "privacy.readback.secretLength";
    public const string PrivacyReadbackSensitiveSuffix = "privacy.readback.sensitiveSuffix";
}
