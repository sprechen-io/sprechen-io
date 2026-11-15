using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Zusammensetzung von Validierungshinweisen aus Feldbezeichnung, Meldung und Korrekturhinweis.
/// Die Matrix kombiniert vier Bezeichnungsfälle, fünf Meldungsquellen und vier Korrekturhinweise.
/// Beim eingebetteten Ersatztext ist bereits eine Handlungsaufforderung enthalten;
/// anderen Quellen wird bei fehlendem modelliertem Korrekturhinweis der allgemeine Text angehängt.
/// Die Tests vergleichen Texte, keine tatsächliche Audioausgabe.
/// </summary>
public sealed class FormConstraintViolationPromptFormatterTests
{
    private const string FieldLabel = "Betrag";
    private const string UnderstoodValue = "zwölf";

    // Eingebettete Katalogtexte, die auch ValidationRepairPromptCatalogTests prüft.
    private const string EmbeddedFallbackFormal = "Die Eingabe ist ungültig. Bitte versuchen Sie es mit einem anderen Wert.";
    private const string EmbeddedFallbackInformal = "Die Eingabe ist ungültig. Bitte versuch es mit einem anderen Wert.";
    private const string RepairPromptFormal = "Bitte nennen Sie einen neuen Wert.";
    private const string RepairPromptInformal = "Bitte nenne einen neuen Wert.";


    private const string SourceModeledBareVerdict = "modeled-bare-verdict";
    private const string SourceModeledWithContent = "modeled-with-content";
    private const string SourceTailLessContractOverride = "tail-less-contract-override";
    private const string SourceContractOverrideWithOwnTail = "contract-override-with-own-tail";
    private const string SourceEmbeddedCatalogFallback = "embedded-catalog-fallback";

    private const string ModeledBareVerdictText = "Wert ungültig.";
    private const string ModeledContentText = "Der Wert muss zwischen 1 und 10 liegen.";
    private const string TailLessOverrideText = "Diese Eingabe passt nicht.";
    private const string OverrideWithOwnTailText = "Diese Eingabe passt nicht. Bitte korrigieren Sie die Eingabe.";
    private const string ModeledRemediationText = "Bitte nennen Sie einen Betrag zwischen 1 und 10.";

    public static IEnumerable<object?[]> FullMatrixCases()
    {
        string?[] labels = [null, "", "   ", FieldLabel];
        string[] sources =
        [
            SourceModeledBareVerdict,
            SourceModeledWithContent,
            SourceTailLessContractOverride,
            SourceContractOverrideWithOwnTail,
            SourceEmbeddedCatalogFallback
        ];
        string?[] remediations = [null, "", "   ", ModeledRemediationText];

        // Für die Ersatzquellen bleibt die modellierte Meldung leer.
        // Null, leere Zeichenkette und Leerzeichen wechseln zwischen den Fällen.
        string?[] blankMessages = [null, "", "   "];
        var blankRotation = 0;

        foreach (var label in labels)
        {
            foreach (var source in sources)
            {
                foreach (var remediation in remediations)
                {
                    var blankMessage = blankMessages[blankRotation++ % blankMessages.Length];
                    yield return [label, source, remediation, blankMessage];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(FullMatrixCases))]
    public void FormatViolationPrompt_NeverEndsVerdictOnly_AcrossTheFullMatrix(
        string? fieldLabel,
        string sourceKind,
        string? remediation,
        string? blankModeledMessage)
    {
        var contract = CreateContractFor(sourceKind);
        var violation = CreateViolation(ModeledMessageFor(sourceKind, blankModeledMessage), remediation);

        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            contract,
            "de",
            PolitenessLevel.Formal,
            violation,
            fieldLabel,
            understoodValue: null,
            includesValue: false);


        Assert.Equal(ExpectedUtterance(fieldLabel, sourceKind, remediation), result);

        // Der Text darf nicht mit einer der hier geprüften bloßen Fehlermeldungen enden.
        Assert.False(
            result.EndsWith(ModeledBareVerdictText, StringComparison.Ordinal)
                || result.EndsWith(TailLessOverrideText, StringComparison.Ordinal)
                || result.EndsWith("Die Eingabe ist ungültig.", StringComparison.Ordinal),
            $"Verdict-only utterance for label={Describe(fieldLabel)}, source={sourceKind}, remediation={Describe(remediation)}: \"{result}\"");

        // Dem eingebetteten Ersatztext wird kein zusätzlicher allgemeiner Korrekturhinweis angehängt.
        if (sourceKind == SourceEmbeddedCatalogFallback)
        {
            Assert.DoesNotContain(RepairPromptFormal, result, StringComparison.Ordinal);
        }
    }

    [Theory] // Ohne Feldbezeichnung, Meldung und Korrekturhinweis wird der eingebettete Ersatztext verwendet.
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankLabel_MissingMessage_MissingRemediation_SpeaksTheRepairedEmbeddedFallback(string? fieldLabel)
    {
        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreateContractWithoutOverride(),
            "de",
            PolitenessLevel.Formal,
            CreateViolation(message: null, remediation: null),
            fieldLabel,
            understoodValue: null,
            includesValue: false);

        Assert.NotEqual("Die Eingabe ist ungültig.", result);
        Assert.Equal(EmbeddedFallbackFormal, result);
    }

    [Theory] // Eine modellierte Fehlermeldung ohne Korrekturhinweis erhält den allgemeinen Hinweis.
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ModeledBareVerdict_WithoutRemediation_GetsTheRepairPromptAppended(string? remediation)
    {
        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreateContractWithoutOverride(),
            "de",
            PolitenessLevel.Formal,
            CreateViolation(ModeledBareVerdictText, remediation),
            fieldLabel: null,
            understoodValue: null,
            includesValue: false);

        Assert.Equal($"{ModeledBareVerdictText} {RepairPromptFormal}", result);
    }

    [Fact] // Eine modellierte Meldung mit Korrekturhinweis bleibt erhalten.
    public void ModeledMessage_WithRemediation_IsUnchanged()
    {
        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreateContractWithoutOverride(),
            "de",
            PolitenessLevel.Formal,
            CreateViolation(ModeledContentText, ModeledRemediationText),
            fieldLabel: null,
            understoodValue: null,
            includesValue: false);

        Assert.Equal($"{ModeledContentText} {ModeledRemediationText}", result);
    }

    [Fact] // Der eingebettete Ersatztext erhält keinen zweiten allgemeinen Korrekturhinweis.
    public void EmbeddedFallback_DoesNotDoubleAppendTheRepairPrompt()
    {
        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreateContractWithoutOverride(),
            "de",
            PolitenessLevel.Formal,
            CreateViolation(message: null, remediation: null),
            fieldLabel: null,
            understoodValue: null,
            includesValue: false);

        Assert.Equal(EmbeddedFallbackFormal, result);
        Assert.DoesNotContain(RepairPromptFormal, result, StringComparison.Ordinal);
    }

    [Fact] // Die Feldbezeichnung erscheint im Kontextsatz ohne Wert.
    public void ContextSentence_AppearsWhenLabelIsPresent_WithoutUnderstoodValue()
    {
        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreateContractWithoutOverride(),
            "de",
            PolitenessLevel.Formal,
            CreateViolation(ModeledContentText, ModeledRemediationText),
            FieldLabel,
            understoodValue: null,
            includesValue: false);

        Assert.Equal($"Es gibt ein Problem mit {FieldLabel}. {ModeledContentText} {ModeledRemediationText}", result);
    }

    [Fact] // Die Feldbezeichnung und der verstandene Wert erscheinen im Kontextsatz.
    public void ContextSentence_AppearsWhenLabelIsPresent_WithUnderstoodValue()
    {
        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreateContractWithoutOverride(),
            "de",
            PolitenessLevel.Formal,
            CreateViolation(ModeledContentText, ModeledRemediationText),
            FieldLabel,
            UnderstoodValue,
            includesValue: true);

        Assert.Equal($"Für {FieldLabel} habe ich {UnderstoodValue} verstanden. {ModeledContentText} {ModeledRemediationText}", result);
    }

    [Theory] // Eine Vertragsüberschreibung ohne Handlungsaufforderung ersetzt den eingebetteten Text.
    [InlineData(null)] // Auch ohne Bezeichnung und modellierten Korrekturhinweis muss eine Handlungsaufforderung entstehen.
    [InlineData("")]
    [InlineData("   ")]
    public void TailLessContractOverride_BlankLabelAndBlankRemediation_GetsTheRepairPromptAppended(string? remediation)
    {
        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreateContractWithInvalidValueOverride(TailLessOverrideText),
            "de",
            PolitenessLevel.Formal,
            CreateViolation(message: null, remediation),
            fieldLabel: null,
            understoodValue: null,
            includesValue: false);

        Assert.Equal($"{TailLessOverrideText} {RepairPromptFormal}", result);
    }

    [Fact] // Die Ergänzung richtet sich nach der Textquelle, nicht nach einer Analyse des Wortlauts. Auch eine Überschreibung mit eigener Handlungsaufforderung erhält den allgemeinen Hinweis.
    public void ContractOverrideCarryingItsOwnTail_StillGetsTheRepairPromptAppended()
    {
        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreateContractWithInvalidValueOverride(OverrideWithOwnTailText),
            "de",
            PolitenessLevel.Formal,
            CreateViolation(message: null, remediation: null),
            fieldLabel: null,
            understoodValue: null,
            includesValue: false);

        Assert.Equal($"{OverrideWithOwnTailText} {RepairPromptFormal}", result);
        Assert.False(result.EndsWith(OverrideWithOwnTailText, StringComparison.Ordinal));
    }

    [Fact] // Eine Überschreibung nur aus Leerzeichen wird nicht verwendet; der eingebettete Ersatztext erhält keinen zusätzlichen allgemeinen Hinweis.
    public void WhitespaceOnlyContractOverride_FallsBackToTheEmbeddedText_WithoutRepairPromptAppend()
    {
        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreateContractWithInvalidValueOverride("   "),
            "de",
            PolitenessLevel.Formal,
            CreateViolation(message: null, remediation: null),
            fieldLabel: null,
            understoodValue: null,
            includesValue: false);

        Assert.Equal(EmbeddedFallbackFormal, result);
        Assert.DoesNotContain(RepairPromptFormal, result, StringComparison.Ordinal);
    }

    [Fact] // Der eingebettete Ersatztext berücksichtigt die informelle Anrede.
    public void EmbeddedFallback_InformalRegister_ResolvesTheInformalEmbeddedText()
    {
        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreateContractWithoutOverride(),
            "de",
            PolitenessLevel.Informal,
            CreateViolation(message: null, remediation: null),
            fieldLabel: null,
            understoodValue: null,
            includesValue: false);

        Assert.Equal(EmbeddedFallbackInformal, result);
        Assert.DoesNotContain(RepairPromptInformal, result, StringComparison.Ordinal);
    }

    [Fact] // Auch der angehängte Korrekturhinweis berücksichtigt die Anrede.
    public void AppendedRepairPrompt_InformalRegister_ResolvesTheInformalRepairPrompt()
    {
        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreateContractWithoutOverride(),
            "de",
            PolitenessLevel.Informal,
            CreateViolation(ModeledBareVerdictText, remediation: null),
            fieldLabel: null,
            understoodValue: null,
            includesValue: false);

        Assert.Equal($"{ModeledBareVerdictText} {RepairPromptInformal}", result);
    }

    private static string? ModeledMessageFor(string sourceKind, string? blankModeledMessage)
        => sourceKind switch
        {
            SourceModeledBareVerdict => ModeledBareVerdictText,
            SourceModeledWithContent => ModeledContentText,
            _ => blankModeledMessage
        };

    private static string ExpectedUtterance(string? fieldLabel, string sourceKind, string? remediation)
    {
        var message = sourceKind switch
        {
            SourceModeledBareVerdict => ModeledBareVerdictText,
            SourceModeledWithContent => ModeledContentText,
            SourceTailLessContractOverride => TailLessOverrideText,
            SourceContractOverrideWithOwnTail => OverrideWithOwnTailText,
            _ => EmbeddedFallbackFormal
        };

        var tail = !string.IsNullOrWhiteSpace(remediation)
            ? remediation
            : sourceKind == SourceEmbeddedCatalogFallback
                ? null // Der eingebettete Text enthält bereits eine Handlungsaufforderung.
                : RepairPromptFormal;

        var segments = new List<string>();
        if (!string.IsNullOrWhiteSpace(fieldLabel))
        {
            segments.Add($"Es gibt ein Problem mit {fieldLabel.Trim()}.");
        }

        segments.Add(message);
        if (tail is not null)
        {
            segments.Add(tail);
        }

        return string.Join(" ", segments);
    }

    private static FormConstraintViolation CreateViolation(string? message, string? remediation)
        => new(
            "field1",
            "constraint1",
            SeverityLevels.Error,
            ConstraintKinds.Required,
            message!,
            remediation,
            ConstraintScopes.Field,
            null);

    private static FormContract CreateContractFor(string sourceKind)
        => sourceKind switch
        {
            SourceTailLessContractOverride => CreateContractWithInvalidValueOverride(TailLessOverrideText),
            SourceContractOverrideWithOwnTail => CreateContractWithInvalidValueOverride(OverrideWithOwnTailText),
            _ => CreateContractWithoutOverride()
        };

    private static FormContract CreateContractWithInvalidValueOverride(string overrideText)
        => CreateContractWithPrompt(FormDialogPromptIds.ValidationInvalidValue, overrideText);

    private static FormContract CreateContractWithoutOverride()
        => CreateContractWithPrompt(FormDialogPromptIds.OptionsNone, "Keine Optionen.");

    private static FormContract CreateContractWithPrompt(string promptId, string deText)
    {
        return new FormContract
        {
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    Hints = new VoiceHintsDefinition
                    {
                        Prompts =
                        [
                            new VoicePrompt
                            {
                                PromptId = promptId,
                                Text = new LocalizedText
                                {
                                    ["de"] = deText
                                }
                            }
                        ]
                    }
                }
            }
        };
    }

    private static string Describe(string? value)
        => value switch
        {
            null => "<null>",
            "" => "<empty>",
            _ when string.IsNullOrWhiteSpace(value) => "<whitespace>",
            _ => $"\"{value}\""
        };
}
