using Sprechen.Forms.TypeScriptCodegen;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.Forms.Tests.TypeScriptCodegen;

/// <summary>
/// Prüft die wiederholbare Erzeugung des TypeScript-Vokabulars und ausgewählte Modellwerte.
/// </summary>
public sealed class TelemetryVocabularySyncTests
{

    [Fact]
    public void Generator_IsDeterministic_AndCoversTheModelVocabulary()
    {
        var first = TelemetryVocabularyGenerator.GenerateText();
        var second = TelemetryVocabularyGenerator.GenerateText();

        Assert.Equal(first, second);

        foreach (var tsTypeName in new[]
                 {
                     "StudyCondition", "TaskEndState", "AmbiguityType", "AmbiguityResolutionStrategy",
                     "AmbiguitySignalClass", "AmbiguityResult", "Severity", "LoggingScope", "ContractScope",
                     "ErrorTrigger",
                 })
        {
            Assert.Contains($"type {tsTypeName} =\n", first, StringComparison.Ordinal);
        }


        Assert.Contains("var telemetryScopeMapping: Record<ContractScope, LoggingScope> = {\n", first, StringComparison.Ordinal);
        Assert.Contains("    fieldGroup: \"field_group\"", first, StringComparison.Ordinal);
        Assert.Contains("var normalizeContractScope = ", first, StringComparison.Ordinal);
        Assert.Contains("var toLoggingScope = ", first, StringComparison.Ordinal);


        Assert.DoesNotContain("\"timeout\"", first, StringComparison.Ordinal);
        Assert.Contains("| \"opt_in_proposal\"", first, StringComparison.Ordinal);
        Assert.Contains($"var telemetryUnknownParticipantId = \"{TelemetryEnvelopeContextDefaults.UnknownParticipantId}\";", first, StringComparison.Ordinal);
        Assert.Contains($"var telemetryUnknownTaskId = \"{TelemetryEnvelopeContextDefaults.UnknownTaskId}\";", first, StringComparison.Ordinal);
    }
}
