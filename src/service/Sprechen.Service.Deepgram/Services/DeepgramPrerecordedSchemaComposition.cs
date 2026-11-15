using Deepgram.Models.Listen.v1.REST;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// REST-Anfrageschema mit Zählern für die enthaltenen und angeforderten Erkennungsbegriffe.
/// </summary>
internal sealed record DeepgramPrerecordedSchemaComposition(
    PreRecordedSchema Schema,
    int KeytermCount,
    int ConfirmationKeytermCount,
    int ContextKeytermRequestedCount,
    int ContextKeytermAppliedCount)
{
    public bool KeytermParameterPresent => KeytermCount > 0;

    public bool ConfirmationKeytermsApplied => ConfirmationKeytermCount > 0;

    public bool ContextKeytermsRequested => ContextKeytermRequestedCount > 0;

    public bool ContextKeytermsApplied => ContextKeytermAppliedCount > 0;
}
