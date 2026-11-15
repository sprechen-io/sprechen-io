namespace Sprechen.Speech.State;

/// <summary>
/// Bündelt die beim Abbaubeginn aufgenommenen Dialogmodule mit der
/// damals aktiven Modulreferenz für die anschließende Freigabe.
/// </summary>
internal readonly record struct DialogHostTeardownSnapshot<TEvent>(
    IReadOnlyList<IDialogFlow<TEvent>> Modules,
    IDialogFlow<TEvent>? ActiveModule) where TEvent : notnull
{
    internal static DialogHostTeardownSnapshot<TEvent> Empty { get; } = new(
        Array.Empty<IDialogFlow<TEvent>>(),
        ActiveModule: null);
}
