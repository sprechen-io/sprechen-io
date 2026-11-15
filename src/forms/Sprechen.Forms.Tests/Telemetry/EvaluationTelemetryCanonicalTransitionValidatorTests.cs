using System.Text.Json;
using Sprechen.EvaluationTelemetry.Ingestion;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Xunit;

namespace Sprechen.Forms.Tests.Telemetry;

/// <summary>
/// Prüft, ob aufgezeichnete Übergänge eine geordnete Teilmenge der aus den
/// Ereignisdaten abgeleiteten Übergänge sind. Die Prüfung der zulässigen
/// Dateneigenschaften gehört zum separaten Schema-Validator.
/// </summary>
public sealed class EvaluationTelemetryCanonicalTransitionValidatorTests
{
    private readonly EvaluationTelemetryCanonicalTransitionValidator _sut = new();

    [Fact]
    public void Validate_WithoutAnEventName_Throws()
    {
        Assert.Throws<ArgumentException>(() => _sut.Validate(
            " ",
            ParseJson("{\"canonical_transitions\":[]}")));
    }

    [Fact]
    public void Validate_WithoutData_RefusesBecauseNothingCanBeProjected()
    {


        Assert.False(_sut.Validate(TelemetryEventNames.TaskEnd, data: null));
    }

    [Fact]
    public void Validate_WithTransitionsTheDataDoesNotProject_Refuses()
    {
        // Der Fokuswechsel auf f1 darf keinen aufgezeichneten Zielwechsel auf f2 enthalten.
        var refused = _sut.Validate(
            TelemetryEventNames.FocusChange,
            ParseJson("{\"field_id\":\"f1\",\"canonical_transitions\":[{\"canonical_class\":"
                + "\"field_target_changed\",\"scope\":\"field\",\"object_id\":\"f2\","
                + "\"resulting_state\":\"targeted\"}]}"));

        Assert.False(refused);
    }

    [Fact]
    public void Validate_WithAnEmptyRecordingOnAProjectingLine_AcceptsTheEmptySubset()
    {
        // Bereits übernommene Übergänge müssen nicht erneut aufgezeichnet werden.
        // Eine leere Aufzeichnung bleibt deshalb auch bei einer nichtleeren Projektion zulässig.
        var accepted = _sut.Validate(
            TelemetryEventNames.FocusChange,
            ParseJson("{\"field_id\":\"f1\",\"canonical_transitions\":[]}"));

        Assert.True(accepted);
    }

    [Fact]
    public void Validate_WithANavigationLineThatRecordsNeitherTarget_Accepts()
    {
        // Prüft ein Navigationsereignis ohne erneut aufgezeichneten Zielwechsel,
        // wie es nach einem zuvor übernommenen Fokuswechsel zulässig ist.
        var accepted = _sut.Validate(
            TelemetryEventNames.NavigationChanged,
            ParseJson("{\"current_field_id\":\"f2\",\"current_field_group_id\":\"g1\","
                + "\"navigation_index\":2,\"navigation_total\":10,\"canonical_transitions\":[]}"));

        Assert.True(accepted);
    }

    [Fact]
    public void Validate_WithTransitionsTheDataProjects_Accepts()
    {
        var accepted = _sut.Validate(
            TelemetryEventNames.ValueChange,
            ParseJson("{\"field_id\":\"f1\",\"canonical_transitions\":[{\"canonical_class\":"
                + "\"confirmed_field_value_changed\",\"scope\":\"field\",\"object_id\":\"f1\","
                + "\"resulting_state\":\"confirmed_changed\"}]}"));

        Assert.True(accepted);
    }

    [Fact]
    public void Validate_WithATransitionAnEventNeverProjects_Refuses()
    {
        // Ein Aufgabenstart ergibt keine projizierten Übergänge;
        // eine nichtleere Aufzeichnung ist daher unzulässig.
        var refused = _sut.Validate(
            TelemetryEventNames.TaskStart,
            ParseJson("{\"canonical_transitions\":[{\"canonical_class\":\"field_target_changed\","
                + "\"scope\":\"field\",\"object_id\":\"f1\",\"resulting_state\":\"targeted\"}]}"));

        Assert.False(refused);
    }

    [Fact]
    public void Validate_WithAnUnknownDataProperty_LeavesThatRefusalToTheModel()
    {
        // Dieser Validator berücksichtigt nur Eigenschaften für die Übergangsprojektion.
        // Die Ablehnung unbekannter Eigenschaften erfolgt separat durch die Schemaprüfung.
        var accepted = _sut.Validate(
            TelemetryEventNames.ValueChange,
            ParseJson("{\"field_id\":\"f1\",\"unexpected\":true,\"canonical_transitions\":[{"
                + "\"canonical_class\":\"confirmed_field_value_changed\",\"scope\":\"field\","
                + "\"object_id\":\"f1\",\"resulting_state\":\"confirmed_changed\"}]}"));

        Assert.True(accepted);
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
