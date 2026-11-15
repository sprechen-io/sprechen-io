using System.Diagnostics.Metrics;
using Sprechen.Service.Grpc;
using Sprechen.Service.Tests.Grpc;
using Sprechen.Service.Tests.TestDoubles;
using Xunit;

namespace Sprechen.Service.Tests.Grpc.Cross;

[Collection(SpeechRecognitionTelemetryCollection.Name)]
public sealed class SpeechRecognitionDiagnosticsEmitterTests
{
    [Fact]
    public void RecordEmptyTranscript_IncrementsEmptyTranscriptMetric()
    {
        var emitter = new SpeechRecognitionDiagnosticsEmitter(
            Microsoft.Extensions.Options.Options.Create(new SpeechProviderOptions()));
        var measurements = new List<(long Value, Dictionary<string, object?> Tags)>();
        using var meterListener = StartEmptyTranscriptMeterListener(measurements);

        emitter.RecordEmptyTranscript("SpeechToText", "de", AudioMimeType.Pcm);

        Assert.Contains(
            measurements,
            measurement => measurement.Value == 1
                && Equals(measurement.Tags["operation"], "SpeechToText")
                && Equals(measurement.Tags["language_code"], "de")
                && Equals(measurement.Tags["mime_type"], "Pcm"));
    }

    private static MeterListener StartEmptyTranscriptMeterListener(List<(long Value, Dictionary<string, object?> Tags)> measurements)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (string.Equals(instrument.Meter.Name, "Sprechen.Service.SpeechRecognition", StringComparison.Ordinal)
                && string.Equals(instrument.Name, "stt.empty_transcripts_total", StringComparison.Ordinal))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            var copiedTags = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var tag in tags)
            {
                copiedTags[tag.Key] = tag.Value;
            }

            measurements.Add((value, copiedTags));
        });
        listener.Start();
        return listener;
    }
}
