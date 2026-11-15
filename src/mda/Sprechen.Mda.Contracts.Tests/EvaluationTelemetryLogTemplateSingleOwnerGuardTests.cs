using System.Text.RegularExpressions;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.Mda.Contracts.Tests;

/// <summary>
/// Prüft, dass die gemeinsame Telemetrievorlage jedes definierte Umschlagfeld
/// genau einmal und keine zusätzlichen Platzhalter enthält.
/// </summary>
public sealed class EvaluationTelemetryLogTemplateSingleOwnerGuardTests
{
    private static readonly Regex PlaceholderRegex = new(
        @"\{[a-z_]+\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void TheTemplateCarriesEveryEnvelopeMemberAsAPlaceholder()
    {
        foreach (var member in TelemetryEnvelopeFieldNames.All)
        {
            Assert.Contains(
                $"{{{member}}}",
                EvaluationTelemetryLogTemplate.MessageTemplate,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheTemplateCarriesNoPlaceholderThatIsNotAnEnvelopeMember()
    {
        var placeholders = PlaceholderRegex
            .Matches(EvaluationTelemetryLogTemplate.MessageTemplate)
            .Select(match => match.Value.Trim('{', '}'))
            .ToArray();

        Assert.Equal(TelemetryEnvelopeFieldNames.All.Count, placeholders.Length);

        foreach (var placeholder in placeholders)
        {
            Assert.Contains(placeholder, TelemetryEnvelopeFieldNames.All);
        }
    }
}
