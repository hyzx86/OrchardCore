using System.Threading.Tasks;
using OrchardCore.Workflows.Models;

namespace OrchardCore.Workflows.Services
{
    public interface IWorkflowTitleGenerator
    {
        Task<string> GenerateTitleAsync(WorkflowExecutionContext workflowContext);
    }
}
