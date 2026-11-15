namespace Sprechen.Forms.Pages;

/// <summary>
/// Übermittelt Studienparameter, Auswahl und geladenes Formular an das Seitenmodell.
/// Bei einem Katalogladefehler bleiben die Studienparameter zusammen mit dem Fehlercode erhalten.
/// </summary>
internal sealed class IndexPageWorkflowResult
{
    public IndexPageStudyParameters StudyParameters { get; init; } =
        new(string.Empty, string.Empty, "A");

    public IndexPageSelectionResult? Selection { get; init; }

    public IndexPageSelectedFormResult? LoadedForm { get; init; }

    public string? ErrorCode { get; init; }

    public static IndexPageWorkflowResult CatalogLoadFailed(
        IndexPageStudyParameters studyParameters,
        FormsCatalogLoadResult catalogLoadResult)
    {
        ArgumentNullException.ThrowIfNull(studyParameters);
        ArgumentNullException.ThrowIfNull(catalogLoadResult);

        return new IndexPageWorkflowResult
        {
            StudyParameters = studyParameters,
            ErrorCode = catalogLoadResult.ErrorCode
        };
    }
}
