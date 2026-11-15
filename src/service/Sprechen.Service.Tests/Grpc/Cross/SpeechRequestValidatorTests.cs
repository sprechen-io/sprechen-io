using Grpc.Core;
using Moq;
using Sprechen.Proto.Transport;
using Sprechen.Service.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc.Cross;

public sealed class SpeechRequestValidatorTests
{
    [Fact]
    public void ValidateRequiredSpeechStart_WhenValidReturnsMimeTypeAndLanguage()
    {
        var validator = CreateValidator();
        var start = new GrpcAudioChunkStreamStart(
            AudioMimeType.Pcm,
            SprechenProto.AudioMimeType.AudioPcm,
            " DE ",
            [new byte[] { 0x1 }]);

        var validated = validator.ValidateRequiredSpeechStart(start);

        Assert.Equal(AudioMimeType.Pcm, validated.MimeType);
        Assert.Equal("de", validated.LanguageCode);
    }

    [Fact]
    public void ValidateTextToSpeechRequest_WhenValidReturnsTrimmedCanonicalRequest()
    {
        var validator = CreateValidator();
        var request = new SprechenProto.TextRequest
        {
            Text = " hallo ",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm,
            PolitenessLevel = SprechenProto.TextToSpeechPolitenessLevel.TtsPolitenessFormal,
            SpeechRate = 1.3f
        };

        var validated = validator.ValidateTextToSpeechRequest(request);

        Assert.Equal("hallo", validated.Text);
        Assert.Equal("de", validated.LanguageCode);
        Assert.Equal(AudioMimeType.Pcm, validated.DesiredMimeType);
        Assert.Equal(TextToSpeechPolitenessLevel.Formal, validated.PolitenessLevel);
        Assert.Equal(1.3f, validated.SpeechRate);
    }

    [Fact]
    public void ValidateAudioStreamStart_WhenAMimeTypeArrivedButNoAudioDid_StillRejects()
    {
        // Auch ein gültiger MIME-Typ genügt nicht, wenn der Strom keine Audiodaten enthält.
        var start = new GrpcAudioChunkStreamStart(
            AudioMimeType.Pcm,
            SprechenProto.AudioMimeType.AudioPcm,
            languageCode: "de",
            Array.Empty<byte[]>());

        var ex = Assert.Throws<RpcException>(() => SpeechRequestValidator.ValidateAudioStreamStart(start));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.AudioInputMimeTypeRequired, errorCode);
    }

    [Fact]
    public void ValidateAudioStreamStart_WhenEmptyStreamThrowsStableMimeTypeCode()
    {
        var start = new GrpcAudioChunkStreamStart(
            AudioMimeType.Unspecified,
            SprechenProto.AudioMimeType.AudioMimeUnspecified,
            languageCode: null,
            Array.Empty<byte[]>());

        var ex = Assert.Throws<RpcException>(() => SpeechRequestValidator.ValidateAudioStreamStart(start));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.AudioInputMimeTypeRequired, errorCode);
    }

    [Fact]
    public void ValidateUnarySpeechToTextStartRequiresPcm_WhenPcm_DoesNotThrow()
    {
        var start = new GrpcAudioChunkStreamStart(
            AudioMimeType.Pcm,
            SprechenProto.AudioMimeType.AudioPcm,
            "de",
            [new byte[] { 0x1 }]);

        SpeechRequestValidator.ValidateUnarySpeechToTextStartRequiresPcm(start);
    }

    [Theory]
    [InlineData(AudioMimeType.Mp3, SprechenProto.AudioMimeType.AudioMp3)]
    [InlineData(AudioMimeType.Wav, SprechenProto.AudioMimeType.AudioWav)]
    [InlineData(AudioMimeType.OggOpus, SprechenProto.AudioMimeType.AudioOggOpus)]
    [InlineData(AudioMimeType.Flac, SprechenProto.AudioMimeType.AudioFlac)]
    [InlineData(AudioMimeType.WebmOpus, SprechenProto.AudioMimeType.AudioWebmOpus)]
    public void ValidateUnarySpeechToTextStartRequiresPcm_WhenNonPcm_ThrowsStableUnsupportedCode(AudioMimeType mime, SprechenProto.AudioMimeType protoMime)
    {
        var start = new GrpcAudioChunkStreamStart(
            mime,
            protoMime,
            "de",
            [new byte[] { 0x1 }]);

        var ex = Assert.Throws<RpcException>(() => SpeechRequestValidator.ValidateUnarySpeechToTextStartRequiresPcm(start));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.AudioInputMimeTypeUnsupported, errorCode);
    }

    [Fact]
    public void ValidateUnarySpeechToTextStartRequiresPcm_WhenMimeUnspecified_ThrowsRequiredCode()
    {
        var start = new GrpcAudioChunkStreamStart(
            AudioMimeType.Unspecified,
            SprechenProto.AudioMimeType.AudioMimeUnspecified,
            languageCode: null,
            Array.Empty<byte[]>());

        var ex = Assert.Throws<RpcException>(() => SpeechRequestValidator.ValidateUnarySpeechToTextStartRequiresPcm(start));

        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.AudioInputMimeTypeRequired, errorCode);
    }

    private static SpeechRequestValidator CreateValidator()
    {
        var supportedLanguages = new Mock<ISupportedLanguagesService>(MockBehavior.Strict);
        supportedLanguages
            .SetupGet(service => service.Languages)
            .Returns(new[] { new SupportedLanguage("de", "Deutsch") });
        supportedLanguages
            .Setup(service => service.IsSupportedIsoCode(It.IsAny<string>()))
            .Returns((string code) => string.Equals(code, "de", StringComparison.OrdinalIgnoreCase));

        return new SpeechRequestValidator(new LanguageResolver(supportedLanguages.Object));
    }
}
