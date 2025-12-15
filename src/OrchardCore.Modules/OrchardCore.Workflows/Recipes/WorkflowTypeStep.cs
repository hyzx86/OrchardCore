using System;
using System.Linq;
using System.Threading.Tasks;
using EasyOC.Core.Shared.Workflows;
using Newtonsoft.Json.Linq;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using OrchardCore.Workflows.Models;

namespace OrchardCore.Workflows.Recipes
{
    public class WorkflowTypeStep : IRecipeStepHandler
    {
        private readonly IVersioningWorkflowTypeStore _workflowTypeStore;

        public WorkflowTypeStep(IVersioningWorkflowTypeStore workflowTypeStore)
        {
            _workflowTypeStore = workflowTypeStore;
        }

        public async Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!string.Equals(context.Name, "WorkflowType", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var model = context.Step.ToObject<WorkflowStepModel>();

            foreach (var token in model.Data.Cast<JObject>())
            {
                var workflow = token.ToObject<WorkflowType>();

                var existing = await _workflowTypeStore.GetAsync(workflow.WorkflowTypeId);

                workflow.Id = 0;
                if (existing == null)
                {
                    await _workflowTypeStore.SaveAsync(workflow);
                }
                else
                {
                    await _workflowTypeStore.SaveAsync(workflow, true);
                }

            }
        }
    }

    public class WorkflowStepModel
    {
        public JArray Data { get; set; }
    }
}
