using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyOC.Scripting;
using EasyOC.Scripting.Services;
using Microsoft.Extensions.Localization;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Scripting;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Workflows.Activities
{
    public class ScriptTask : TaskActivity<ScriptTask>
    {
        private readonly IWorkflowScriptEvaluator _scriptEvaluator;
        private readonly IScriptExecutionManager _scriptExecutionManager;
        protected readonly IStringLocalizer S;

        public ScriptTask(
            IWorkflowScriptEvaluator scriptEvaluator,
            IScriptExecutionManager scriptExecutionManager,
            IStringLocalizer<ScriptTask> localizer)
        {
            _scriptEvaluator = scriptEvaluator;
            _scriptExecutionManager = scriptExecutionManager;
            S = localizer;
        }

        public override LocalizedString DisplayText => S["Script Task"];

        public override LocalizedString Category => S["Control Flow"];

        public IList<string> AvailableOutcomes
        {
            get => GetProperty(() => new List<string> { "Done" });
            set => SetProperty(value);
        }

        /// <summary>
        /// The script can call any available functions, including setOutcome().
        /// </summary>
        public WorkflowExpression<object> Script
        {
            get => GetProperty(() => new WorkflowExpression<object>("setOutcome('Done');"));
            set => SetProperty(value);
        }

        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes(AvailableOutcomes.Select(x => S[x]).ToArray());
        }

        public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            var outcomes = new List<string>();
            var currentExecution = _scriptExecutionManager.GetCurrentExecution();
            var workflowName = workflowContext.WorkflowType?.Name ?? workflowContext.WorkflowType?.WorkflowTypeId ?? "Workflow";
            var activityName = activityContext.ActivityRecord.Properties["ActivityMetadata"]?["Title"]?.ToString();
            activityName = string.IsNullOrWhiteSpace(activityName)
                ? activityContext.ActivityRecord.Name
                : activityName;
            var sessionId = currentExecution?.SessionId ?? workflowContext.WorkflowId;

            var executionParameters = new Dictionary<string, object>
            {
                ["workflowName"] = workflowName,
                ["workflowTypeId"] = workflowContext.WorkflowType?.WorkflowTypeId,
                ["workflowId"] = workflowContext.WorkflowId,
                ["activityId"] = activityContext.ActivityRecord.ActivityId,
                ["activityName"] = activityName,
            };
            executionParameters.SetExecutionMetadata(
                "WorkflowScriptTask",
                $"{workflowName} / {activityName}",
                sessionId
            );

            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                _scriptExecutionManager.GetCurrentCancellationToken()
            );
            using var executionScope = _scriptExecutionManager.Register(
                $"{workflowName}:{activityName}",
                executionParameters,
                cancellationTokenSource,
                isDebugMode: false
            );

            workflowContext.LastResult = await _scriptEvaluator.EvaluateAsync(Script, workflowContext, new OutcomeMethodProvider(outcomes));

            return Outcomes(outcomes);
        }
    }
}
