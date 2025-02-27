using System.Text.Encodings.Web;
using Microsoft.Extensions.Localization;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Email.Workflows.Activities;

public class EmailTask : TaskActivity<EmailTask>
{
    private readonly IEmailService _emailService;
    private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
    protected readonly IStringLocalizer S;
    private readonly HtmlEncoder _htmlEncoder;

    public EmailTask(
        IEmailService emailService,
        IWorkflowExpressionEvaluator expressionEvaluator,
        IStringLocalizer<EmailTask> localizer,
        HtmlEncoder htmlEncoder
    )
    {
        _emailService = emailService;
        _expressionEvaluator = expressionEvaluator;
        S = localizer;
        _htmlEncoder = htmlEncoder;
    }

    public override LocalizedString DisplayText => S["Email Task"];

    public override LocalizedString Category => S["Messaging"];

    public WorkflowExpression<string> Author
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> Sender
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> ReplyTo
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    // TODO: Add support for the following format: Jack Bauer<jack@ctu.com>, ...
    public WorkflowExpression<string> Recipients
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> Cc
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> Bcc
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    public WorkflowExpression<string> Subject
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    [Obsolete("This property is deprecated, please use TextBody & HtmlBody instead.")]
    public WorkflowExpression<string> Body
    {
        get => GetProperty(() => new WorkflowExpression<string>());
        set => SetProperty(value);
    }

    [Obsolete("This property is deprecated, please use TextBody & HtmlBody instead.")]
    public bool IsHtmlBody
    {
        get => GetProperty(() => true);
        set => SetProperty(value);
    }

    public MailMessageBodyFormat BodyFormat
    {
        get => GetProperty(() => MailMessageBodyFormat.All);
        set => SetProperty(value);
    }

    public WorkflowExpression<string> TextBody
    {
        get
        {
            var textBody = GetProperty<WorkflowExpression<string>>();

            if (textBody == null && !GetProperty(() => true, "IsHtmlBody"))
            {
                textBody = GetProperty(() => new WorkflowExpression<string>(), "Body");
            }

            return textBody ?? new WorkflowExpression<string>();
        }
        set => SetProperty(value);
    }

    public WorkflowExpression<string> HtmlBody
    {
        get
        {
            var htmlBody = GetProperty<WorkflowExpression<string>>();

            if (htmlBody == null && GetProperty(() => true, "IsHtmlBody"))
            {
                htmlBody = GetProperty(() => new WorkflowExpression<string>(), "Body");
            }

            return htmlBody ?? new WorkflowExpression<string>();
        }
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
    {
        return Outcomes(S["Done"], S["Failed"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
    {
        var author = await _expressionEvaluator.EvaluateAsync(Author, workflowContext, null);
        var sender = await _expressionEvaluator.EvaluateAsync(Sender, workflowContext, null);
        var replyTo = await _expressionEvaluator.EvaluateAsync(ReplyTo, workflowContext, null);
        var recipients = await _expressionEvaluator.EvaluateAsync(Recipients, workflowContext, null);
        var cc = await _expressionEvaluator.EvaluateAsync(Cc, workflowContext, null);
        var bcc = await _expressionEvaluator.EvaluateAsync(Bcc, workflowContext, null);
        var subject = await _expressionEvaluator.EvaluateAsync(Subject, workflowContext, null);
        var textBody = await _expressionEvaluator.EvaluateAsync(TextBody, workflowContext, null);
        var htmlBody = await _expressionEvaluator.EvaluateAsync(HtmlBody, workflowContext, _htmlEncoder);

        var message = new MailMessage
        {
            // Author and Sender are both not required fields.
            From = author?.Trim() ?? sender?.Trim(),
            To = recipients?.Trim(),
            Cc = cc?.Trim(),
            Bcc = bcc?.Trim(),
            // Email reply-to header https://tools.ietf.org/html/rfc4021#section-2.1.4
            ReplyTo = replyTo?.Trim(),
            Subject = subject?.Trim(),
        };

        switch (BodyFormat)
        {
            case MailMessageBodyFormat.All:
                message.HtmlBody = htmlBody?.Trim();
                message.TextBody = textBody?.Trim();
                break;
            case MailMessageBodyFormat.Text:
                message.TextBody = textBody?.Trim();
                break;
            case MailMessageBodyFormat.Html:
                message.HtmlBody = htmlBody?.Trim();
                break;
            default:
                break;
        }

        if (!string.IsNullOrWhiteSpace(sender))
        {
            message.Sender = sender.Trim();
        }

        var result = await _emailService.SendAsync(message);
        workflowContext.LastResult = result;

        if (!result.Succeeded)
        {
            return Outcomes("Failed");
        }

        return Outcomes("Done");
    }
}
