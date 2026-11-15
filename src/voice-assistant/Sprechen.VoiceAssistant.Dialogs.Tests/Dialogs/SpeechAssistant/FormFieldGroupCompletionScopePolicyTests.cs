namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormFieldGroupCompletionScopePolicyTests
{
    [Fact]
    public void BlocksCandidateFieldGroup_WhenScopeIsFieldGroup_OnlyBlocksMatchingFieldGroup()
    {
        Assert.True(FormFieldGroupCompletionScopePolicy.BlocksCandidateFieldGroup(
            ConstraintScopes.FieldGroup,
            "address",
            "address"));
        Assert.False(FormFieldGroupCompletionScopePolicy.BlocksCandidateFieldGroup(
            ConstraintScopes.FieldGroup,
            "personal",
            "address"));
    }

    [Fact]
    public void BlocksCandidateFieldGroup_WhenScopeIsCrossGroup_DoesNotBlockLocally()
    {
        // Eine feldgruppenübergreifende Regel wird keiner einzelnen abgeschlossenen Gruppe zugerechnet.
        // Die Sperre beim Absenden wird separat im FormVoiceCommandSubmissionService behandelt.
        Assert.False(FormFieldGroupCompletionScopePolicy.BlocksCandidateFieldGroup(
            ConstraintScopes.CrossGroup,
            "address",
            "address"));
        Assert.False(FormFieldGroupCompletionScopePolicy.BlocksCandidateFieldGroup(
            ConstraintScopes.CrossGroup,
            "personal",
            "address"));
    }

    [Theory]
    [InlineData("address", "address")]
    [InlineData("address", "personal")]
    [InlineData("contact", null)]
    public void BlocksCandidateFieldGroup_CrossGroupNeverInflatesGroupCounter(
        string candidateFieldGroupId,
        string? scopedFieldGroupId)
    {
        // Die angegebenen Gruppenpaare dürfen einen feldgruppenübergreifenden Fehler
        // nicht zur lokalen Abschlussbedingung machen.
        Assert.False(FormFieldGroupCompletionScopePolicy.BlocksCandidateFieldGroup(
            ConstraintScopes.CrossGroup,
            candidateFieldGroupId,
            scopedFieldGroupId));
    }

    [Fact]
    public void BlocksCandidateFieldGroup_WhenScopeIsForm_BlocksAllRelevantFieldGroups()
    {
        Assert.True(FormFieldGroupCompletionScopePolicy.BlocksCandidateFieldGroup(
            ConstraintScopes.Form,
            "address",
            null));
        Assert.True(FormFieldGroupCompletionScopePolicy.BlocksCandidateFieldGroup(
            ConstraintScopes.Form,
            "personal",
            "address"));
    }
}
