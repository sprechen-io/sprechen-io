using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Leitet die Seitenladung an den Workflow und die Abfrage <c>?handler=forms</c> an den Forms-Endpunkt weiter.
/// </summary>
public sealed class IndexPageRuntime
{
    private readonly IndexPageFormsEndpoint _formsEndpoint;
    private readonly IndexPageWorkflow _workflow;

    internal IndexPageRuntime(IndexPageWorkflow workflow, IndexPageFormsEndpoint formsEndpoint)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(formsEndpoint);

        _workflow = workflow;
        _formsEndpoint = formsEndpoint;
    }

    internal Task<IndexPageWorkflowResult> LoadAsync(
        IndexPageWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        return _workflow.LoadAsync(request, cancellationToken);
    }

    internal Task<IActionResult> GetFormsAsync(
        HttpRequest request,
        string? applicationId,
        string? language,
        CancellationToken cancellationToken)
    {
        return _formsEndpoint.GetFormsAsync(request, applicationId, language, cancellationToken);
    }
}
