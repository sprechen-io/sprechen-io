using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormJumpTargetResolverTests
{
    [Fact]
    public void TryResolve_WhenSpokenNameDiffersFromLabel_ReturnsFieldTarget()
    {
        var resolver = CreateResolver();

        var resolved = resolver.TryResolve(FormJumpTarget.From("briefkasten"), out var target);

        Assert.True(resolved);
        Assert.NotNull(target);
        Assert.Equal(FormJumpTargetKind.Field, target.Kind);
        Assert.Equal("emailField", target.TargetId);
    }

    [Fact]
    public void TryResolve_WhenFieldLabelMatches_ReturnsFieldTarget()
    {
        var resolver = CreateResolver();

        var resolved = resolver.TryResolve(FormJumpTarget.From("E-Mail-Adresse"), out var target);

        Assert.True(resolved);
        Assert.NotNull(target);
        Assert.Equal(FormJumpTargetKind.Field, target.Kind);
        Assert.Equal("emailField", target.TargetId);
    }

    [Fact]
    public void TryResolve_WhenPrimaryNameMatches_ReturnsFieldTarget()
    {
        var resolver = CreateResolver();

        var resolved = resolver.TryResolve(FormJumpTarget.From("email"), out var target);

        Assert.True(resolved);
        Assert.NotNull(target);
        Assert.Equal(FormJumpTargetKind.Field, target.Kind);
        Assert.Equal("emailField", target.TargetId);
    }

    [Fact]
    public void TryResolve_WhenSynonymMatches_ReturnsFieldTarget()
    {
        var resolver = CreateResolver();

        var resolved = resolver.TryResolve(FormJumpTarget.From("ort"), out var target);

        Assert.True(resolved);
        Assert.NotNull(target);
        Assert.Equal(FormJumpTargetKind.Field, target.Kind);
        Assert.Equal("cityField", target.TargetId);
    }

    [Fact]
    public void TryResolve_WhenSynonymMatchesMultipleVisibleFields_ReturnsFirstFieldInDocumentOrder()
    {
        var resolver = CreateResolver();

        var resolved = resolver.TryResolve(FormJumpTarget.From("kontakt"), out var target);

        Assert.True(resolved);
        Assert.NotNull(target);
        Assert.Equal(FormJumpTargetKind.Field, target.Kind);
        Assert.Equal("emailField", target.TargetId);
    }

    [Fact]
    public void TryResolve_WhenSynonymMatchesUnavailableField_SkipsToVisibleCandidate()
    {
        var resolver = CreateResolver(availableFieldIds: ["cityField"]);

        var resolved = resolver.TryResolve(FormJumpTarget.From("kontakt"), out var target);

        Assert.True(resolved);
        Assert.NotNull(target);
        Assert.Equal(FormJumpTargetKind.Field, target.Kind);
        Assert.Equal("cityField", target.TargetId);
    }

    [Fact]
    public void TryResolve_WhenOnlyUnavailableSynonymMatches_ReturnsFalse()
    {
        var resolver = CreateResolver(availableFieldIds: ["emailField"]);

        var resolved = resolver.TryResolve(FormJumpTarget.From("ort"), out var target);

        Assert.False(resolved);
        Assert.Null(target);
    }

    [Fact]
    public void TryResolve_WhenAudiblyOnlyFieldLabelMatches_ReturnsFieldTarget()
    {
        // Die Zielauflösung berücksichtigt hörbar verfügbare Felder. Der Testzustand bildet
        // ein solches Feld ohne Eingabeverfügbarkeit ab.
        var resolver = CreateResolver(audiblyOnlyFieldIds: ["cityField"]);

        var resolved = resolver.TryResolve(FormJumpTarget.From("Stadt"), out var target);

        Assert.True(resolved);
        Assert.NotNull(target);
        Assert.Equal(FormJumpTargetKind.Field, target.Kind);
        Assert.Equal("cityField", target.TargetId);
    }

    [Fact]
    public void TryResolve_WhenGroupContainsOnlyAudiblyAvailableFields_ReturnsFieldGroupTarget()
    {
        // Auch eine Gruppe mit ausschließlich hörbar verfügbaren Feldern bleibt
        // ein auflösbares Sprungziel.
        var resolver = CreateResolver(audiblyOnlyFieldIds: ["cityField"]);

        var resolved = resolver.TryResolve(FormJumpTarget.From("Adresse"), out var target);

        Assert.True(resolved);
        Assert.NotNull(target);
        Assert.Equal(FormJumpTargetKind.FieldGroup, target.Kind);
        Assert.Equal("address", target.TargetId);
    }

    [Fact]
    public void TryResolve_WhenFieldGroupTitleMatchesNavigableGroup_ReturnsFieldGroupTarget()
    {
        var resolver = CreateResolver();

        var resolved = resolver.TryResolve(FormJumpTarget.From("Adresse"), out var target);

        Assert.True(resolved);
        Assert.NotNull(target);
        Assert.Equal(FormJumpTargetKind.FieldGroup, target.Kind);
        Assert.Equal("address", target.TargetId);
    }

    private static FormJumpTargetResolver CreateResolver(
        IReadOnlyCollection<string>? availableFieldIds = null,
        IReadOnlyCollection<string>? audiblyOnlyFieldIds = null)
    {
        var orderedFieldIds = ImmutableArray.Create("emailField", "cityField");
        var fieldsById = new Dictionary<string, FieldDefinition>(StringComparer.Ordinal)
        {
            ["emailField"] = new()
            {
                FieldId = FieldId.From("emailField"),
                VariableId = VariableId.From("email"),
                Label = new LocalizedText { ["de"] = "E-Mail-Adresse" },
                Editability = FieldEditability.UserEditable,
                Projections = new FieldProjections
                {
                    Voice = new FieldVoiceProjection
                    {
                        Names = new LocalizedText { ["de"] = "briefkasten" }
                    }
                }
            },
            ["cityField"] = new()
            {
                FieldId = FieldId.From("cityField"),
                VariableId = VariableId.From("city"),
                Label = new LocalizedText { ["de"] = "Stadt" },
                Editability = FieldEditability.UserEditable
            }
        }.ToImmutableDictionary(kvp => FieldId.From(kvp.Key), kvp => kvp.Value);

        var fieldGroupsById = new Dictionary<string, FieldGroupDefinition>(StringComparer.Ordinal)
        {
            ["entry"] = new()
            {
                FieldGroupId = FieldGroupId.From("entry"),
                Title = new LocalizedText { ["de"] = "Einstieg" },
                Members =
                [
                    new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("emailField") },
                    new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("address") }
                ]
            },
            ["address"] = new()
            {
                FieldGroupId = FieldGroupId.From("address"),
                Title = new LocalizedText { ["de"] = "Adresse" },
                Members =
                [
                    new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("cityField") }
                ]
            }
        };

        var localizationResolver = new FormLocalizationResolver(
            requestedIsoCode: "de",
            defaultLanguage: "de",
            supportedLanguages: ["de"],
            strictLocalization: false,
            politenessLevel: PolitenessLevel.Formal,
            variablesById: ImmutableDictionary<VariableId, VariableDefinition>.Empty,
            fieldGroupsById: fieldGroupsById.ToImmutableDictionary(kvp => FieldGroupId.From(kvp.Key), kvp => kvp.Value));

        var fieldIndexById = orderedFieldIds
            .Select((fieldId, index) => (fieldId, index))
            .ToImmutableDictionary(pair => pair.fieldId, pair => pair.index, StringComparer.Ordinal);
        var fieldToFieldGroupId = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["emailField"] = "entry",
            ["cityField"] = "address"
        }.ToImmutableDictionary(StringComparer.Ordinal);
        var parentByFieldGroupId = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["entry"] = null,
            ["address"] = "entry"
        }.ToImmutableDictionary(StringComparer.Ordinal);
        var navigatorState = new TestNavigatorState(
            orderedFieldIds,
            fieldIndexById,
            fieldToFieldGroupId,
            parentByFieldGroupId);

        if (availableFieldIds is not null)
        {
            foreach (var fieldId in orderedFieldIds)
            {
                navigatorState.SetFieldAvailable(fieldId, availableFieldIds.Contains(fieldId));
            }
        }

        if (audiblyOnlyFieldIds is not null)
        {
            foreach (var fieldId in audiblyOnlyFieldIds)
            {
                navigatorState.SetFieldAudiblyOnly(fieldId);
            }
        }

        var fieldNameSynonyms = new Dictionary<string, SynonymSet>(StringComparer.Ordinal)
        {
            ["emailField"] = new()
            {
                PrimaryName = new LocalizedText { ["de"] = "email" },
                Synonyms =
                [
                    new LocalizedText { ["de"] = "kontakt" },
                    new LocalizedText { ["en"] = "mail" }
                ]
            },
            ["cityField"] = new()
            {
                PrimaryName = new LocalizedText { ["de"] = "stadt" },
                Synonyms =
                [
                    new LocalizedText { ["de"] = "ort" },
                    new LocalizedText { ["de"] = "kontakt" }
                ]
            }
        };

        return new FormJumpTargetResolver(
            orderedFieldIds,
            fieldsById,
            ImmutableArray.Create("entry", "address"),
            fieldNameSynonyms,
            localizationResolver,
            navigatorState);
    }
}
