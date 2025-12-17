using OrchardCore.Workflows.Display;
using OrchardCore.Workflows.Models;

namespace OrchardCore.Workflows.Timers
{
    public class TimerEventDisplayDriver : ActivityDisplayDriver<TimerEvent, TimerEventViewModel>
    {
        protected override void EditActivity(TimerEvent source, TimerEventViewModel model)
        {
            model.CronExpression = source.CronExpression;
            model.UseSiteTimeZone = source.UseSiteTimeZone;
            model.UseScriptCondition = source.UseScriptCondition;
            model.ScriptCondition = source.ScriptCondition.Expression;
        }

        protected override void UpdateActivity(TimerEventViewModel model, TimerEvent target)
        {
            target.CronExpression = model.CronExpression.Trim();
            target.UseSiteTimeZone = model.UseSiteTimeZone;
            target.UseScriptCondition = model.UseScriptCondition;
            target.ScriptCondition = new WorkflowExpression<bool>(model.ScriptCondition);
        }
    }
}
