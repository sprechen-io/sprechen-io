namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Verbindet einen Kindknoten mit seinem übergeordneten Pfad für die rekursive Schemaauswertung.
/// </summary>
internal sealed record SchemaNodeChild(SchemaNode Node, string ParentPath);
