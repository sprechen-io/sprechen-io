using Xunit;

namespace Sprechen.Service.Tests.Grpc;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SpeechRecognitionTelemetryCollection
{
    public const string Name = "SpeechRecognitionTelemetry";
}
