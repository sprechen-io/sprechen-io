using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Verknüpft Befehlsanfrage, Formularauswahl und Laufzeitdienste zur globalen Befehlsbearbeitung.
/// </summary>
public sealed record FormVoiceCommandGlobalCommandContext(
    FormVoiceCommandGlobalCommandRequest Request,
    FormVoiceCommandSelectionContext Selection,
    FormVoiceCommandGlobalCommandRuntime Runtime)
{
    public FormStatusSnapshot? StatusSnapshot { get; init; }

    public string CommandId => Request.CommandId;

    public string IsoCode => Request.IsoCode;

    public string? Argument => Request.Argument;

    public VoiceDialogSettings VoiceSettings => Selection.VoiceSettings;

    public ApplicationDescriptor Application => Selection.Application;

    public ImmutableHashSet<string> CompletedFormIds => Selection.CompletedFormIds;

    public FormDescriptor CurrentForm => Selection.Form;

    public FormContract Contract => Selection.Contract;

    public IFormDialogRuntime Interpreter => Runtime.Interpreter;

    public EvaluationSession Session => Runtime.Session;

    public IFormVoiceCommandDialogStateTracker StateTracker => Runtime.StateTracker;

    public FormVoiceCommandCaptureSettings CaptureSettings
        => FormVoiceCommandCaptureSettings.From(VoiceSettings, Contract);
}
