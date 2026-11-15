using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

/// <summary>
/// Liefert für Dialogablauftests unmittelbar die vorgegebene Anwendung als ausgewählt zurück.
/// </summary>
internal sealed class FixedFormsCatalogDialog : IFormsCatalogDialog
{
    private readonly string _applicationId;
    private readonly string _applicationName;
    private readonly int _formCount;
    private readonly string? _defaultFormId;

    public FixedFormsCatalogDialog(string applicationId, string applicationName)
        : this(applicationId, applicationName, formCount: 1, defaultFormId: null)
    {
    }

    public FixedFormsCatalogDialog(string applicationId, string applicationName, int formCount, string? defaultFormId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        if (formCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(formCount), formCount, "Form count must be positive.");
        }

        _applicationId = applicationId;
        _applicationName = applicationName;
        _formCount = formCount;
        _defaultFormId = string.IsNullOrWhiteSpace(defaultFormId) ? null : defaultFormId.Trim();
    }

    public Task<FormsCatalogDialogResult> ExecuteAsync(
        string languageCode,
        string correlationId,
        CancellationToken cancellationToken)
        => Task.FromResult(FormsCatalogDialogResult.Selected(_applicationId, _applicationName, _formCount, _defaultFormId));

    public Task<FormsCatalogDialogResult> ExecuteAsync(
        string languageCode,
        string correlationId,
        SpeechTranscript initialTranscript,
        CancellationToken cancellationToken)
        => Task.FromResult(FormsCatalogDialogResult.Selected(_applicationId, _applicationName, _formCount, _defaultFormId));
}
