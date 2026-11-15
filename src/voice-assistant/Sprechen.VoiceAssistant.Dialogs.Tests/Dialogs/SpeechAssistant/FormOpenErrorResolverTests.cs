using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormOpenErrorResolverTests
{
    [Fact]
    public void ResolveOpenErrors_WithoutViolations_FindsNothingOpen()
    {
        var openErrors = FormOpenErrorResolver.ResolveOpenErrors(ImmutableArray<FormConstraintViolation>.Empty);

        Assert.Empty(openErrors);
    }

    [Fact]
    public void ResolveOpenErrors_CountsEveryFieldThatBreaksARuleOfItsOwn()
    {
        var violations = ImmutableArray.Create(
            CreateViolation(fieldId: "first", code: "form.required"),
            CreateViolation(fieldId: "second", code: "form.required"),
            CreateViolation(fieldId: "third", code: "form.email.invalid"));

        var openErrors = FormOpenErrorResolver.ResolveOpenErrors(violations);

        Assert.Equal(
            ["first", "second", "third"],
            openErrors.Select(openError => openError.FieldId));
    }

    [Fact]
    public void ResolveOpenErrors_CountsAFieldThatBreaksSeveralRulesOnce()
    {
        // Mehrere verletzte Regeln desselben Feldes ergeben hier nur einen offenen Fehler.
        // Die Auswahl der zuerst zu behandelnden Regel erfolgt separat nach ihrer Priorität.
        var violations = ImmutableArray.Create(
            CreateViolation(fieldId: "first", code: "form.required"),
            CreateViolation(fieldId: "first", code: "form.minLength"));

        var openErrors = FormOpenErrorResolver.ResolveOpenErrors(violations);

        Assert.Single(openErrors);
    }

    [Fact]
    public void ResolveOpenErrors_WhenARuleOfTheFieldBlocks_TakesItOverTheMerelyAdvisoryOne()
    {
        // Eine blockierende Regel muss Vorrang vor einem bloßen Hinweis desselben Feldes haben.
        var violations = ImmutableArray.Create(
            CreateViolation(fieldId: "first", code: "form.advisory", severity: SeverityLevels.Warning),
            CreateViolation(fieldId: "first", code: "form.required"));

        var openErrors = FormOpenErrorResolver.ResolveOpenErrors(violations);

        var openError = Assert.Single(openErrors);
        Assert.Equal("form.required", openError.Code);
    }

    [Fact]
    public void ResolveOpenErrors_AtEqualSeverity_TakesTheRuleInstanceAndNotTheOrderItArrivedIn()
    {
        // Bei gleichem Schweregrad entscheidet die Regelkennung; die Reihenfolge
        // der gemeldeten Verletzungen darf die Auswahl nicht bestimmen.
        var violations = ImmutableArray.Create(
            CreateViolation(fieldId: "first", code: "form.required"),
            CreateViolation(fieldId: "first", code: "form.minLength"));

        var openErrors = FormOpenErrorResolver.ResolveOpenErrors(violations);

        var openError = Assert.Single(openErrors);
        Assert.Equal("form.minLength", openError.Code);
    }

    [Fact]
    public void ResolveOpenErrors_CountsARuleReachedThroughSeveralFieldsOfOneGroupOnce()
    {
        var violations = ImmutableArray.Create(
            CreateViolation(fieldId: "start", code: "form.dateOrder", scope: ConstraintScopes.CrossGroup, fieldGroupId: "dates"),
            CreateViolation(fieldId: "end", code: "form.dateOrder", scope: ConstraintScopes.CrossGroup, fieldGroupId: "dates"));

        var openErrors = FormOpenErrorResolver.ResolveOpenErrors(violations);

        var openError = Assert.Single(openErrors);
        Assert.Equal("start", openError.FieldId);
    }

    [Fact]
    public void ResolveOpenErrors_CountsAGroupRuleOncePerGroupItIsOpenIn()
    {
        // Dieselbe Gruppenregel ist in zwei verschiedenen Gruppen jeweils ein offener Fehler.
        var violations = ImmutableArray.Create(
            CreateViolation(fieldId: "first", code: "form.groupIncomplete", scope: ConstraintScopes.FieldGroup, fieldGroupId: "one"),
            CreateViolation(fieldId: "second", code: "form.groupIncomplete", scope: ConstraintScopes.FieldGroup, fieldGroupId: "two"));

        var openErrors = FormOpenErrorResolver.ResolveOpenErrors(violations);

        Assert.Equal(["one", "two"], openErrors.Select(openError => openError.FieldGroupId));
    }

    [Fact]
    public void ResolveOpenErrors_KeepsTwoDifferentRulesOfOneGroupApart()
    {
        var violations = ImmutableArray.Create(
            CreateViolation(fieldId: "start", code: "form.dateOrder", scope: ConstraintScopes.CrossGroup, fieldGroupId: "dates"),
            CreateViolation(fieldId: "end", code: "form.dateRange", scope: ConstraintScopes.CrossGroup, fieldGroupId: "dates"));

        var openErrors = FormOpenErrorResolver.ResolveOpenErrors(violations);

        Assert.Equal(["form.dateOrder", "form.dateRange"], openErrors.Select(openError => openError.Code));
    }

    [Fact]
    public void ResolveOpenErrors_KeepsAnErrorThatNamesNoField()
    {
        var violations = ImmutableArray.Create(
            CreateViolation(fieldId: string.Empty, code: "form.incomplete", scope: ConstraintScopes.Form, fieldGroupId: null),
            CreateViolation(fieldId: string.Empty, code: "form.inconsistent", scope: ConstraintScopes.Form, fieldGroupId: null));

        var openErrors = FormOpenErrorResolver.ResolveOpenErrors(violations);

        Assert.Equal(["form.incomplete", "form.inconsistent"], openErrors.Select(openError => openError.Code));
    }

    private static FormConstraintViolation CreateViolation(
        string fieldId,
        string code,
        string scope = ConstraintScopes.Field,
        string? fieldGroupId = "group",
        string severity = SeverityLevels.Error)
        => new(
            FieldId: fieldId,
            ConstraintId: code + "@" + fieldId,
            Severity: severity,
            Code: code,
            Message: "Nachricht",
            Remediation: null,
            Scope: scope,
            FieldGroupId: fieldGroupId);
}
