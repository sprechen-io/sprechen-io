using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält ausgewählten Formularvertrag, Spracheinstellungen und Werte einer
/// Befehlssitzung für deren weitere Verarbeitung zusammen.
/// </summary>
public sealed record FormVoiceCommandContext(
    FormVoiceCommandSessionIdentity SessionIdentity,
    FormVoiceCommandSelectionContext Selection,
    FormVoiceCommandValueSnapshot Values) : IFormDialogRuntimeContext
{
    public FormSurfaceDefinition? FormProjection { get; init; }

    public string IsoCode => SessionIdentity.IsoCode;

    public string ApplicationId => SessionIdentity.ApplicationId;

    public string? FormId => SessionIdentity.FormId;

    public ImmutableArray<string> WakeWordActivationForms => SessionIdentity.WakeWordActivationForms;

    public ImmutableArray<string> WakeWordRecognitionOnlyActivationForms => SessionIdentity.WakeWordRecognitionOnlyActivationForms;

    /// <summary>
    /// Verkettet Aktivierungsformen und reine Erkennungsvarianten des
    /// Aktivierungsworts für die Sitzung. Der Kontextlader bezieht beide
    /// Folgen aus <c>IWakeWordProvider</c>. Hier findet keine zusätzliche
    /// Deduplizierung statt.
    /// </summary>
    public IReadOnlyList<string> AcceptedCommandAnchorForms =>
        [.. WakeWordActivationForms, .. WakeWordRecognitionOnlyActivationForms];

    public ApplicationDescriptor Application => Selection.Application;

    public FormDescriptor Form => Selection.Form;

    public FormContract Contract => Selection.Contract;

    public VoiceDialogSettings VoiceSettings => Selection.VoiceSettings;

    public ImmutableHashSet<string> CompletedFormIds => Selection.CompletedFormIds;

    public ImmutableDictionary<string, string> ValuesByVariableId => Values.ValuesByVariableId;

    public ImmutableArray<(string VariableId, string? SourceFormId)> SharedAdoptions => Values.SharedAdoptions;

    public FormVoiceCommandCaptureSettings CaptureSettings
        => FormVoiceCommandCaptureSettings.From(VoiceSettings, Contract);
}
