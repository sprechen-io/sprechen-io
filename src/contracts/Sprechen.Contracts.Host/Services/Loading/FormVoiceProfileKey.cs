namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Kennzeichnet Dialogprofile eines Formulars durch Kennung und Version, damit ihre Schlüssel nicht mit anderen Artefaktarten verwechselt werden.
/// </summary>
internal readonly record struct FormVoiceProfileKey(string FormVoiceProfileId, string ProfileVersion);
