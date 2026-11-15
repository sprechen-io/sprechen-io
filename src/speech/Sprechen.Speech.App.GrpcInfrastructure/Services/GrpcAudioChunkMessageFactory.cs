using Google.Protobuf;
using Sprechen.Proto;
using AppKeytermComposition = Sprechen.Speech.App.Contracts.Services.KeytermComposition;
using AppRecognitionProfile = Sprechen.Speech.App.Contracts.Services.RecognitionProfile;
using AppSpeechKeyterms = Sprechen.Speech.App.Contracts.Services.SpeechKeyterms;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Erzeugt gRPC-Audioblöcke und fügt Sprachcode, Erkennungsprofil, Schlüsselbegriffe
/// und Diagnosekennzeichen bei Bedarf einmalig dem ersten gesendeten Block hinzu.
/// </summary>
internal sealed class GrpcAudioChunkMessageFactory
{
    private readonly AudioMimeType _mimeType;
    private readonly string? _languageCode;
    private readonly AppRecognitionProfile _recognitionProfile;
    private readonly AppSpeechKeyterms _keyterms;
    private readonly bool _isDiagnostic;
    private bool _languageCodeSent;
    private bool _recognitionProfileSent;
    private bool _keytermsSent;
    private bool _diagnosticSessionSent;

    public GrpcAudioChunkMessageFactory(
        SpeechAudioMimeType mimeType,
        string? languageCode,
        AppRecognitionProfile recognitionProfile = AppRecognitionProfile.Conversational,
        AppSpeechKeyterms? keyterms = null,
        bool isDiagnostic = false)
    {
        _mimeType = SpeechAudioMimeTypeGrpcMapper.ToProto(mimeType);
        _languageCode = string.IsNullOrWhiteSpace(languageCode) ? null : languageCode.Trim();
        _recognitionProfile = recognitionProfile;
        _keyterms = keyterms ?? new AppSpeechKeyterms([], AppKeytermComposition.WithServerBase);
        _isDiagnostic = isDiagnostic;
    }

    public AudioChunk Create(byte[] chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        var message = new AudioChunk
        {
            Data = ByteString.CopyFrom(chunk),
            MimeType = _mimeType
        };

        if (_languageCode is not null && !_languageCodeSent)
        {
            message.LanguageCode = _languageCode;
            _languageCodeSent = true;
        }

        // Das Diagnosekennzeichen der Sitzung wird wie der Sprachcode einmal im ersten Block
        // übertragen. Ohne Diagnosebetrieb bleibt der Standardwert false ungesetzt.
        if (_isDiagnostic && !_diagnosticSessionSent)
        {
            message.DiagnosticSession = true;
            _diagnosticSessionSent = true;
        }

        // Das Gesprächsprofil ist der Standard der Übertragung und bleibt ungesetzt.
        // Abweichende Erkennungsprofile werden wie der Sprachcode einmal im ersten Block übertragen.
        if (_recognitionProfile != AppRecognitionProfile.Conversational && !_recognitionProfileSent)
        {
            message.RecognitionProfile = RecognitionProfileGrpcMapper.ToProto(_recognitionProfile);
            _recognitionProfileSent = true;
        }

        // Schlüsselbegriffe und die Regel zur Einbeziehung des Serverwortschatzes werden
        // wie der Sprachcode einmal im ersten Block übertragen. Eine abweichende Regel
        // wird auch bei leerer Begriffsliste geschrieben: Ohne Serverwortschatz bedeutet
        // diese Kombination keine Begriffsverstärkung. Die leere Liste allein könnte
        // den Ausschluss des Serverwortschatzes nicht ausdrücken.
        if (!_keytermsSent && (_keyterms.Terms.Count > 0 || _keyterms.Composition != AppKeytermComposition.WithServerBase))
        {
            message.Keyterms.AddRange(_keyterms.Terms);
            if (_keyterms.Composition != AppKeytermComposition.WithServerBase)
            {
                message.KeytermComposition = KeytermCompositionGrpcMapper.ToProto(_keyterms.Composition);
            }

            _keytermsSent = true;
        }

        return message;
    }
}
