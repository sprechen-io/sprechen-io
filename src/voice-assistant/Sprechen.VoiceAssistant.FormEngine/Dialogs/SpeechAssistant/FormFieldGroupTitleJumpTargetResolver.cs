using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet einen lokalisierten Gruppentitel der ersten passenden verfügbaren Feldgruppe zu.
/// </summary>
internal sealed class FormFieldGroupTitleJumpTargetResolver
{
    private readonly ImmutableArray<string> orderedFieldGroupIds;
    private readonly FormLocalizationResolver localizationResolver;
    private readonly FormJumpTargetAvailability availability;
    private readonly FormJumpTargetTextMatcher textMatcher;

    public FormFieldGroupTitleJumpTargetResolver(
        ImmutableArray<string> orderedFieldGroupIds,
        FormLocalizationResolver localizationResolver,
        FormJumpTargetAvailability availability,
        FormJumpTargetTextMatcher textMatcher)
    {
        this.orderedFieldGroupIds = orderedFieldGroupIds;
        this.localizationResolver = localizationResolver ?? throw new ArgumentNullException(nameof(localizationResolver));
        this.availability = availability ?? throw new ArgumentNullException(nameof(availability));
        this.textMatcher = textMatcher ?? throw new ArgumentNullException(nameof(textMatcher));
    }

    public bool TryResolve(string normalizedTarget, out FormJumpTargetResolution? resolution)
    {
        foreach (var fieldGroupId in orderedFieldGroupIds)
        {
            if (!availability.IsNavigableFieldGroup(fieldGroupId))
            {
                continue;
            }

            var title = localizationResolver.ResolveFieldGroupTitle(fieldGroupId);
            if (textMatcher.Matches(title, normalizedTarget))
            {
                resolution = new FormJumpTargetResolution(FormJumpTargetKind.FieldGroup, fieldGroupId);
                return true;
            }
        }

        resolution = null;
        return false;
    }
}
