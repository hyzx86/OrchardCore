using System;
using System.Linq;
using OrchardCore.Workflows.Models;
using YesSql.Indexes;
using OrchardCore.Entities;

namespace OrchardCore.Workflows.Indexes
{
    public class WorkflowTypeIndex : MapIndex
    {
        public long DocumentId { get; set; }
        public string WorkflowTypeId { get; set; }
        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public bool HasStart { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string WorkflowTypeVersionId { get; set; }
        public bool Latest { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedUtc { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class WorkflowTypeStartActivitiesIndex : MapIndex
    {
        public string WorkflowTypeId { get; set; }
        public string WorkflowTypeVersionId { get; set; }
        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string StartActivityId { get; set; }
        public string StartActivityName { get; set; }
    }

    public class WorkflowTypeIndexProvider : IndexProvider<WorkflowType>
    {
        public override void Describe(DescribeContext<WorkflowType> context)
        {
            context.For<WorkflowTypeIndex>()
                .Map(workflowType =>
                {
                    var workflowTypeAudit = workflowType.As<WorkflowTypeVersionAudit>();
                    var index = new WorkflowTypeIndex
                    {
                        WorkflowTypeId = workflowType.WorkflowTypeId,
                        Name = workflowType.Name,
                        IsEnabled = workflowType.IsEnabled,
                        HasStart = workflowType.Activities.Any(x => x.IsStart),
                        DisplayName = workflowTypeAudit.DisplayName,
                        Description = workflowTypeAudit.Description,
                        WorkflowTypeVersionId = workflowTypeAudit.WorkflowTypeVersionId,
                        Latest = workflowTypeAudit.Latest,
                        CreatedUtc = workflowTypeAudit.CreatedUtc,
                        ModifiedUtc = workflowTypeAudit.ModifiedUtc,
                        ModifiedBy = workflowTypeAudit.ModifiedBy,
                        CreatedBy = workflowTypeAudit.CreatedBy
                    };
                    return index;
                }
                );

            context.For<WorkflowTypeStartActivitiesIndex>()
                .Map(workflowType =>
                {
                    var workflowTypeAudit = workflowType.As<WorkflowTypeVersionAudit>();

                    var startIndexies = workflowType.Activities.Where(x => x.IsStart).Select(x =>
                        new WorkflowTypeStartActivitiesIndex
                        {
                            WorkflowTypeVersionId = workflowTypeAudit.WorkflowTypeVersionId,
                            WorkflowTypeId = workflowType.WorkflowTypeId,
                            Name = workflowType.Name,
                            IsEnabled = workflowType.IsEnabled,
                            StartActivityId = x.ActivityId,
                            StartActivityName = x.Name
                        });
                    return startIndexies;
                }
                );
        }
    }
}
