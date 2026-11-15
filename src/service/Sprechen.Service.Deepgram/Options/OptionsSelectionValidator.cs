using Microsoft.Extensions.Options;
using Sprechen.Service.Deepgram.Services;

namespace Sprechen.Service.Deepgram.Options;

/// <summary>
/// Führt Attribut-, Endpunkt- und TTS-Konfigurationsprüfungen für Deepgram zusammen.
/// </summary>
internal sealed class OptionsSelectionValidator(
    IOptions<SupportedLanguagesOptions> supportedLanguagesOptions) : IValidateOptions<DeepgramOptions>
{
    private static readonly DataAnnotationValidateOptions<DeepgramOptions> DataAnnotationsValidator =
        new(global::Microsoft.Extensions.Options.Options.DefaultName);

    private readonly IOptions<SupportedLanguagesOptions> _supportedLanguagesOptions =
        supportedLanguagesOptions ?? throw new ArgumentNullException(nameof(supportedLanguagesOptions));

    private readonly DeepgramEndpointRootValidator _endpointRoots = new();
    private readonly DeepgramSupportedIsoCodeResolver _supportedIsoCodes = new();
    private readonly DeepgramTtsOptionsValidator _ttsOptions = new();

    public ValidateOptionsResult Validate(string? name, DeepgramOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        var dataAnnotationResult = DataAnnotationsValidator.Validate(name, options);
        if (dataAnnotationResult.Failed)
        {
            failures.AddRange(dataAnnotationResult.Failures);
        }

        _endpointRoots.Validate(options, failures);

        var supportedIsoCodes = _supportedIsoCodes.Resolve(_supportedLanguagesOptions.Value);
        _ttsOptions.Validate(options, supportedIsoCodes, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
