namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Transportiert einen klassifizierten Fehler beim Mikrofonzugriff.
/// </summary>
public sealed class MicrophoneAcquisitionException : InvalidOperationException
{
    public MicrophoneAcquisitionException(MicrophoneAcquisitionFailureCode code)
        : base(CreateMessage(code))
    {
        Code = code;
    }

    public MicrophoneAcquisitionException(MicrophoneAcquisitionFailureCode code, Exception innerException)
        : base(CreateMessage(code), innerException)
    {
        Code = code;
    }

    public MicrophoneAcquisitionFailureCode Code { get; }

    private static string CreateMessage(MicrophoneAcquisitionFailureCode code)
    {
        return code switch
        {
            MicrophoneAcquisitionFailureCode.DeviceNotFound => "The microphone device is unavailable.",
            MicrophoneAcquisitionFailureCode.PermissionDenied => "Microphone access is not permitted.",
            _ => "Microphone acquisition failed."
        };
    }
}
