namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Bezeichnet die zuständige Prüfung eines Schemaknotens für die Auswahl des passenden Validators.
/// </summary>
internal enum SchemaNodeValidationRoute
{
    Object,
    Map,
    Array,
    String,
    Integer,
    Number,
    Boolean,
    Union,
    Ignored
}
