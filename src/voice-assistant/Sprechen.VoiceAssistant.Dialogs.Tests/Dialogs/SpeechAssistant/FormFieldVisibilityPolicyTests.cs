using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormFieldVisibilityPolicyTests
{
    [Fact]
    public void IsFieldGroupVisible_WhenAncestorIsHidden_ReturnsFalse()
    {
        var service = CreateService(
            fieldGroupVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["main"] = true,
                ["child"] = true,
                ["grandchild"] = true
            },
            parentFieldGroupById: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["main"] = null,
                ["child"] = "main",
                ["grandchild"] = "child"
            });

        Assert.True(service.IsFieldGroupVisible("grandchild"));

        service = CreateService(
            fieldGroupVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["main"] = false,
                ["child"] = true,
                ["grandchild"] = true
            },
            parentFieldGroupById: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["main"] = null,
                ["child"] = "main",
                ["grandchild"] = "child"
            });

        Assert.False(service.IsFieldGroupVisible("grandchild"));
    }

    [Fact]
    public void IsFieldGroupOnPath_WhenAncestorMatches_ReturnsTrue()
    {
        var service = CreateService(
            parentFieldGroupById: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["main"] = null,
                ["child"] = "main",
                ["grandchild"] = "child"
            });

        Assert.True(service.IsFieldGroupOnPath("grandchild", "main"));
        Assert.True(service.IsFieldGroupOnPath("grandchild", "child"));
        Assert.False(service.IsFieldGroupOnPath("main", "child"));
    }

    [Fact]
    public void IsFieldEffectivelyVisible_WhenFieldOrSectionHidden_ReturnsFalse()
    {
        var service = CreateService(
            fieldGroupVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["main"] = true
            },
            fieldVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["name"] = true
            },
            fieldToFieldGroupId: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = "main"
            });

        Assert.True(service.IsFieldEffectivelyVisible("name"));

        service = CreateService(
            fieldGroupVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["main"] = false
            },
            fieldVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["name"] = true
            },
            fieldToFieldGroupId: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = "main"
            });

        Assert.False(service.IsFieldEffectivelyVisible("name"));

        service = CreateService(
            fieldGroupVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["main"] = true
            },
            fieldVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["name"] = false
            },
            fieldToFieldGroupId: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = "main"
            });

        Assert.False(service.IsFieldEffectivelyVisible("name"));
    }

    [Theory]
    // editability, visible, enabled, runtimeReadOnly => expectedInputAvailable, expectedAudiblyAvailable
    [InlineData(FieldEditability.UserEditable, true, true, false, true, true)]
    [InlineData(FieldEditability.Unknown, true, true, false, true, true)]
    [InlineData(FieldEditability.ReadOnly, true, true, false, false, true)]
    [InlineData(FieldEditability.Computed, true, true, false, false, false)]
    [InlineData(FieldEditability.UserEditable, false, true, false, false, false)]
    [InlineData(FieldEditability.Unknown, false, true, false, false, false)]
    [InlineData(FieldEditability.ReadOnly, false, true, false, false, false)]
    [InlineData(FieldEditability.Computed, false, true, false, false, false)]
    [InlineData(FieldEditability.UserEditable, true, false, false, false, false)]
    [InlineData(FieldEditability.Unknown, true, false, false, false, false)]
    [InlineData(FieldEditability.Computed, true, false, false, false, false)]
    [InlineData(FieldEditability.ReadOnly, true, false, false, false, true)]
    [InlineData(FieldEditability.UserEditable, true, true, true, false, false)]
    [InlineData(FieldEditability.Unknown, true, true, true, false, false)]
    [InlineData(FieldEditability.ReadOnly, true, true, true, false, true)]
    [InlineData(FieldEditability.Computed, true, true, true, false, false)]
    public void AvailabilityPredicates_FollowEditabilityVisibilityEnabledAndRuntimeReadOnlyMatrix(
        FieldEditability editability,
        bool visible,
        bool enabled,
        bool runtimeReadOnly,
        bool expectedInputAvailable,
        bool expectedAudiblyAvailable)
    {
        var service = CreateAvailabilityService(editability, visible, enabled, runtimeReadOnly);

        Assert.Equal(expectedInputAvailable, service.IsFieldInputAvailable("name"));
        Assert.Equal(expectedAudiblyAvailable, service.IsFieldAudiblyAvailable("name"));
    }

    [Fact]
    public void AvailabilityPredicates_DivergeExactlyForVisibleReadOnlyDisplayFields()
    {
        // Bei aktivierten Feldern ohne Laufzeitschreibschutz unterscheiden sich die beiden
        // Prädikate nur für sichtbare, ausdrücklich schreibgeschützte Felder:
        // Sie sind hörbar verfügbar, aber nicht für Eingaben.
        foreach (var editability in new[]
                 {
                     FieldEditability.Unknown,
                     FieldEditability.UserEditable,
                     FieldEditability.ReadOnly,
                     FieldEditability.Computed
                 })
        {
            foreach (var visible in new[] { true, false })
            {
                var service = CreateAvailabilityService(editability, visible, enabled: true, runtimeReadOnly: false);
                var diverges = service.IsFieldInputAvailable("name") != service.IsFieldAudiblyAvailable("name");
                var expectedDivergence = visible && editability == FieldEditability.ReadOnly;

                Assert.Equal(expectedDivergence, diverges);
            }
        }
    }

    [Fact]
    public void MalformedEditabilityWireToken_StaysOnTheEditablePath_AndNeverBecomesDisplayOnly()
    {
        // Der ungültige JSON-Wert wird als Unknown gelesen. Für das sichtbare, aktivierte Feld
        // ohne Laufzeitschreibschutz bleiben beide Verfügbarkeiten wahr; es wird dadurch
        // nicht zu einem reinen Anzeigefeld.
        var field = JsonSerializer.Deserialize<FieldDefinition>(
            """{"fieldId":"name","variableId":"name","label":{"de":"Name"},"editability":"totally-bogus"}""")!;
        Assert.Equal(FieldEditability.Unknown, field.Editability);

        var service = CreateService(
            fieldGroupVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal) { ["main"] = true },
            fieldVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal) { ["name"] = true },
            fieldEnabledById: new Dictionary<string, bool>(StringComparer.Ordinal) { ["name"] = true },
            fieldReadOnlyById: new Dictionary<string, bool>(StringComparer.Ordinal) { ["name"] = false },
            fieldToFieldGroupId: new Dictionary<string, string>(StringComparer.Ordinal) { ["name"] = "main" },
            fieldsById: new Dictionary<string, FieldDefinition>(StringComparer.Ordinal) { ["name"] = field });

        Assert.True(service.IsFieldInputAvailable("name"));
        Assert.True(service.IsFieldAudiblyAvailable("name"));
        Assert.Equal(service.IsFieldInputAvailable("name"), service.IsFieldAudiblyAvailable("name"));
    }

    private static FormFieldVisibilityPolicy CreateAvailabilityService(
        FieldEditability editability,
        bool visible,
        bool enabled,
        bool runtimeReadOnly)
        => CreateService(
            fieldGroupVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["main"] = true
            },
            fieldVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["name"] = visible
            },
            fieldEnabledById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["name"] = enabled
            },
            fieldReadOnlyById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["name"] = runtimeReadOnly
            },
            fieldToFieldGroupId: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = "main"
            },
            fieldsById: new Dictionary<string, FieldDefinition>(StringComparer.Ordinal)
            {
                ["name"] = new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Editability = editability
                }
            });

    [Fact]
    public void IsFieldInputAvailable_RespectsEnabledReadOnlyAndEditability()
    {
        var service = CreateService(
            fieldGroupVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["main"] = true
            },
            fieldVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["name"] = true
            },
            fieldEnabledById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["name"] = true
            },
            fieldReadOnlyById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["name"] = false
            },
            fieldToFieldGroupId: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = "main"
            },
            fieldsById: new Dictionary<string, FieldDefinition>(StringComparer.Ordinal)
            {
                ["name"] = new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Editability = FieldEditability.UserEditable
                }
            });

        Assert.True(service.IsFieldInputAvailable("name"));

        service = CreateService(
            fieldGroupVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["main"] = true
            },
            fieldVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["name"] = true
            },
            fieldEnabledById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["name"] = false
            },
            fieldToFieldGroupId: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = "main"
            },
            fieldsById: new Dictionary<string, FieldDefinition>(StringComparer.Ordinal)
            {
                ["name"] = new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Editability = FieldEditability.UserEditable
                }
            });

        Assert.False(service.IsFieldInputAvailable("name"));
    }

    [Fact]
    public void IsFieldGroupVisible_WithCyclicHierarchy_TerminatesWithoutHang()
    {
        var service = CreateService(
            fieldGroupVisibleById: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["a"] = true,
                ["b"] = true
            },
            parentFieldGroupById: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["a"] = "b",
                ["b"] = "a"
            });

        var result = service.IsFieldGroupVisible("a");

        Assert.True(result);
    }

    [Fact]
    public void IsFieldGroupOnPath_WithCyclicHierarchy_TerminatesWithoutHang()
    {
        var service = CreateService(
            parentFieldGroupById: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["a"] = "b",
                ["b"] = "a"
            });

        var result = service.IsFieldGroupOnPath("a", "nonexistent");

        Assert.False(result);
    }

    private static FormFieldVisibilityPolicy CreateService(
        IDictionary<string, bool>? fieldGroupVisibleById = null,
        IDictionary<string, bool>? fieldVisibleById = null,
        IDictionary<string, bool>? fieldEnabledById = null,
        IDictionary<string, bool>? fieldReadOnlyById = null,
        IDictionary<string, string?>? parentFieldGroupById = null,
        IDictionary<string, string>? fieldToFieldGroupId = null,
        IDictionary<string, FieldDefinition>? fieldsById = null)
    {
        return new FormFieldVisibilityPolicy(
            (fieldGroupVisibleById ?? new Dictionary<string, bool>(StringComparer.Ordinal))
            .ToDictionary(pair => FieldGroupId.From(pair.Key), pair => pair.Value),
            (fieldVisibleById ?? new Dictionary<string, bool>(StringComparer.Ordinal))
            .ToDictionary(pair => FieldId.From(pair.Key), pair => pair.Value),
            (fieldEnabledById ?? new Dictionary<string, bool>(StringComparer.Ordinal))
            .ToDictionary(pair => FieldId.From(pair.Key), pair => pair.Value),
            (fieldReadOnlyById ?? new Dictionary<string, bool>(StringComparer.Ordinal))
            .ToDictionary(pair => FieldId.From(pair.Key), pair => pair.Value),
            (parentFieldGroupById ?? new Dictionary<string, string?>(StringComparer.Ordinal))
            .ToImmutableDictionary(pair => FieldGroupId.From(pair.Key), pair => pair.Value),
            (fieldToFieldGroupId ?? new Dictionary<string, string>(StringComparer.Ordinal))
            .ToImmutableDictionary(pair => FieldId.From(pair.Key), pair => pair.Value),
            (fieldsById ?? new Dictionary<string, FieldDefinition>(StringComparer.Ordinal))
            .ToImmutableDictionary(pair => FieldId.From(pair.Key), pair => pair.Value));
    }
}



