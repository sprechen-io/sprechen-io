namespace Sprechen.Speech.TestDoubles;

public static class ModeAwareSpeechCapture
{
    public static IModeAwareSpeechCaptureClient Required(ISpeechCaptureClient captureClient)
        => captureClient as IModeAwareSpeechCaptureClient
            ?? new ForwardingModeAwareSpeechCaptureClient(captureClient);
}
