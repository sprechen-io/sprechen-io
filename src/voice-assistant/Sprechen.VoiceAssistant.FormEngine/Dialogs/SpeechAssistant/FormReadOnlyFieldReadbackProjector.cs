using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.Mda.Contracts.V1.Variables;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Projiziert gespeicherte Feldwerte und Datenschutzfreigaben in Vorleseinhalte für
/// einzelne Ziele und Feldgruppen. Die Ergebnisse dienen Sprung- und Einstiegsansagen.
/// Satzbildung und tatsächliche Wiedergabe bleiben bei deren Aufrufern.
/// </summary>
public sealed class FormReadOnlyFieldReadbackProjector
{
    private readonly IFormDialogRuntimeState _runtimeState;
    private readonly IFormDialogTrustedValueState _trustedValues;
    private readonly IFormDialogSurfaceState _surfaceState;
    private readonly IFormDialogPrivacyReadbackPolicy _privacyReadbackPolicy;
    private readonly FormSpokenValueProjector _spokenValueProjector;
    private readonly FormChoiceOptionsResolver _choiceOptionsResolver;
    private readonly ImmutableDictionary<VariableId, VariableDefinition> _variablesById;
    private readonly string _isoCode;

    public FormReadOnlyFieldReadbackProjector(
        IFormDialogRuntimeState runtimeState,
        IFormDialogTrustedValueState trustedValues,
        IFormDialogSurfaceState surfaceState,
        IFormDialogPrivacyReadbackPolicy privacyReadbackPolicy,
        string isoCode)
    {
        _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
        _trustedValues = trustedValues ?? throw new ArgumentNullException(nameof(trustedValues));
        _surfaceState = surfaceState ?? throw new ArgumentNullException(nameof(surfaceState));
        _privacyReadbackPolicy = privacyReadbackPolicy ?? throw new ArgumentNullException(nameof(privacyReadbackPolicy));
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        _isoCode = isoCode.Trim();
        _spokenValueProjector = new FormSpokenValueProjector(_isoCode);
        _variablesById = runtimeState.Contract.Variables.ToImmutableDictionary(variable => variable.VariableId);
        _choiceOptionsResolver = new FormChoiceOptionsResolver(_variablesById, ResolveLocalizedText);
    }

    /// <summary>
    /// Projiziert Bezeichnung und gegebenenfalls freigegebenen Wert des angegebenen Felds.
    /// Eine fehlende oder ohne Einwilligung nicht vorlesbare Angabe erhält den entsprechenden
    /// Inhaltsstatus. Eine gesonderte Einwilligung wird an diesem Einstieg nicht eingeholt.
    /// </summary>
    public async Task<FormReadOnlyFieldReadback> ProjectFieldReadbackAsync(string fieldId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);

        var field = ResolveField(fieldId)
            ?? throw new InvalidOperationException($"Field '{fieldId}' is not defined in the form contract.");
        var values = await _trustedValues.GetValuesSnapshotAsync().ConfigureAwait(false);

        return await ProjectAsync(field, values).ConfigureAwait(false);
    }

    /// <summary>
    /// Projiziert die gespeicherten Inhalte direkt zugeordneter schreibgeschützter Felder
    /// der angegebenen Gruppe. Jedes Feld durchläuft dieselbe Datenschutzprüfung wie das Einzelziel.
    /// </summary>
    public Task<ImmutableArray<FormReadOnlyFieldReadback>> ProjectFieldGroupReadbacksAsync(string fieldGroupId)
        => ProjectFieldGroupAsync(fieldGroupId);

    /// <summary>
    /// Verwendet die Gruppenprojektion für den Ansageaufruf beim Gruppeneinstieg, damit
    /// Inhaltsauswahl und Datenschutzprüfung mit der gezielten Gruppenabfrage übereinstimmen.
    /// </summary>
    public Task<ImmutableArray<FormReadOnlyFieldReadback>> ProjectFieldGroupAnnouncementsAsync(string fieldGroupId)
        => ProjectFieldGroupAsync(fieldGroupId);

    private async Task<ImmutableArray<FormReadOnlyFieldReadback>> ProjectFieldGroupAsync(string fieldGroupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldGroupId);
        var normalizedFieldGroupId = fieldGroupId;

        // Die Feldfolge aus der Statusprojektion übernehmen und mit dem getrennt
        // projizierten Wertbestand verbinden.
        var detailedStatus = await _surfaceState.GetDetailedStatusSnapshotAsync().ConfigureAwait(false);
        var values = await _trustedValues.GetValuesSnapshotAsync().ConfigureAwait(false);

        var readbacks = ImmutableArray.CreateBuilder<FormReadOnlyFieldReadback>();
        foreach (var fieldStatus in detailedStatus.Fields)
        {
            if (!string.Equals(fieldStatus.FieldGroupId, normalizedFieldGroupId, StringComparison.Ordinal))
            {
                continue;
            }

            var field = ResolveField(fieldStatus.FieldId);
            if (field is null || field.Editability is not FieldEditability.ReadOnly)
            {
                continue;
            }

            if (!TryGetStoredValue(values, field, out _))
            {
                continue;
            }

            readbacks.Add(await ProjectAsync(field, values).ConfigureAwait(false));
        }

        return readbacks.ToImmutable();
    }

    private async Task<FormReadOnlyFieldReadback> ProjectAsync(
        FieldDefinition field,
        IReadOnlyDictionary<string, string> values)
    {
        var label = ResolveLabel(field);
        if (!TryGetStoredValue(values, field, out var rawValue))
        {
            return new FormReadOnlyFieldReadback(label, SpokenValue: null, FormSpokenValueProjectionStatus.Empty);
        }

        // Jeden vorhandenen Wert vor der Sprachprojektion auf Vorlesefreigabe ohne
        // zusätzliche Einwilligung prüfen. Weder Gruppeneinstieg noch gezielter Sprung
        // erteilen hier eine solche Einwilligung.
        var variableId = field.VariableId.Value;
        if (!await _privacyReadbackPolicy.CanSpeakValueInConfirmationAsync(variableId, explicitOptIn: false).ConfigureAwait(false))
        {
            return new FormReadOnlyFieldReadback(label, SpokenValue: null, FormSpokenValueProjectionStatus.Withheld);
        }

        var valueTypeKind = FormVariableValueReader.TryGetValueTypeKind(_variablesById, variableId)
            ?? ValueTypeKinds.String;
        var projection = _spokenValueProjector.Project(
            rawValue,
            valueTypeKind,
            FormSpokenValueSpeakDisposition.SpeakFull,
            _choiceOptionsResolver.ResolveChoiceOptions(variableId),
            maskedWording: null);
        return new FormReadOnlyFieldReadback(label, projection.SpokenValue, projection.Status);
    }

    private FieldDefinition? ResolveField(string fieldId)
        => _runtimeState.Contract.Fields.FirstOrDefault(
            field => string.Equals(field.FieldId.Value, fieldId, StringComparison.Ordinal));

    private string ResolveLabel(FieldDefinition field)
        => ResolveLocalizedText(field.Label, field.FieldId.Value);

    private string ResolveLocalizedText(LocalizedText? text, string? fallback)
    {
        if (text is null)
        {
            return fallback ?? string.Empty;
        }

        var contract = _runtimeState.Contract;
        return LocalizedTextResolver.Resolve(
            text,
            _isoCode,
            contract.DefaultLanguage,
            contract.SupportedLanguages,
            contract.StrictLocalization,
            _runtimeState.PolitenessLevel);
    }

    private bool TryGetStoredValue(
        IReadOnlyDictionary<string, string> values,
        FieldDefinition field,
        out string value)
    {
        value = string.Empty;
        if (!values.TryGetValue(field.VariableId.Value, out var stored))
        {
            return false;
        }

        var valueTypeKind = FormVariableValueReader.TryGetValueTypeKind(_variablesById, field.VariableId.Value);
        var isSet = string.Equals(valueTypeKind, ValueTypeKinds.Set, StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(stored) && !isSet)
        {
            return false;
        }

        value = stored;
        return true;
    }
}
