namespace Sprechen.MdaComposer.Services;

/// <summary>
/// Verknüpft den Inhalt eines Referenzvertrags mit Dateiname und Schema, damit der Composer ihn passend laden kann.
/// </summary>
public sealed record ReferenceContract(string FileName, string SchemaType, string Json);
