using System;

namespace OrchardCore.Workflows.Models
{
    public class WorkflowExecutionLogEntry
    {
        public string ActivityName { get; set; }
        public DateTime? EnteredUtc { get; set; }
        public string ExecutedBy { get; set; }
        public string ErrorMessage { get; set; }
        public string ExceptionDetails { get; set; }
    }
}
