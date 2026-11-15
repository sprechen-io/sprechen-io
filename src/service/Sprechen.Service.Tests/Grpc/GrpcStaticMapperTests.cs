using Grpc.Core;
using Sprechen.Proto.Transport;
using Sprechen.Service.Abstractions;
using Sprechen.Service.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc;

public sealed class GrpcStaticMapperTests
{
    [Fact]
    public void GrpcAudioMimeTypeMapper_MapsKnownValuesAndUsesUnspecifiedFallback()
    {
        Assert.Equal(AudioMimeType.Pcm, GrpcAudioMimeTypeMapper.ToCore(SprechenProto.AudioMimeType.AudioPcm));
        Assert.Equal(SprechenProto.AudioMimeType.AudioPcm, GrpcAudioMimeTypeMapper.ToProto(AudioMimeType.Pcm));
        Assert.Equal(AudioMimeType.Unspecified, GrpcAudioMimeTypeMapper.ToCore((SprechenProto.AudioMimeType)999));
        Assert.Equal(SprechenProto.AudioMimeType.AudioMimeUnspecified, GrpcAudioMimeTypeMapper.ToProto((AudioMimeType)999));
    }

    [Fact]
    public void GrpcRecognitionProfileMapper_RoundTripsAndExhaustivelyMapsProto()
    {
        foreach (var core in Enum.GetValues<RecognitionProfile>())
        {
            Assert.Equal(core, GrpcRecognitionProfileMapper.ToCore(GrpcRecognitionProfileMapper.ToProto(core)));
        }

        foreach (var proto in Enum.GetValues<SprechenProto.RecognitionProfile>())
        {
            Assert.Contains(GrpcRecognitionProfileMapper.ToCore(proto), Enum.GetValues<RecognitionProfile>());
        }

        // Unspecified und unbekannte Werte werden dem Gesprächsprofil zugeordnet.
        Assert.Equal(RecognitionProfile.Conversational, GrpcRecognitionProfileMapper.ToCore(SprechenProto.RecognitionProfile.Unspecified));
        Assert.Equal(RecognitionProfile.Conversational, GrpcRecognitionProfileMapper.ToCore((SprechenProto.RecognitionProfile)999));

        foreach (var core in Enum.GetValues<RecognitionProfile>())
        {
            Assert.NotEqual(SprechenProto.RecognitionProfile.Unspecified, GrpcRecognitionProfileMapper.ToProto(core));
        }

        // Protobuf hat genau einen zusätzlichen Wert: Unspecified.
        Assert.Equal(Enum.GetValues<SprechenProto.RecognitionProfile>().Length - 1, Enum.GetValues<RecognitionProfile>().Length);
    }

    [Fact]
    public void GrpcSpeechTranscriptMapper_MapsTextAndClampsConfidence()
    {
        var mapped = GrpcSpeechTranscriptMapper.Map(new SpeechTranscript("Hallo", 150));

        Assert.Equal("Hallo", mapped.Text);
        Assert.Equal(100, mapped.Confidence);
    }

    [Fact]
    public void SpeechConversationEventGrpcMapper_MapsEventsAndTranscriptProjection()
    {
        var languageLocked = SpeechConversationEventGrpcMapper.Map(new SpeechConversationLanguageLockedEvent("de", 7));

        Assert.Equal("de", languageLocked.LanguageLocked.LanguageCode);
        Assert.Equal(7, languageLocked.LanguageLocked.TurnIndex);

        var hasProjection = SpeechConversationEventGrpcMapper.TryMapTranscriptProjection(
            new SpeechConversationTranscriptFinalizedEvent("fertig", 91, 3),
            out var transcript);

        Assert.True(hasProjection);
        Assert.NotNull(transcript);
        Assert.Equal("fertig", transcript.Text);
        Assert.Equal(91, transcript.Confidence);
    }

    [Fact]
    public void SprechenRpcExceptionFactory_EmitsStableErrorCodeTrailersAndRejectsBlankMessage()
    {
        var exception = SprechenRpcExceptionFactory.InvalidArgument(
            SprechenProto.SprechenErrorCode.TextRequired,
            "Text is required.");

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(exception, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.TextRequired, errorCode);
        Assert.Throws<ArgumentException>(() =>
            SprechenRpcExceptionFactory.InvalidArgument(SprechenProto.SprechenErrorCode.TextRequired, " "));
    }
}
