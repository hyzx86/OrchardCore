using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrchardCore.Workflows.Models;

namespace OrchardCore.Workflows.Services
{
    public class WorkflowTitleGenerator : IWorkflowTitleGenerator
    {
        private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
        private readonly IWorkflowScriptEvaluator _scriptEvaluator;
        private readonly ILogger _logger;

        public WorkflowTitleGenerator(
            IWorkflowExpressionEvaluator expressionEvaluator,
            IWorkflowScriptEvaluator scriptEvaluator,
            ILogger<WorkflowTitleGenerator> logger)
        {
            _expressionEvaluator = expressionEvaluator;
            _scriptEvaluator = scriptEvaluator;
            _logger = logger;
        }

        public async Task<string> GenerateTitleAsync(WorkflowExecutionContext workflowContext)
        {
            ArgumentNullException.ThrowIfNull(workflowContext);

            var workflow = workflowContext.Workflow;
            var workflowType = workflowContext.WorkflowType;
            var fallbackTitle = workflow.WorkflowId;
            var expression = workflowType?.TitleExpression?.Trim();

            if (string.IsNullOrWhiteSpace(expression))
            {
                return fallbackTitle;
            }

            try
            {
                var syntax = NormalizeSyntax(workflowType.TitleExpressionSyntax);

                var title = syntax switch
                {
                    WorkflowExpressionSyntaxNames.JavaScript => await EvaluateJavaScriptAsync(expression, workflowContext),
                    WorkflowExpressionSyntaxNames.Liquid => await _expressionEvaluator.EvaluateAsync(new WorkflowExpression<string>(expression), workflowContext, HtmlEncoder.Default),
                    _ => throw new InvalidOperationException($"Unsupported workflow title expression syntax '{workflowType.TitleExpressionSyntax}'.")
                };

                return string.IsNullOrWhiteSpace(title) ? fallbackTitle : title.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to generate title for workflow '{WorkflowId}' from workflow type '{WorkflowTypeId}'. Falling back to workflow ID.",
                    workflow.WorkflowId,
                    workflowType?.WorkflowTypeId);

                return fallbackTitle;
            }
        }

        private async Task<string> EvaluateJavaScriptAsync(string expression, WorkflowExecutionContext workflowContext)
        {
            var result = await _scriptEvaluator.EvaluateAsync<object>(new WorkflowExpression<object>(expression), workflowContext);
            return result?.ToString();
        }

        private static string NormalizeSyntax(string syntax)
        {
            if (string.IsNullOrWhiteSpace(syntax))
            {
                return WorkflowExpressionSyntaxNames.Liquid;
            }

            if (string.Equals(syntax, WorkflowExpressionSyntaxNames.JavaScript, StringComparison.OrdinalIgnoreCase))
            {
                return WorkflowExpressionSyntaxNames.JavaScript;
            }

            if (string.Equals(syntax, WorkflowExpressionSyntaxNames.Liquid, StringComparison.OrdinalIgnoreCase))
            {
                return WorkflowExpressionSyntaxNames.Liquid;
            }

            return syntax;
        }
    }
}
