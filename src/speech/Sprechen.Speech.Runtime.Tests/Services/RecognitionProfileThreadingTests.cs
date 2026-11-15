using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;
using AppRecognitionProfile = Sprechen.Speech.App.Contracts.Services.RecognitionProfile;
using ProtoKeytermComposition = Sprechen.Proto.KeytermComposition;
using ProtoRecognitionProfile = Sprechen.Proto.RecognitionProfile;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class RecognitionProfileThreadingTests
{
    [Theory]
    [InlineData(AppRecognitionProfile.Conversational)]
    [InlineData(AppRecognitionProfile.Spelling)]
    [InlineData(AppRecognitionProfile.Digits)]
    public async Task RecognizeAsync_ThreadsCaptureSiteProfileToRecognitionClientUnchanged(AppRecognitionProfile profile)
    {
        var client = new CapturingRecognitionClient();
        await using var worker = new SpeechRecognitionWorker(client);
        var runner = new FormVoiceCommandSpeechRecognitionRunner(worker, NullLogger.Instance);
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);

        var result = await runner.RecognizeAsync(utterance, "de", profile, CancellationToken.None);

        Assert.Equal("ok", result.Transcript);
        Assert.Equal(profile, client.LastProfile);
    }

    [Fact]
    public async Task RecognizeAsync_WhenTheFarSideCancels_ReturnsRecoverableFailureRatherThanEscaping()
    {
        // Ein Abbruch der Gegenseite bei weiter gültigem Aufrufertoken soll als behebbarer Fehler zurückkommen.
        var client = new FarSideCancellingRecognitionClient();
        await using var worker = new SpeechRecognitionWorker(client);
        var runner = new FormVoiceCommandSpeechRecognitionRunner(worker, NullLogger.Instance);
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);

        var result = await runner.RecognizeAsync(utterance, "de", AppRecognitionProfile.Conversational, CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.Equal(SpeechCaptureFailureCodes.CaptureInterrupted, result.FailureCode);
        Assert.True(result.Recoverable);
    }

    [Fact]
    public async Task RecognizeAsync_ThreadsKeytermsToRecognitionClient()
    {
        var client = new CapturingRecognitionClient();
        await using var worker = new SpeechRecognitionWorker(client);
        var runner = new FormVoiceCommandSpeechRecognitionRunner(worker, NullLogger.Instance);
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        var keyterms = new[] { "Blau", "Rot", "Grün" };

        var result = await runner.RecognizeAsync(
            utterance,
            "de",
            AppRecognitionProfile.Conversational,
            new SpeechKeyterms(keyterms, KeytermComposition.WithServerBase),
            CancellationToken.None);

        Assert.Equal("ok", result.Transcript);
        Assert.Equal(keyterms, client.LastKeyterms!.Terms);
    }

    [Fact]
    public async Task RecognizeAsync_WithoutKeyterms_LeavesClientKeytermsNull()
    {
        var client = new CapturingRecognitionClient();
        await using var worker = new SpeechRecognitionWorker(client);
        var runner = new FormVoiceCommandSpeechRecognitionRunner(worker, NullLogger.Instance);
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);

        await runner.RecognizeAsync(utterance, "de", AppRecognitionProfile.Conversational, CancellationToken.None);

        Assert.Null(client.LastKeyterms);
    }

    [Fact]
    public void MessageFactory_WhenConversational_LeavesWireProfileUnsetForByteIdenticalRequest()
    {
        var factory = new GrpcAudioChunkMessageFactory(SpeechAudioMimeType.Pcm, "de", AppRecognitionProfile.Conversational);

        var message = factory.Create(new byte[] { 0x1 });

        Assert.Equal(ProtoRecognitionProfile.Unspecified, message.RecognitionProfile);
    }

    [Fact]
    public void MessageFactory_WithoutServerBaseAndNoTerms_StillWritesTheComposition()
    {
        var factory = new GrpcAudioChunkMessageFactory(
            SpeechAudioMimeType.Pcm,
            "de",
            AppRecognitionProfile.Conversational,
            new SpeechKeyterms([], KeytermComposition.WithoutServerBase));

        var message = factory.Create(new byte[] { 0x1 });

        // Das separate Feld unterscheidet eine bewusst leere Liste ohne Serverbegriffe von fehlenden Keyterms.
        Assert.Empty(message.Keyterms);
        Assert.Equal(ProtoKeytermComposition.WithoutServerBase, message.KeytermComposition);
    }

    [Fact]
    public void MessageFactory_WithServerBaseAndNoTerms_StaysByteIdenticalToSayingNothing()
    {
        var factory = new GrpcAudioChunkMessageFactory(
            SpeechAudioMimeType.Pcm,
            "de",
            AppRecognitionProfile.Conversational,
            new SpeechKeyterms([], KeytermComposition.WithServerBase));

        var message = factory.Create(new byte[] { 0x1 });

        Assert.Empty(message.Keyterms);
        Assert.Equal(ProtoKeytermComposition.Unspecified, message.KeytermComposition);
    }

    [Theory]
    [InlineData(AppRecognitionProfile.Spelling, ProtoRecognitionProfile.Spelling)]
    [InlineData(AppRecognitionProfile.Digits, ProtoRecognitionProfile.Digits)]
    public void MessageFactory_WhenTuningProfile_WritesItOnceOnTheFirstChunk(
        AppRecognitionProfile appProfile,
        ProtoRecognitionProfile expectedProto)
    {
        var factory = new GrpcAudioChunkMessageFactory(SpeechAudioMimeType.Pcm, "de", appProfile);

        var first = factory.Create(new byte[] { 0x1 });
        var second = factory.Create(new byte[] { 0x2 });

        Assert.Equal(expectedProto, first.RecognitionProfile);
        Assert.Equal(ProtoRecognitionProfile.Unspecified, second.RecognitionProfile);
    }
}
