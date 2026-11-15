using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die dritte Hilfestufe: Sie kündigt entweder die unmittelbar folgende Feldhilfe an
/// oder nennt deren aktivierten Aufruf. Die ersten beiden Stufen kündigen die Hilfe nicht als unmittelbar folgend an.
/// </summary>
public sealed class UnrecognizedInputThirdRungTests
{
    [Fact]
    public void ThirdRung_WhenAutoHelpFollows_AnnouncesTheHelpInsteadOfInviting()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = SpellingWorkflowTestFactory.CreateContext("de");
        var field = SpellingWorkflowTestFactory.CreateField();

        Select(policy, context, field, isAutoHelpDue: false);
        Select(policy, context, field, isAutoHelpDue: false);
        var third = Select(policy, context, field, isAutoHelpDue: true);

        Assert.Contains("Hilfe zu diesem Feld", third, StringComparison.Ordinal);
        Assert.DoesNotContain("Okay Alex", third, StringComparison.Ordinal);
    }

    [Fact]
    public void ThirdRung_WhenNoHelpFollows_InvitesWithTheAnchoredForm()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = SpellingWorkflowTestFactory.CreateContext("de");
        var field = SpellingWorkflowTestFactory.CreateField();

        Select(policy, context, field, isAutoHelpDue: false);
        Select(policy, context, field, isAutoHelpDue: false);
        var third = Select(policy, context, field, isAutoHelpDue: false);

        Assert.Contains("Okay Alex Hilfe", third, StringComparison.Ordinal);
        Assert.DoesNotContain("Hilfe zu diesem Feld", third, StringComparison.Ordinal);
    }

    [Fact]
    public void EarlierRungs_IgnoreThePreview()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = SpellingWorkflowTestFactory.CreateContext("de");
        var field = SpellingWorkflowTestFactory.CreateField();

        var first = Select(policy, context, field, isAutoHelpDue: true);
        var second = Select(policy, context, field, isAutoHelpDue: true);

        Assert.DoesNotContain("Hilfe zu diesem Feld", first, StringComparison.Ordinal);
        Assert.DoesNotContain("Hilfe zu diesem Feld", second, StringComparison.Ordinal);
        Assert.Contains("Okay Alex Hilfe", second, StringComparison.Ordinal);
    }

    private static string Select(
        FormVoiceCommandUnrecognizedInputEscalationPolicy policy,
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        bool isAutoHelpDue)
        => policy.SelectUnrecognizedInputPrompt(
            context,
            field,
            "xyz",
            FormRepairReason.UnparsedUtterance,
            offerConstructiveFallback: false,
            isPrimaryFieldValueEntry: true,
            isAutoHelpDue: isAutoHelpDue);
}
