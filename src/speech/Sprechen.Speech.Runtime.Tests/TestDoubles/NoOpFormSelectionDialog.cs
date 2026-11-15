
namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class NoOpFormSelectionDialog : IFormSelectionDialog
{
    public Task<FormSelectionDialogResult> ExecuteAsync(
        string languageCode,
        string applicationId,
        string correlationId,
        IReadOnlyCollection<string> completedFormIds,
        CancellationToken cancellationToken)
        => Task.FromResult(FormSelectionDialogResult.Cancelled(DialogCancellationReason.UserCancelled));
}


