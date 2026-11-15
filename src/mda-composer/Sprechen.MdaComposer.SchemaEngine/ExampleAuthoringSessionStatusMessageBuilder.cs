namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Erzeugt Statusmeldungen aus Dokumentquelle, Validierung und Reparaturvorschlägen für die Oberfläche.
/// </summary>
internal sealed class ExampleAuthoringSessionStatusMessageBuilder
{
    public string Build(
        ExampleSessionDocumentSource documentSource,
        CanonicalValidationResult? canonicalValidation,
        IReadOnlyList<ExampleRepairAction> repairActions,
        bool canonicalValidationAvailable)
    {
        if (!canonicalValidationAvailable)
        {
            return BuildUncheckedMessage(documentSource, repairActions);
        }

        if (documentSource == ExampleSessionDocumentSource.SchemaDefault)
        {
            return BuildSchemaDraftMessage(canonicalValidation, repairActions);
        }

        return BuildLoadedDocumentMessage(canonicalValidation, repairActions);
    }

    private static string BuildUncheckedMessage(
        ExampleSessionDocumentSource documentSource,
        IReadOnlyList<ExampleRepairAction> repairActions)
    {
        var repairSuffix = BuildRepairSuffix(repairActions);
        return documentSource == ExampleSessionDocumentSource.SchemaDefault
            ? $"Entwurf aus dem Basisschema erzeugt. {repairSuffix}"
            : $"Dokument geladen, kanonische Prüfung noch nicht verfügbar. {repairSuffix}";
    }

    private static string BuildSchemaDraftMessage(
        CanonicalValidationResult? canonicalValidation,
        IReadOnlyList<ExampleRepairAction> repairActions)
    {
        return canonicalValidation?.IsValid is true
            ? "Entwurf aus dem Basisschema ist exportierbar."
            : $"Entwurf aus dem Basisschema ist noch nicht exportierbar. {BuildRepairSuffix(repairActions)}";
    }

    private static string BuildLoadedDocumentMessage(
        CanonicalValidationResult? canonicalValidation,
        IReadOnlyList<ExampleRepairAction> repairActions)
    {
        return canonicalValidation?.IsValid is true
            ? "Geladenes Dokument ist gegen das Basisschema valide."
            : $"Geladenes Dokument ist parsebar, aber gegen das Basisschema nicht valide. {BuildRepairSuffix(repairActions)}";
    }

    private static string BuildRepairSuffix(IReadOnlyList<ExampleRepairAction> repairActions)
    {
        return repairActions.Count switch
        {
            0 => "Keine automatische Reparatur verfügbar.",
            1 => "1 Reparaturaktion verfügbar.",
            _ => $"{repairActions.Count} Reparaturaktionen verfügbar.",
        };
    }
}
