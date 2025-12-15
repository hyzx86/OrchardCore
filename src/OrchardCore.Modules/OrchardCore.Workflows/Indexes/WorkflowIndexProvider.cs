using System;
using System.Linq;
using OrchardCore.Workflows.Models;
using YesSql.Indexes;
using EasyOC.Core.Shared.Workflows;

namespace OrchardCore.Workflows.Indexes
{
    public class WorkflowIndex : MapIndex
    {
        public long DocumentId { get; set; }
        public string WorkflowTypeId { get; set; }
        public string WorkflowId { get; set; }
        public int WorkflowStatus { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string WorkflowTypeVersionId { get; set; }
        public DateTime? LastExecutedOnUtc { get; set; }
        public string StatusName { get; set; }
    }

    public class WorkflowBlockingActivitiesIndex : MapIndex
    {
        public string ActivityId { get; set; }
        public string ActivityName { get; set; }
        public bool ActivityIsStart { get; set; }
        public string WorkflowTypeId { get; set; }
        public string WorkflowId { get; set; }
        public string WorkflowTypeVersionId { get; set; }
        public string WorkflowCorrelationId { get; set; }
    }

    public class WorkflowIndexProvider : IndexProvider<Workflow>
    {
        public override void Describe(DescribeContext<Workflow> context)
        {
            context.For<WorkflowIndex>()
                .Map(workflow =>
                    {
                        var state = workflow.GetWorkflowState();

                        var index = new WorkflowIndex
                        {
                            WorkflowTypeId = workflow.WorkflowTypeId,
                            WorkflowId = workflow.WorkflowId,
                            CreatedUtc = workflow.CreatedUtc,
                            WorkflowStatus = (int)workflow.Status,
                            WorkflowTypeVersionId = state.WorkflowTypeVersionId,
                            LastExecutedOnUtc = state.LastExecutedOn,
                            StatusName = workflow.Status.ToString()
                        };
                        return index;
                    }
                );

            context.For<WorkflowBlockingActivitiesIndex>()
                .Map(workflow =>
                     {
                         var state = workflow.GetWorkflowState();
                         var blockIndex = workflow.BlockingActivities.Select(x =>
                               new WorkflowBlockingActivitiesIndex
                               {
                                   ActivityId = x.ActivityId,
                                   ActivityName = x.Name,
                                   ActivityIsStart = x.IsStart,
                                   WorkflowTypeId = workflow.WorkflowTypeId,
                                   WorkflowId = workflow.WorkflowId,
                                   WorkflowTypeVersionId = state.WorkflowTypeVersionId,
                                   WorkflowCorrelationId = workflow.CorrelationId ?? ""
                               });
                         return blockIndex;
                     }
                );
        }
    }
}
