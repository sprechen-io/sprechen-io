using Sprechen.Proto;
using Sprechen.Speech.App.GrpcInfrastructure.Services;
using AppDetectedLanguageCandidate = Sprechen.Speech.LanguageContracts.DetectedLanguageCandidate;
using AppRecognitionProfile = Sprechen.Speech.App.Contracts.Services.RecognitionProfile;
using AppSpeechTranscript = Sprechen.Speech.App.Contracts.Services.SpeechTranscript;
using ProtoAudioMimeType = Sprechen.Proto.AudioMimeType;
using ProtoRecognitionProfile = Sprechen.Proto.RecognitionProfile;
using ProtoSpeechTranscript = Sprechen.Proto.SpeechTranscript;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class GrpcMapperTests
{
    public static IEnumerable<object[]> MimeRoundTrips()
    {
        yield return [ProtoAudioMimeType.AudioMp3, SpeechAudioMimeType.Mp3];
        yield return [ProtoAudioMimeType.AudioWav, SpeechAudioMimeType.Wav];
        yield return [ProtoAudioMimeType.AudioOggOpus, SpeechAudioMimeType.OggOpus];
        yield return [ProtoAudioMimeType.AudioFlac, SpeechAudioMimeType.Flac];
        yield return [ProtoAudioMimeType.AudioWebmOpus, SpeechAudioMimeType.WebmOpus];
        yield return [ProtoAudioMimeType.AudioPcm, SpeechAudioMimeType.Pcm];
        yield return [ProtoAudioMimeType.AudioMimeUnspecified, SpeechAudioMimeType.Unspecified];
    }

    public static IEnumerable<object[]> ErrorCodeMappings()
    {
        yield return [SprechenErrorCode.AuthMissingApiKey, SpeechErrorCode.AuthMissingApiKey];
        yield return [SprechenErrorCode.AuthInvalidApiKey, SpeechErrorCode.AuthInvalidApiKey];
        yield return [SprechenErrorCode.TextRequired, SpeechErrorCode.TextRequired];
        yield return [SprechenErrorCode.LanguageCodeRequired, SpeechErrorCode.LanguageCodeRequired];
        yield return [SprechenErrorCode.LanguageCodeInvalid, SpeechErrorCode.LanguageCodeInvalid];
        yield return [SprechenErrorCode.LanguageCodeUnsupported, SpeechErrorCode.LanguageCodeUnsupported];
        yield return [SprechenErrorCode.AudioInputMimeTypeRequired, SpeechErrorCode.AudioInputMimeTypeRequired];
        yield return [SprechenErrorCode.AudioInputMimeTypeUnsupported, SpeechErrorCode.AudioInputMimeTypeUnsupported];
        yield return [SprechenErrorCode.AudioOutputMimeTypeRequired, SpeechErrorCode.AudioOutputMimeTypeRequired];
        yield return [SprechenErrorCode.AudioOutputMimeTypeUnsupported, SpeechErrorCode.AudioOutputMimeTypeUnsupported];
        yield return [SprechenErrorCode.ProviderFailure, SpeechErrorCode.ProviderFailure];
        yield return [SprechenErrorCode.ProviderRateLimited, SpeechErrorCode.ProviderRateLimited];
        yield return [SprechenErrorCode.VoiceConfigurationInvalidRequest, SpeechErrorCode.VoiceConfigurationInvalidRequest];
        yield return [SprechenErrorCode.VoiceConfigurationUnsupportedVersion, SpeechErrorCode.VoiceConfigurationUnsupportedVersion];
        yield return [SprechenErrorCode.VoiceConfigurationInvalidConfiguration, SpeechErrorCode.VoiceConfigurationInvalidConfiguration];
        yield return [SprechenErrorCode.SpeechKeytermsInvalid, SpeechErrorCode.SpeechKeytermsInvalid];
        yield return [SprechenErrorCode.Unspecified, SpeechErrorCode.Unspecified];
    }

    [Theory]
    [MemberData(nameof(MimeRoundTrips))]
    public void SpeechAudioMimeTypeGrpcMapper_MapsBothDirections(
        ProtoAudioMimeType proto,
        SpeechAudioMimeType app)
    {
        Assert.Equal(app, SpeechAudioMimeTypeGrpcMapper.FromProto(proto));
        Assert.Equal(proto, SpeechAudioMimeTypeGrpcMapper.ToProto(app));
    }

    [Theory]
    [MemberData(nameof(ErrorCodeMappings))]
    public void SpeechErrorCodeGrpcMapper_MapsStableErrorCodes(
        SprechenErrorCode proto,
        SpeechErrorCode app)
    {
        Assert.Equal(app, SpeechErrorCodeGrpcMapper.FromProto(proto));
    }

    [Fact]
    public void SpeechErrorCodeGrpcMapper_CoversEveryKnownProtoCode()
    {
        var missing = Enum.GetValues<SprechenErrorCode>()
            .Except(SpeechErrorCodeGrpcMapper.MappedProtoCodes)
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void SpeechErrorCodeGrpcMapper_WhenProtoCodeUnknown_ReturnsUnspecified()
    {
        var result = SpeechErrorCodeGrpcMapper.FromProto((SprechenErrorCode)int.MaxValue);

        Assert.Equal(SpeechErrorCode.Unspecified, result);
    }

    [Fact]
    public void SpeechTranscriptGrpcMapper_TrimsTextAndClampsConfidence()
    {
        var transcript = SpeechTranscriptGrpcMapper.FromProto(new ProtoSpeechTranscript
        {
            Text = "  hallo  ",
            Confidence = 120
        });

        Assert.Equal(new AppSpeechTranscript("hallo", 100), transcript);
    }

    [Fact]
    public void SpeechTranscriptGrpcMapper_WhenConfidenceMissing_ReturnsNullConfidence()
    {
        var transcript = SpeechTranscriptGrpcMapper.FromProto(new ProtoSpeechTranscript
        {
            Text = "  text  "
        });

        Assert.Equal("text", transcript.Text);
        Assert.Null(transcript.ConfidencePercent);
    }

    [Fact]
    public void SupportedLanguagesGrpcMapper_TrimsRootAndLanguages()
    {
        var response = new SupportedLanguagesResponse
        {
            DefaultIsoCode = " de "
        };
        response.Languages.Add(new Sprechen.Proto.SupportedLanguage
        {
            IsoCode = " en ",
            DisplayName = " English "
        });

        var result = SupportedLanguagesGrpcMapper.FromProto(response);

        Assert.Equal("de", result.DefaultIsoCode);
        var language = Assert.Single(result.Languages);
        Assert.Equal("en", language.IsoCode);
        Assert.Equal("English", language.DisplayName);
    }

    [Fact]
    public void SpeechConversationEventGrpcMapper_MapsEveryPayloadCase()
    {
        Assert.IsType<SpeechConversationTranscriptUpdateEvent>(SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage
        {
            TranscriptUpdate = new SpeechConversationTranscriptUpdate
            {
                Text = "partial",
                Confidence = 80,
                TurnIndex = 1
            }
        }));
        Assert.IsType<SpeechConversationTranscriptFinalizedEvent>(SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage
        {
            TranscriptFinalized = new SpeechConversationTranscriptFinalized
            {
                Text = "final",
                Confidence = 90,
                TurnIndex = 2
            }
        }));
        Assert.IsType<SpeechConversationTurnStartedEvent>(SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage
        {
            TurnStarted = new SpeechConversationTurnStarted { TurnIndex = 3 }
        }));
        Assert.IsType<SpeechConversationEndOfTurnEvent>(SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage
        {
            EndOfTurn = new SpeechConversationEndOfTurn { TurnIndex = 4, Transcript = "done" }
        }));
        Assert.IsType<SpeechConversationEagerEndOfTurnEvent>(SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage
        {
            EagerEndOfTurn = new SpeechConversationEagerEndOfTurn { TurnIndex = 5, Transcript = "eager" }
        }));
        Assert.IsType<SpeechConversationTurnResumedEvent>(SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage
        {
            TurnResumed = new SpeechConversationTurnResumed { TurnIndex = 6 }
        }));

        var languageDetected = Assert.IsType<SpeechConversationLanguageDetectedEvent>(SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage
        {
            LanguageDetected = new SpeechConversationLanguageDetected
            {
                TurnIndex = 7,
                LanguageCodes = { "de", "en" },
                HintedLanguageCodes = { "de", "en", "es" }
            }
        }));
        Assert.Equal(["de", "en"], languageDetected.Languages);
        Assert.Equal(["de", "en", "es"], languageDetected.HintedLanguages);

        var languageDetectedWithoutHints = Assert.IsType<SpeechConversationLanguageDetectedEvent>(SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage
        {
            LanguageDetected = new SpeechConversationLanguageDetected { TurnIndex = 9, LanguageCodes = { "de" } }
        }));
        Assert.Equal(["de"], languageDetectedWithoutHints.Languages);
        Assert.Empty(languageDetectedWithoutHints.HintedLanguages);

        var languageLocked = Assert.IsType<SpeechConversationLanguageLockedEvent>(SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage
        {
            LanguageLocked = new SpeechConversationLanguageLocked { TurnIndex = 8, LanguageCode = "de" }
        }));
        Assert.Equal("de", languageLocked.LanguageCode);

        Assert.IsType<SpeechConversationCompletedEvent>(SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage
        {
            ConversationCompleted = new SpeechConversationCompleted()
        }));
        Assert.IsType<SpeechConversationCancelledEvent>(SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage
        {
            ConversationCancelled = new SpeechConversationCancelled()
        }));

        var failed = Assert.IsType<SpeechConversationFailedEvent>(SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage
        {
            ConversationFailed = new SpeechConversationFailed
            {
                ErrorCode = "provider.failure",
                Detail = "boom"
            }
        }));
        Assert.Equal("provider.failure", failed.ErrorCode);
        Assert.Equal("boom", failed.Detail);
    }

    [Fact]
    public void SpeechConversationEventGrpcMapper_CoversEveryKnownPayloadCase()
    {
        var missing = Enum.GetValues<SpeechConversationEventMessage.PayloadOneofCase>()
            .Where(static payloadCase => payloadCase != SpeechConversationEventMessage.PayloadOneofCase.None)
            .Except(SpeechConversationEventGrpcMapper.MappedPayloadCases)
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void SpeechConversationEventGrpcMapper_WhenPayloadMissing_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SpeechConversationEventGrpcMapper.FromProto(new SpeechConversationEventMessage()));
    }

    [Fact]
    public void RecognitionProfile_AppMirror_RoundTripsAndExhaustivelyMapsProto()
    {
        foreach (var app in Enum.GetValues<AppRecognitionProfile>())
        {
            Assert.Equal(app, RecognitionProfileGrpcMapper.FromProto(RecognitionProfileGrpcMapper.ToProto(app)));
        }

        foreach (var proto in Enum.GetValues<ProtoRecognitionProfile>())
        {
            Assert.Contains(RecognitionProfileGrpcMapper.FromProto(proto), Enum.GetValues<AppRecognitionProfile>());
        }

        Assert.Equal(AppRecognitionProfile.Conversational, RecognitionProfileGrpcMapper.FromProto(ProtoRecognitionProfile.Unspecified));
        Assert.Equal(AppRecognitionProfile.Conversational, RecognitionProfileGrpcMapper.FromProto((ProtoRecognitionProfile)999));

        foreach (var app in Enum.GetValues<AppRecognitionProfile>())
        {
            Assert.NotEqual(ProtoRecognitionProfile.Unspecified, RecognitionProfileGrpcMapper.ToProto(app));
        }

        // Ein nur auf einer Seite ergänzter Enumwert soll auffallen; Unspecified gibt es nur im Protobuf-Vertrag.
        Assert.Equal(Enum.GetValues<ProtoRecognitionProfile>().Length - 1, Enum.GetValues<AppRecognitionProfile>().Length);
    }
}
