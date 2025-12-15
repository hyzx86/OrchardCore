using System;

namespace OrchardCore.Workflows.Indexes
{
    public class WorkflowTypeVersionAudit
    {
        public string WorkflowTypeVersionId { get; set; }
        public bool Latest { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedUtc { get; set; }
        public string ModifiedBy { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }
}
