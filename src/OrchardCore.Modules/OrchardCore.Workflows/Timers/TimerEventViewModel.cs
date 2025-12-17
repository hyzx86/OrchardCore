using System.ComponentModel.DataAnnotations;

namespace OrchardCore.Workflows.Timers
{
    public class TimerEventViewModel
    {
        [Required]
        public string CronExpression { get; set; }

        [Required]
        public bool UseSiteTimeZone { get; set; }
        public bool UseScriptCondition { get; set; }
        public string ScriptCondition { get; set; }
    }
}
