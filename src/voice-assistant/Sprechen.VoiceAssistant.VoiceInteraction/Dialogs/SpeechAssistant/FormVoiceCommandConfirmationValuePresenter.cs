namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Erstellt die Bestätigungsfrage für einen Feldwert unter Beachtung der
/// Vorleseregeln. Der Wert erscheint vollständig, maskiert oder gar nicht.
/// Die Erklärung zulässiger Antworten wird nach den ersten Bestätigungen gekürzt.
/// </summary>
internal sealed class FormVoiceCommandConfirmationValuePresenter
{
    // Die ersten zwei Bestätigungen einer Formularsitzung erklären die
    // Antwortmöglichkeiten ausführlich. Der Zähler wird vor dem Lesen erhöht
    // und beginnt daher bei eins. Der Vergleich verwendet kleiner oder gleich.
    internal const int FullReparableTailTurns = 2;

    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly IPhoneticSpellOutRenderer _spellOutRenderer;
    private readonly FormConfirmationReadbackValueFormatter _valueFormatter;

    public FormVoiceCommandConfirmationValuePresenter(
        IFormVoiceCommandSpeechService speechService,
        IPhoneticSpellOutRenderer spellOutRenderer)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _spellOutRenderer = spellOutRenderer ?? throw new ArgumentNullException(nameof(spellOutRenderer));
        _valueFormatter = new FormConfirmationReadbackValueFormatter(_spellOutRenderer);
    }

    public async Task<FormVoiceCommandConfirmationPrompt> BuildAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(request);

        var context = turnContext.Context;
        var field = turnContext.Field;
        var speakFullValue = await ResolveSpeakFullValueAsync(turnContext, cancellationToken)
            .ConfigureAwait(false);
        var projection = await BuildValueAsync(
                turnContext.Interpreter,
                turnContext.PrivacyReadbackPolicy,
                field,
                request.TranscriptForFallback,
                context.IsoCode,
                speakFullValue)
            .ConfigureAwait(false);
        var value = projection.SpokenValue ?? string.Empty;
        var includesValue = IncludesSpokenPayload(projection);

        var politeness = context.VoiceSettings.GetPolitenessLevel();
        if (projection.Status == FormSpokenValueProjectionStatus.Unavailable)
        {
            var unavailablePrompt = FormConfirmationPromptBuilder.GetSpokenValueUnavailablePrompt(
                context.Contract,
                context.IsoCode,
                politeness);
            return new FormVoiceCommandConfirmationPrompt(
                unavailablePrompt,
                value,
                IncludesValue: true,
                FormSpokenValueProjectionStatus.Unavailable);
        }

        // Nach den ersten Bestätigungen die kürzere Erklärung verwenden. Nach
        // Buchstabier- oder Zifferneingabe wird Buchstabieren nicht erneut angeboten.
        var offersSpell = !request.FromSpellingOrDigits && OffersSpellRepairVerb(field);
        var prompt = UsesFullReparableTail(turnContext.StateTracker.ReparableConfirmationTurnCount)
            ? FormConfirmationPromptBuilder.GetReparableConfirmationPrompt(
                context.Contract,
                context.IsoCode,
                field.Label,
                value,
                includesValue,
                politeness,
                offersSpell)
            : FormConfirmationPromptBuilder.GetShortReparableConfirmationPrompt(
                context.Contract,
                context.IsoCode,
                field.Label,
                value,
                includesValue,
                politeness,
                offersSpell);

        return new FormVoiceCommandConfirmationPrompt(prompt, value, includesValue, projection.Status, offersSpell);
    }

    /// <summary>
    /// Erstellt eine kurze Rückfrage mit einem zum Grund passenden Hinweis.
    /// Wert und angebotene Korrekturmöglichkeiten stammen aus der ersten Frage.
    /// Es wird weder erneut eine Vorlesefreigabe eingeholt noch der
    /// Bestätigungszähler erhöht.
    /// </summary>
    public FormVoiceCommandConfirmationPrompt BuildReask(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandConfirmationPrompt firstPrompt,
        ConfirmationUndecidedReason reason)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(firstPrompt);

        var context = turnContext.Context;
        var politeness = context.VoiceSettings.GetPolitenessLevel();
        var leadIn = FormConfirmationPromptBuilder.GetConfirmationReaskPrompt(
            context.Contract,
            context.IsoCode,
            reason,
            politeness);
        var shortPrompt = FormConfirmationPromptBuilder.GetShortReparableConfirmationPrompt(
            context.Contract,
            context.IsoCode,
            turnContext.Field.Label,
            firstPrompt.Value,
            firstPrompt.IncludesValue,
            politeness,
            firstPrompt.OffersSpell);

        return firstPrompt with { Prompt = $"{leadIn.Trim()} {shortPrompt.Trim()}" };
    }

    /// <summary>
    /// Verwendet bis einschließlich <see cref="FullReparableTailTurns"/> die
    /// ausführliche Erklärung der Antwortmöglichkeiten. Der Aufrufer erhöht
    /// den Sitzungszähler vor dieser Prüfung. Danach folgt die Kurzfassung.
    /// </summary>
    internal static bool UsesFullReparableTail(int reparableConfirmationTurnCount)
        => reparableConfirmationTurnCount <= FullReparableTailTurns;

    // Buchstabieren nur für Text- und Codeeingaben anbieten. Andere
    // Interaktionstypen erhalten die Bestätigung ohne diese Korrekturmöglichkeit.
    private static bool OffersSpellRepairVerb(FormFieldSnapshot field)
        => FieldInteractionSemantics.ResolveEffectiveInteractionType(field.CodeStyle, field.ValueTypeKind)
            is EffectiveFieldInteractionTypes.Text or EffectiveFieldInteractionTypes.Code;

    /// <summary>
    /// Bereitet den verstandenen Wert auch außerhalb einer Bestätigungsfrage
    /// zum Vorlesen auf. Dafür gelten dieselben Regeln wie bei der Bestätigung.
    /// Es wird keine zusätzliche Zustimmung zum Vorlesen erfragt. Ist die
    /// Wiedergabe im Feld nicht freigegeben, entsteht
    /// <see cref="FormVoiceCommandUnderstoodValue.WithoutValue"/>.
    /// </summary>
    public async Task<FormVoiceCommandUnderstoodValue> BuildUnderstoodValueAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        string transcript)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        // Ein leerer Text ist für eine ausdrücklich leere optionale Mehrfachauswahl
        // zulässig. Nur null wird zurückgewiesen.
        ArgumentNullException.ThrowIfNull(transcript);

        var field = turnContext.Field;
        if (!field.SpeakValueInConfirmation)
        {
            return FormVoiceCommandUnderstoodValue.WithoutValue;
        }

        var projection = await BuildValueAsync(
                turnContext.Interpreter,
                turnContext.PrivacyReadbackPolicy,
                field,
                transcript,
                turnContext.Context.IsoCode,
                speakFullValue: true)
            .ConfigureAwait(false);

        var value = projection.SpokenValue ?? string.Empty;
        return IncludesSpokenPayload(projection)
            ? new FormVoiceCommandUnderstoodValue(value, IncludesValue: true, projection.Status)
            : FormVoiceCommandUnderstoodValue.WithoutValue with { Status = projection.Status };
    }

    private async Task<bool> ResolveSpeakFullValueAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken)
    {
        var context = turnContext.Context;
        var privacyReadbackPolicy = turnContext.PrivacyReadbackPolicy;
        var field = turnContext.Field;

        if (field.SpeakValueInConfirmation
            || !await privacyReadbackPolicy.RequiresReadbackOptInAsync(field.VariableId).ConfigureAwait(false))
        {
            return field.SpeakValueInConfirmation;
        }

        var readbackOptIn = await _speechService.ConfirmYesNoAsync(
                context.CaptureSettings,
                FormInputFeedbackPromptBuilder.GetReadbackOptInPrompt(
                    context.Contract,
                    context.IsoCode,
                    field.Label,
                    context.VoiceSettings.GetPolitenessLevel()),
                context.IsoCode,
                new SpeechCaptureTelemetryContext(turnContext.Session),
                cancellationToken)
            .ConfigureAwait(false);

        return readbackOptIn is true
            && await privacyReadbackPolicy.CanSpeakValueInConfirmationAsync(field.VariableId, explicitOptIn: true).ConfigureAwait(false);
    }

    private async Task<FormSpokenValueProjection> BuildValueAsync(
        IFormDialogRuntime interpreter,
        IFormDialogPrivacyReadbackPolicy privacyReadbackPolicy,
        FormFieldSnapshot field,
        string transcript,
        string isoCode,
        bool speakFullValue)
    {
        var fallback = transcript.Trim();
        var valuesByVariableId = await interpreter.GetValuesSnapshotAsync().ConfigureAwait(false);
        var hasStoredValue = valuesByVariableId.TryGetValue(field.VariableId, out var canonicalValue);
        var isSet = string.Equals(field.ValueTypeKind, ValueTypeKinds.Set, StringComparison.OrdinalIgnoreCase);

        // Nur an dieser Stelle steht das ursprüngliche Transkript als Ersatz bereit.
        // Eine gespeicherte leere Mehrfachauswahl ist jedoch eine gültige Antwort
        // und darf nicht durch diesen Ersatztext überschrieben werden.
        string? toProject;
        if (hasStoredValue && string.IsNullOrWhiteSpace(canonicalValue) && isSet)
        {
            toProject = string.Empty;
        }
        else if (hasStoredValue && !string.IsNullOrWhiteSpace(canonicalValue))
        {
            toProject = canonicalValue;
        }
        else
        {
            toProject = fallback;
        }

        var disposition = speakFullValue
            ? FormSpokenValueSpeakDisposition.SpeakFull
            : FormSpokenValueSpeakDisposition.Mask;
        string? maskedWording = null;
        if (disposition == FormSpokenValueSpeakDisposition.Mask
            && !string.IsNullOrEmpty(toProject))
        {
            maskedWording = await privacyReadbackPolicy
                .BuildMaskedReadbackAsync(field.VariableId, toProject, isoCode)
                .ConfigureAwait(false);
        }

        var projection = new FormSpokenValueProjector(isoCode).Project(
            toProject,
            field.ValueTypeKind,
            disposition,
            field.Options,
            maskedWording);

        if (projection.Status == FormSpokenValueProjectionStatus.FullValue
            && speakFullValue
            && !string.IsNullOrWhiteSpace(projection.SpokenValue))
        {
            var allowSpellOut = await privacyReadbackPolicy
                .IsSpellOutAllowedInConfirmationAsync(field.VariableId)
                .ConfigureAwait(false);
            var formatted = _valueFormatter.Format(
                projection.SpokenValue,
                field.InputMode,
                allowSpellOut,
                isoCode,
                projection.ValueResolvedFromOption);
            return projection with { SpokenValue = formatted };
        }

        return projection;
    }

    private static bool IncludesSpokenPayload(FormSpokenValueProjection projection)
        => projection.Status switch
        {
            FormSpokenValueProjectionStatus.FullValue
                or FormSpokenValueProjectionStatus.Empty
                or FormSpokenValueProjectionStatus.Masked
                => !string.IsNullOrWhiteSpace(projection.SpokenValue),
            FormSpokenValueProjectionStatus.Unavailable => true,
            _ => false
        };
}
