using Microsoft.Extensions.Options;

namespace Sprechen.Service.Core.Services;

/// <summary>
/// Prüft, dass für Spracherkennung und Sprachsynthese ausschließlich Deepgram konfiguriert ist.
/// </summary>
internal sealed class SpeechProviderOptionsSelectionValidator : IValidateOptions<SpeechProviderOptions>
{
    public ValidateOptionsResult Validate(string? name, SpeechProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        if (options.SttProvider != SpeechProvider.Deepgram)
        {
            failures.Add($"{SpeechProviderOptions.SectionName}:SttProvider must be Deepgram.");
        }

        if (options.TtsProvider != SpeechProvider.Deepgram)
        {
            failures.Add($"{SpeechProviderOptions.SectionName}:TtsProvider must be Deepgram.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
