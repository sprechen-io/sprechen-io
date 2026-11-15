using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Services;

public sealed class EvaluationTelemetryErrorClassifierTests
{
    [Fact]
    public void ClassifyConstraintViolation_CarriesTheReportedMembersAndRoutesTheCode()
    {
        var error = EvaluationTelemetryErrorClassifier.ClassifyConstraintViolation(
            "field-1",
            "no_speech_detected",
            TelemetrySeverityLevels.Warning,
            ConstraintScopes.FieldGroup,
            "group-1",
            TelemetryErrorTriggers.SystemRecognition);

        Assert.Equal("no_speech_detected", error.ErrorCode);
        Assert.Equal(TelemetrySeverityLevels.Warning, error.Severity);
        Assert.Equal(TelemetryScopes.FieldGroup, error.Scope);
        Assert.Equal("field-1", error.FieldId);
        Assert.Equal("group-1", error.FieldGroupId);
        Assert.Equal(TelemetryStudyErrorRoute.Silence, error.Route);
    }

    [Fact]
    public void MapScope_IsTheModelsPairingAndNothingElse()
    {
        foreach (var (contractScope, loggingScope) in TelemetryScopeMapping.ContractToLoggingScope)
        {
            Assert.Equal(loggingScope, EvaluationTelemetryErrorClassifier.MapScope(contractScope));
        }
    }

    // Unbekannte Geltungsbereiche bleiben unverändert, damit diese Zuordnung
    // keinen anderen als den gemeldeten Bereich erfindet.
    [Theory]
    [InlineData("unknown")]
    [InlineData("FIELD_GROUP")]
    [InlineData(" fieldGroup ")]
    public void MapScope_WhenThePairingDoesNotNameTheScope_CarriesItOverUnchanged(string scope)
    {
        Assert.Equal(scope, EvaluationTelemetryErrorClassifier.MapScope(scope));
    }

    // Bereits im Telemetrieformat angegebene Bereiche bleiben unverändert.
    [Theory]
    [InlineData("cross_group")]
    [InlineData("form")]
    public void MapScope_WhenTheScopeIsAlreadyTheLoggingTerm_KeepsIt(string scope)
    {
        Assert.Equal(scope, EvaluationTelemetryErrorClassifier.MapScope(scope));
    }

    // Prüft die modellierte Zuordnung der aufgeführten Fehlercodes.
    // Nicht ausgewertete Eingaben und unbekannte Befehle erzeugen über diese Zuordnung keinen Studienfehler.
    [Theory]
    [InlineData("no_speech_detected", TelemetryStudyErrorRoute.Silence)]
    [InlineData("no_input", TelemetryStudyErrorRoute.Silence)]
    [InlineData("capture_interrupted", TelemetryStudyErrorRoute.Recognition)]
    [InlineData("recognized_as_empty", TelemetryStudyErrorRoute.Recognition)]
    [InlineData("low_stt_confidence", TelemetryStudyErrorRoute.Recognition)]
    [InlineData("stt_error", TelemetryStudyErrorRoute.Recognition)]
    [InlineData("unrecognized_input", TelemetryStudyErrorRoute.None)]
    [InlineData("unknown_command", TelemetryStudyErrorRoute.None)]
    [InlineData("keyboard_mode_active", TelemetryStudyErrorRoute.Validation)]
    [InlineData("voice_input_forbidden", TelemetryStudyErrorRoute.Validation)]
    [InlineData("spoken_value_unavailable", TelemetryStudyErrorRoute.Validation)]
    // Nicht ausdrücklich zugeordnete Bedingungscodes werden als Validierungsfehler behandelt.
    [InlineData("form.required", TelemetryStudyErrorRoute.Validation)]
    [InlineData("sample.checkout.email.invalid", TelemetryStudyErrorRoute.Validation)]
    public void ClassifyConstraintViolation_RoutesErrorCodesPerModelOwnedRoutingMap(
        string errorCode,
        TelemetryStudyErrorRoute expectedRoute)
    {
        var error = EvaluationTelemetryErrorClassifier.ClassifyConstraintViolation(
            "field-1",
            errorCode,
            TelemetrySeverityLevels.Warning,
            TelemetryScopes.Field,
            fieldGroupId: null,
            TelemetryErrorTriggers.UserInput);

        Assert.Equal(expectedRoute, error.Route);
    }
}
