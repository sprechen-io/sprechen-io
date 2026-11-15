using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Contracts.Tests.FormSurfaces;

internal sealed class FormSurfaceProjectionTestContractFactory
{
    public FormContract CreateContractShell()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "sample.simple",
            FormId = FormId.From("surface-demo"),
            SupportedLanguages = ["de", "en", "es"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("entry"),
            StrictLocalization = false
        };
    }

    public FormContract CreateRichContract()
    {
        var contract = CreateContractShell();
        contract.Variables =
        [
            CreateVariable("emailVar", """{"kind":"string","formatHint":"email"}""", "contact.email"),
            CreateVariable("ageVar", """{"kind":"integer","min":18,"max":99}"""),
            CreateVariable("commentsVar", """{"kind":"string"}"""),
            CreateVariable("newsletterVar", """{"kind":"boolean"}"""),
            CreateVariable(
                "colorVar",
                """
                {"kind":"enum","values":[{"code":"red","displayName":{"de":"Rot","en":"Red"}},{"code":"blue","displayName":{"de":"Blau","en":"Blue"}}]}
                """),
            CreateVariable("countryVar", """{"kind":"string"}"""),
            CreateVariable(
                "tagsVar",
                """
                {"kind":"set","values":[{"code":"sports","displayName":{"de":"Sport","en":"Sports"}},{"code":"music","displayName":{"de":"Musik","en":"Music"}}]}
                """)
        ];
        contract.Fields =
        [
            new FieldDefinition
            {
                FieldId = FieldId.From("email"),
                VariableId = VariableId.From("emailVar"),
                Label = new LocalizedText { ["de"] = "E-Mail" },
                HelpText = new LocalizedText { ["de"] = "Geben Sie Ihre E-Mail-Adresse ein." },
                Placeholder = new LocalizedText { ["de"] = "name@example.com" },
                Examples = [new LocalizedText { ["de"] = "ana@example.com" }],
                Editability = FieldEditability.UserEditable,
                Projections = new FieldProjections
                {
                    Html = new FieldHtmlProjection
                    {
                        Autocomplete = " email ",
                        InputMode = " email ",
                        AccessibleDescription = new LocalizedText { ["de"] = "Wird fuer die Kontaktaufnahme genutzt." }
                    }
                }
            },
            new FieldDefinition
            {
                FieldId = FieldId.From("age"),
                VariableId = VariableId.From("ageVar"),
                Label = new LocalizedText { ["de"] = "Alter" },
                Editability = FieldEditability.UserEditable
            },
            new FieldDefinition
            {
                FieldId = FieldId.From("newsletter"),
                VariableId = VariableId.From("newsletterVar"),
                Label = new LocalizedText { ["de"] = "Newsletter" },
                Editability = FieldEditability.UserEditable
            },
            new FieldDefinition
            {
                FieldId = FieldId.From("color"),
                VariableId = VariableId.From("colorVar"),
                Label = new LocalizedText { ["de"] = "Farbe" },
                Editability = FieldEditability.UserEditable
            },
            new FieldDefinition
            {
                FieldId = FieldId.From("country"),
                VariableId = VariableId.From("countryVar"),
                Label = new LocalizedText { ["de"] = "Land" },
                Editability = FieldEditability.UserEditable
            },
            new FieldDefinition
            {
                FieldId = FieldId.From("tags"),
                VariableId = VariableId.From("tagsVar"),
                Label = new LocalizedText { ["de"] = "Interessen" },
                Editability = FieldEditability.UserEditable
            },
            new FieldDefinition
            {
                FieldId = FieldId.From("comments"),
                VariableId = VariableId.From("commentsVar"),
                Label = new LocalizedText { ["de"] = "Kommentare" },
                Editability = FieldEditability.UserEditable,
                PresentationHints = new FieldPresentationHints { MultilinePreferred = true }
            }
        ];
        contract.FieldGroups =
        [
            new FieldGroupDefinition
            {
                FieldGroupId = FieldGroupId.From("entry"),
                Title = new LocalizedText { ["de"] = "Kontakt" },
                Description = new LocalizedText { ["de"] = "Kontaktinformationen" },
                VoiceSummary = new LocalizedText { ["de"] = "Nennen Sie Ihre Kontaktdaten." },
                Members =
                [
                    new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("email") },
                    new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("age") },
                    new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("preferences") }
                ]
            },
            new FieldGroupDefinition
            {
                FieldGroupId = FieldGroupId.From("preferences"),
                Title = new LocalizedText { ["de"] = "Praeferenzen" },
                Description = new LocalizedText { ["de"] = "Auswahlfelder" },
                VoiceSummary = new LocalizedText { ["de"] = "Waehlen Sie Ihre Praeferenzen." },
                Members =
                [
                    new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("newsletter") },
                    new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("color") },
                    new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("country") },
                    new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("tags") },
                    new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("comments") }
                ]
            }
        ];
        contract.Constraints =
        [
            CreateConstraint("required-email", "email", ConstraintKinds.Required, new Dictionary<string, JsonElement>()),
            CreateConstraint(
                "email-pattern",
                "email",
                ConstraintKinds.Pattern,
                new Dictionary<string, JsonElement> { [ConstraintPropertyNames.Pattern] = ParseJsonElement("\"^[^@]+@[^@]+$\"") }),
            CreateConstraint(
                "email-min",
                "email",
                ConstraintKinds.MinLength,
                new Dictionary<string, JsonElement> { [ConstraintPropertyNames.MinLength] = ParseJsonElement("5") }),
            CreateConstraint(
                "email-max",
                "email",
                ConstraintKinds.MaxLength,
                new Dictionary<string, JsonElement> { [ConstraintPropertyNames.MaxLength] = ParseJsonElement("80") }),
            CreateConstraint(
                "country-one-of",
                "country",
                ConstraintKinds.OneOf,
                new Dictionary<string, JsonElement> { [ConstraintPropertyNames.Values] = ParseJsonElement("""["DE","ES"]""") })
        ];

        return contract;
    }

    public VariableDefinition CreateVariable(string variableId, string valueTypeJson)
    {
        return new VariableDefinition
        {
            VariableId = VariableId.From(variableId),
            Name = variableId,
            ValueType = ParseJsonElement(valueTypeJson)
        };
    }

    public VariableDefinition CreateVariable(string variableId, string valueTypeJson, string semanticTag)
    {
        var variable = CreateVariable(variableId, valueTypeJson);
        variable.SemanticTag = semanticTag;
        return variable;
    }

    public FieldDefinition CreateField(string fieldId, string variableId, string label)
    {
        return new FieldDefinition
        {
            FieldId = FieldId.From(fieldId),
            VariableId = VariableId.From(variableId),
            Label = new LocalizedText { ["de"] = label },
            Editability = FieldEditability.UserEditable
        };
    }

    public ConstraintDefinition CreateConstraint(
        string constraintId,
        string fieldId,
        string kind,
        Dictionary<string, JsonElement> additionalProperties)
    {
        return new ConstraintDefinition
        {
            ConstraintId = constraintId,
            Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From(fieldId) },
            Severity = ConstraintSeverity.Error,
            Code = constraintId,
            Message = new LocalizedText { ["de"] = constraintId },
            Kind = ConstraintKindExtensions.TryFromWireToken(kind, out var parsedKind) ? parsedKind : ConstraintKind.Unknown,
            AdditionalProperties = additionalProperties
        };
    }

    public JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
