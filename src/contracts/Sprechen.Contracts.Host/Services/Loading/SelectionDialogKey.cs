namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Kennzeichnet Auswahldialoge durch Kennung und Modellversion, damit ihre Schlüssel nicht mit anderen Artefaktarten verwechselt werden.
/// </summary>
internal readonly record struct SelectionDialogKey(string SelectionDialogId, string ModelVersion);
