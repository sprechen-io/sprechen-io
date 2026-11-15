namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Schlüssel aus Kennung und Version eines Profils zur Anwendungsauswahl.
/// Der eigene Typ verhindert die versehentliche Übergabe anderer Artefaktschlüssel.
/// </summary>
public readonly record struct ApplicationSelectionDialogProfileKey(string ProfileId, string ProfileVersion);
