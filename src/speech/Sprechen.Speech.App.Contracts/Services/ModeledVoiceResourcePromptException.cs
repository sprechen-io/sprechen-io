namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Kennzeichnet fehlgeschlagene Auflösung modellierter Sprachansagen mit einem auswertbaren Fehlercode.
/// </summary>
public sealed class ModeledVoiceResourcePromptException : InvalidOperationException
{
    public const string UnknownPromptIdCodeValue = "E_VOICE_RESOURCE_PROMPT_ID_UNKNOWN";
    public const string MissingLocalizedTextCodeValue = "E_VOICE_RESOURCE_PROMPT_TEXT_MISSING";

    public ModeledVoiceResourcePromptException(string code, string message)
        : base(string.IsNullOrWhiteSpace(message) ? "Modeled voice resource prompt resolution failed." : message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Code = code;
    }

    public string Code { get; }
}
