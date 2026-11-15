namespace Sprechen.Speech.App.Contracts.Messages;

/// <summary>
/// Übermittelt Änderungen des Aktivierungszustands an die Beobachter des Sprachassistenten.
/// </summary>
public sealed record SpeechAssistantActivationStateChanged(SpeechAssistantActivationState State);
