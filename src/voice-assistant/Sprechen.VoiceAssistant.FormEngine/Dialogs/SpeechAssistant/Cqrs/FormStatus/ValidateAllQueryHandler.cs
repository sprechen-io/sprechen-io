using System.Collections.Immutable;
using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Leitet die Gesamtvalidierung an den <see cref="FormDialogValidator"/> weiter und
/// übergibt dabei die Prüfung der effektiven Feldsichtbarkeit.
/// </summary>
internal sealed class ValidateAllQueryHandler : IQueryHandler<ValidateAllQuery, ImmutableArray<FormConstraintViolation>>
{
    private readonly FormDialogValidator _validator;
    private readonly FormFieldVisibilityPolicy _visibilityPolicy;

    public ValidateAllQueryHandler(
        FormDialogValidator validator,
        FormFieldVisibilityPolicy visibilityPolicy)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _visibilityPolicy = visibilityPolicy ?? throw new ArgumentNullException(nameof(visibilityPolicy));
    }

    public Task<ImmutableArray<FormConstraintViolation>> HandleAsync(ValidateAllQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(_validator.ValidateAll(_visibilityPolicy.IsFieldEffectivelyVisible));
    }
}
