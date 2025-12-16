using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using NCrontab;
using OrchardCore.Modules;
using OrchardCore.Settings;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Workflows.Timers
{
    public class TimerEvent : EventActivity
    {
        public static string EventName => "TimerEvent";
        private readonly IClock _clock;
        protected readonly IStringLocalizer S;
        private readonly ISiteService _siteService;
        private readonly IWorkflowScriptEvaluator _scriptEvaluator;


        public TimerEvent(IClock clock, IStringLocalizer<TimerEvent> localizer, ISiteService siteService, IWorkflowScriptEvaluator scriptEvaluator)
        {
            _clock = clock;
            S = localizer;
            _siteService = siteService;
            _scriptEvaluator = scriptEvaluator;
        }

        public override string Name => EventName;

        public override LocalizedString DisplayText => S["Timer Event"];

        public override LocalizedString Category => S["Background"];

        public string CronExpression
        {
            get => GetProperty(() => "*/5 * * * *");
            set => SetProperty(value);
        }
        public bool UseScriptCondition
        {
            get => GetProperty(() => false);
            set => SetProperty(value);
        }
        public WorkflowExpression<bool> ScriptCondition
        {
            get => GetProperty(() => new WorkflowExpression<bool>(GetDefaultValue()));
            set => SetProperty(value);
        }
        public bool UseSiteTimeZone
        {
            get => GetProperty(() => false);
            set => SetProperty(value);
        }

        private DateTime? StartedTime
        {
            get => GetProperty<DateTime?>();
            set => SetProperty(value);
        }

        public override async Task<bool> CanExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return StartedTime == null || await IsExpired(workflowContext);
        }

        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes(S["Done"]);
        }

        public override async Task<ActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            if (await IsExpired(workflowContext))
            {
                workflowContext.LastResult = "TimerEvent";
                return Outcomes("Done");
            }

            return Halt();
        }

        private async Task<bool> IsExpired(WorkflowExecutionContext workflowContext)
        {
            DateTime when, now;
            if (UseSiteTimeZone)
            {
                var timeZoneId = (await _siteService.GetSiteSettingsAsync()).TimeZoneId;
                now = _clock.ConvertToTimeZone(new DateTimeOffset(_clock.UtcNow), _clock.GetTimeZone(timeZoneId)).DateTime;
            }
            else
            {
                now = _clock.UtcNow;
            }
            StartedTime ??= now;
            var expired = false;
            foreach (var CronExpressionItem in CronExpression.Split("\n").Select(x => x.Trim()))
            {
                var schedule = CrontabSchedule.Parse(CronExpressionItem);
                when = schedule.GetNextOccurrence(StartedTime.Value);
                expired = now >= when;
                if (expired)
                {
                    break;
                }
            }



            if (expired && UseScriptCondition && workflowContext is not null)
            {
                //如果表达式为空，则始终返回false
                if (string.IsNullOrEmpty(ScriptCondition.Expression))
                {
                    return false;
                }
                var result = await _scriptEvaluator.EvaluateAsync(ScriptCondition, workflowContext);
                return result;
            }
            return expired;
        }


        private string GetDefaultValue()
        {
            var sample = $@"
//sample
return true;
            ";
            return sample;
        }
    }
}
