using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class NoOpFormsCatalogDialog : IFormsCatalogDialog
{
    public string? LastInitialTranscript { get; private set; }

    public Task<FormsCatalogDialogResult> ExecuteAsync(
        string languageCode,
        string correlationId,
        CancellationToken cancellationToken)
        => Task.FromResult(FormsCatalogDialogResult.Cancelled(DialogCancellationReason.UserCancelled));

    public Task<FormsCatalogDialogResult> ExecuteAsync(
        string languageCode,
        string correlationId,
        SpeechTranscript initialTranscript,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initialTranscript);
        LastInitialTranscript = initialTranscript.Text;
        return Task.FromResult(FormsCatalogDialogResult.Cancelled(DialogCancellationReason.UserCancelled));
    }
}
