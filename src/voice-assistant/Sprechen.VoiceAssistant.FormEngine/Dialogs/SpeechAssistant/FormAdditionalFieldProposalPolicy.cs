using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bereitet die freiwillige Übernahme zusätzlich genannter Feldwerte vor und prüft Ziel, Datenschutz und Parsing. Erst beim Bestätigen werden Regeln und Validierung auf einer Zustandskopie ausgeführt. Übernommen wird nur ein gültiges Ergebnis.
/// </summary>
internal sealed class FormAdditionalFieldProposalPolicy
{
    private readonly Dictionary<string, FormAdditionalFieldProposalPendingProposal> _pendingByProposalId
        = new(StringComparer.Ordinal);

    private readonly FormDialogDefinitionState _definitionState;
    private readonly FormMutableStateStore _mutableState;
    private readonly FormDialogDependencies _services;
    private readonly FormAdditionalFieldProposalTransactionFactory _transactionFactory;
    private readonly FormSpokenValueProjector _spokenValueProjector;
    private readonly FormErrorViolationFilter _errorViolationFilter = new();

    internal FormAdditionalFieldProposalPolicy(
        FormDialogDefinitionState definitionState,
        FormMutableStateStore mutableState,
        FormDialogDependencies services,
        FormAdditionalFieldProposalTransactionFactory transactionFactory)
    {
        _definitionState = definitionState ?? throw new ArgumentNullException(nameof(definitionState));
        _mutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _transactionFactory = transactionFactory ?? throw new ArgumentNullException(nameof(transactionFactory));
        _spokenValueProjector = new FormSpokenValueProjector(definitionState.RequestedIsoCode);
    }

    internal FormAdditionalFieldProposalPreparation Prepare(FormAdditionalFieldValueCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (!TryResolveTarget(candidate.FieldId, out var variableId)
            || !IsTargetOpenForVoiceInput(candidate.FieldId, variableId))
        {
            return Unprepared(FormAdditionalFieldProposalPreparationStatus.Unavailable);
        }

        if (!TryParseCandidateValue(candidate.FieldId, variableId, candidate.ValueSegment, out var canonicalValue))
        {
            return Unprepared(FormAdditionalFieldProposalPreparationStatus.Invalid);
        }

        var spokenOffer = ProjectSpokenOffer(variableId, canonicalValue, candidate);
        if (spokenOffer is null)
        {
            return Unprepared(FormAdditionalFieldProposalPreparationStatus.Unavailable);
        }

        var proposalId = Guid.NewGuid().ToString("N");
        _pendingByProposalId[proposalId] = new FormAdditionalFieldProposalPendingProposal(
            proposalId,
            candidate.FieldId,
            variableId,
            candidate.ValueSegment,
            canonicalValue,
            _mutableState.StateRevision);

        return new FormAdditionalFieldProposalPreparation(
            FormAdditionalFieldProposalPreparationStatus.Prepared,
            spokenOffer,
            new FormAdditionalFieldProposalCommitToken(proposalId));
    }

    internal FormAdditionalFieldValueAdoptionResult Commit(FormAdditionalFieldProposalCommitToken commitToken)
    {
        ArgumentNullException.ThrowIfNull(commitToken);

        // Das Angebot wird vor der Verarbeitung entfernt, damit auch ein fehlgeschlagener Übernahmeversuch nicht wiederholt werden kann.
        if (!TryConsumePendingProposal(commitToken, out var pending))
        {
            return NotAdopted(commitToken.ProposalId, FormAdditionalFieldValueAdoptionStatus.Unavailable);
        }

        if (pending.StateRevision != _mutableState.StateRevision
            || !IsTargetOpenForVoiceInput(pending.FieldId, pending.VariableId))
        {
            return NotAdopted(pending.ProposalId, FormAdditionalFieldValueAdoptionStatus.Unavailable);
        }

        if (!TryParseCandidateValue(pending.FieldId, pending.VariableId, pending.ValueSegment, out var canonicalValue))
        {
            return NotAdopted(pending.ProposalId, FormAdditionalFieldValueAdoptionStatus.Invalid);
        }

        // Zusätzlich zur Formularrevision muss der erneut geparste Wert mit dem angebotenen Wert übereinstimmen.
        if (!string.Equals(canonicalValue, pending.CanonicalValue, StringComparison.Ordinal))
        {
            return NotAdopted(pending.ProposalId, FormAdditionalFieldValueAdoptionStatus.Unavailable);
        }

        var transaction = _transactionFactory.Begin();
        var inputResult = transaction.ApplyToField(pending.FieldId, pending.ValueSegment);
        if (!inputResult.ValueCaptured)
        {
            return NotAdopted(pending.ProposalId, FormAdditionalFieldValueAdoptionStatus.Invalid);
        }

        var errorViolations = _errorViolationFilter.Filter(inputResult.Violations);
        if (errorViolations.Length > 0)
        {
            return NotAdopted(
                pending.ProposalId,
                FormAdditionalFieldValueAdoptionStatus.BlockedByValidation,
                errorViolations[0]);
        }

        _mutableState.RestoreSnapshot(transaction.TakeSnapshot());

        // Erst nach Übernahme des Zustands werden zurückgehaltene Meldungen zur überschrittenen Regeliterationsgrenze weitergereicht.
        transaction.ReplayLoopGuardBreaches();
        return new FormAdditionalFieldValueAdoptionResult(
            pending.ProposalId,
            _mutableState.StateRevision,
            FormAdditionalFieldValueAdoptionStatus.Applied);
    }

    internal void Abandon(FormAdditionalFieldProposalCommitToken commitToken)
    {
        ArgumentNullException.ThrowIfNull(commitToken);
        _ = TryConsumePendingProposal(commitToken, out _);
    }

    private bool TryConsumePendingProposal(
        FormAdditionalFieldProposalCommitToken commitToken,
        out FormAdditionalFieldProposalPendingProposal pending)
    {
        pending = null!;
        return !string.IsNullOrWhiteSpace(commitToken.ProposalId)
            && _pendingByProposalId.Remove(commitToken.ProposalId, out pending!);
    }

    private bool TryResolveTarget(FieldId fieldId, out string variableId)
    {
        variableId = string.Empty;
        return _definitionState.FieldsById.ContainsKey(fieldId)
            && _definitionState.FieldToVariableId.TryGetValue(fieldId, out variableId!);
    }

    // Das Zielfeld muss bearbeitbar und für Spracheingabe freigegeben sein. Auch das Angebot darf die Datenschutzregeln nicht umgehen.
    private bool IsTargetOpenForVoiceInput(FieldId fieldId, string variableId)
        => _services.VisibilityService.IsFieldInputAvailable(fieldId)
            && _services.PrivacyReadbackPolicy.IsVoiceInputAllowed(variableId).Allowed;

    private bool TryParseCandidateValue(
        FieldId fieldId,
        string variableId,
        string valueSegment,
        out string canonicalValue)
    {
        canonicalValue = string.Empty;
        if (string.IsNullOrWhiteSpace(valueSegment)
            || !_definitionState.FieldsById.TryGetValue(fieldId, out var field))
        {
            return false;
        }

        var valueTypeKind = ResolveValueTypeKind(variableId);
        var options = _definitionState.LocalizationResolver.ResolveChoiceOptions(variableId);
        var preferredInputModes = _services.InputModeAvailabilityPolicy.ResolvePreferredInputModes(field, field.FieldId);

        return _services.ValueParser.TryParseInput(
            field.CodeStyle,
            valueTypeKind,
            valueSegment,
            options,
            preferredInputModes,
            out canonicalValue);
    }

    private string? ProjectSpokenOffer(
        string variableId,
        string canonicalValue,
        FormAdditionalFieldValueCandidate candidate)
    {
        var maySpeakValue = _services.PrivacyReadbackPolicy.CanSpeakValueInConfirmation(variableId, explicitOptIn: false);
        var projection = _spokenValueProjector.Project(
            canonicalValue,
            ResolveValueTypeKind(variableId),
            maySpeakValue ? FormSpokenValueSpeakDisposition.SpeakFull : FormSpokenValueSpeakDisposition.Mask,
            candidate.Options,
            maySpeakValue
                ? null
                : _services.PrivacyReadbackPolicy.BuildMaskedReadback(
                    variableId,
                    canonicalValue,
                    _definitionState.RequestedIsoCode));

        // Ohne vorlesbaren Wert kann keine verständliche Übernahmefrage angeboten werden.
        return projection.Status == FormSpokenValueProjectionStatus.Unavailable
            || string.IsNullOrWhiteSpace(projection.SpokenValue)
            ? null
            : projection.SpokenValue;
    }

    private string ResolveValueTypeKind(string variableId)
        => FormVariableValueReader.TryGetValueTypeKind(_definitionState.VariablesById, variableId)
            ?? ValueTypeKinds.String;

    private static FormAdditionalFieldProposalPreparation Unprepared(
        FormAdditionalFieldProposalPreparationStatus status)
        => new(status, SpokenOffer: null, CommitToken: null);

    private static FormAdditionalFieldValueAdoptionResult NotAdopted(
        string proposalId,
        FormAdditionalFieldValueAdoptionStatus status,
        FormConstraintViolation? blockingViolation = null)
        => new(proposalId, CommitRevision: 0, status, blockingViolation);
}
