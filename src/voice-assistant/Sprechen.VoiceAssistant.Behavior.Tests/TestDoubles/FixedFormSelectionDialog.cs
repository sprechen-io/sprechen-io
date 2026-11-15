
namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

/// <summary>
/// Liefert für Dialogablauftests unmittelbar das vorgegebene Formular als ausgewählt zurück.
/// </summary>
internal sealed class FixedFormSelectionDialog : IFormSelectionDialog
{
    private readonly string _formId;
    private readonly string _formName;

    public FixedFormSelectionDialog(string formId)
        : this(formId, formName: formId)
    {
    }

    public FixedFormSelectionDialog(string formId, string formName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formId);
        ArgumentException.ThrowIfNullOrWhiteSpace(formName);

        _formId = formId.Trim();
        _formName = formName.Trim();
    }

    public Task<FormSelectionDialogResult> ExecuteAsync(
        string languageCode,
        string applicationId,
        string correlationId,
        IReadOnlyCollection<string> completedFormIds,
        CancellationToken cancellationToken)
        => Task.FromResult(FormSelectionDialogResult.Selected(_formId, _formName));
}

