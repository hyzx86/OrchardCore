using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.Workflows.Models;

namespace OrchardCore.Workflows.Services
{
    public interface IWorkflowExecutionLogStore
    {
        Task<IReadOnlyList<WorkflowExecutionLogEntry>> ListAsync(Workflow workflow);
    }
}
