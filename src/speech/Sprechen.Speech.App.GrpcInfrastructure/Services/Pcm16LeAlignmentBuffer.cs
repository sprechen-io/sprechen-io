namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>Aligns incoming gRPC payloads to complete PCM16LE samples.</summary>
internal sealed class Pcm16LeAlignmentBuffer
{
    private readonly MicroSharp.Processing.Pcm16LeAlignmentBuffer _alignment = new();

    public byte[] Align(byte[] bytes) => _alignment.Align(bytes);

    public void Complete() => _ = _alignment.Complete();
}
