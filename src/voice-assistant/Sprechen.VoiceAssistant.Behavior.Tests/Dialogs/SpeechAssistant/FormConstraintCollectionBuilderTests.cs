using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

public sealed class FormConstraintCollectionBuilderTests
{
    [Fact]
    public void Build_Throws_WhenVariableInvariantConstraintIsNotObject()
    {
        using var document = JsonDocument.Parse("[]");
        var contract = CreateContractWithInvariantConstraint(document.RootElement.Clone());

        Assert.Throws<InvalidOperationException>(() => FormConstraintCollectionBuilder.Build(contract));
    }

    [Fact]
    public void Build_Throws_WhenVariableInvariantConstraintHasMalformedTarget()
    {
        using var document = JsonDocument.Parse("""{"constraintId":"c1","target":1,"kind":"required"}""");
        var contract = CreateContractWithInvariantConstraint(document.RootElement.Clone());

        Assert.Throws<InvalidOperationException>(() => FormConstraintCollectionBuilder.Build(contract));
    }

    private static FormContract CreateContractWithInvariantConstraint(JsonElement invariantConstraint)
    {
        return new FormContract
        {
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("v1"),
                    InvariantConstraints = [invariantConstraint]
                }
            ]
        };
    }
}
