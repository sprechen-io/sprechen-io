namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Unterscheidet fehlendes Gerät, verweigerte Berechtigung und allgemeine Nichtverfügbarkeit beim Mikrofonzugriff.
/// </summary>
public enum MicrophoneAcquisitionFailureCode
{
    Unavailable = 0,
    DeviceNotFound = 1,
    PermissionDenied = 2
}
