using Microsoft.Extensions.Options;

namespace Sprechen.Service.Core.Services;

/// <summary>
/// Validiert die Sprachkonfiguration anhand des Aufbaus der unterstützten Sprachmenge.
/// </summary>
internal sealed class SupportedLanguagesOptionsSelectionValidator : IValidateOptions<SupportedLanguagesOptions>
{
    private readonly SupportedLanguageSetBuilder _languageSets = new();

    private static readonly ValidateOptionsResult FailureResult = ValidateOptionsResult.Fail(
        $"{SupportedLanguagesOptions.SectionName} must contain only supported ISO codes ({SupportedLanguagesOptions.AllowedIsoCodesDisplay}) and a valid default.");

    public ValidateOptionsResult Validate(string? name, SupportedLanguagesOptions options)
    {
        if (options is null)
        {
            return FailureResult;
        }

        try
        {
            _ = _languageSets.Build(options);
            return ValidateOptionsResult.Success;
        }
        catch (InvalidOperationException)
        {
            return FailureResult;
        }
    }
}
