using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bietet Bedingungsprüfung, Fehlerfokus und Regelausführung zur Vorbereitung von Abgabe und Feldgruppeneintritt.
/// </summary>
public interface IFormDialogValidationInput
{
    Task<ImmutableArray<FormConstraintViolation>> ValidateAllAsync();

    Task<FormConstraintViolation?> TryFocusNextErrorAsync();

    Task<ImmutableArray<string>> TryApplySubmitAttemptRulesAsync();

    Task<ImmutableArray<string>> TryApplyEnterFieldGroupRulesAsync();
}
