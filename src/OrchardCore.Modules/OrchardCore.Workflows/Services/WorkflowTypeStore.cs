using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EasyOC;
using EasyOC.Core.Shared.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OrchardCore.Modules;
using OrchardCore.Workflows.Indexes;
using OrchardCore.Workflows.Models;
using YesSql;

namespace OrchardCore.Workflows.Services
{
    public class WorkflowTypeStore : IVersioningWorkflowTypeStore
    {
        private readonly YesSql.ISession _session;
        private readonly IEnumerable<IWorkflowTypeEventHandler> _handlers;
        private readonly ILogger _logger;
        private readonly IClock _clock;
        private readonly IWorkflowTypeIdGenerator _idGenerator;
        private readonly IHttpContextAccessor _contextAccessor;

        public WorkflowTypeStore(YesSql.ISession session, IEnumerable<IWorkflowTypeEventHandler> handlers,
            ILogger<WorkflowTypeStore> logger, IClock clock, IWorkflowTypeIdGenerator idGenerator, IHttpContextAccessor contextAccessor)
        {
            _session = session;
            _handlers = handlers;
            _logger = logger;
            _clock = clock;
            _idGenerator = idGenerator;
            _contextAccessor = contextAccessor;
        }

        public Task<WorkflowType> GetByVersionAsync(string workflowTypeVersionId)
        {
            return _session.Query<WorkflowType, WorkflowTypeIndex>(x => x.Latest && x.WorkflowTypeVersionId == workflowTypeVersionId)
                .FirstOrDefaultAsync();
        }

        public Task<WorkflowType> GetAsync(long id)
        {
            return _session.GetAsync<WorkflowType>(id);
        }

        public Task<IEnumerable<WorkflowType>> GetAsync(IEnumerable<long> ids)
        {
            return _session.GetAsync<WorkflowType>(ids.ToArray());
        }

        public Task<WorkflowType> GetAsync(string workflowTypeId)
        {
            return _session.Query<WorkflowType, WorkflowTypeIndex>(x => x.Latest && x.WorkflowTypeId == workflowTypeId).FirstOrDefaultAsync();
        }

        public Task<WorkflowType> GetAsync(Workflow workflow)
        {
            var versionId = workflow.GetWorkflowState().WorkflowTypeVersionId;

            return _session.Query<WorkflowType, WorkflowTypeIndex>(x => x.Latest)
                .Where(x => x.WorkflowTypeId == workflow.WorkflowTypeId)
                .WhereIf(!string.IsNullOrEmpty(versionId), x => x.WorkflowTypeVersionId == versionId)
                .FirstOrDefaultAsync();
        }

        public Task<IEnumerable<WorkflowType>> ListAsync()
        {
            return _session.Query<WorkflowType, WorkflowTypeIndex>(x => x.Latest).ListAsync();
        }

        public Task<IEnumerable<WorkflowType>> GetByStartActivityAsync(string activityName)
        {
            return _session
                .Query<WorkflowType, WorkflowTypeStartActivitiesIndex>(index =>
                    index.StartActivityName == activityName &&
                    index.IsEnabled)
                .With<WorkflowTypeIndex>(x => x.Latest)
                .ListAsync();
        }

        public Task SaveAsync(WorkflowType workflowType)
        {
            return SaveAsync(workflowType, false);
        }

        public async Task SaveAsync(WorkflowType workflowType, bool newVersion)
        {
            var isNew = workflowType.Id == 0;
            if (newVersion)
            {
                // ¶Ï¿ªÒýÓÃ 
                workflowType = JObject.FromObject(workflowType).ToObject<WorkflowType>();
                var existsedEntity = await GetAsync(workflowType.WorkflowTypeId);
                if (existsedEntity != null)
                {
                    await DeleteAsync(existsedEntity);
                }
                // reset as new entity.
                workflowType.SetWorkflowTypeVersionAudit(audit =>
                {
                    audit.WorkflowTypeVersionId = _idGenerator.GenerateUniqueId(workflowType);
                });
                workflowType.Id = 0;
            }
            workflowType.SetWorkflowTypeVersionAudit(audit =>
            {
                audit.DisplayName ??= workflowType.Name;
                audit.WorkflowTypeVersionId ??= _idGenerator.GenerateUniqueId(workflowType);
                audit.Latest = true;
                audit.ModifiedUtc = _clock.UtcNow;
                audit.ModifiedBy = _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            });

            await _session.SaveAsync(workflowType);

            if (isNew)
            {
                workflowType.SetWorkflowTypeVersionAudit(audit =>
                {
                    audit.CreatedUtc = _clock.UtcNow;
                    audit.CreatedBy = audit.ModifiedBy;
                });

                var context = new WorkflowTypeCreatedContext(workflowType);
                await _handlers.InvokeAsync((handler, context) => handler.CreatedAsync(context), context, _logger);
            }
            else
            {
                var context = new WorkflowTypeUpdatedContext(workflowType);
                await _handlers.InvokeAsync((handler, context) => handler.UpdatedAsync(context), context, _logger);
            }
            await _session.SaveChangesAsync();
        }

        public async Task DeleteAsync(WorkflowType workflowType)
        {
            workflowType.SetWorkflowTypeVersionAudit(audit =>
            {
                audit.Latest = false;
                audit.ModifiedUtc = _clock.UtcNow;
                audit.ModifiedBy = _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            });
            await _session.SaveAsync(workflowType);
            var context = new WorkflowTypeDeletedContext(workflowType);
            await _handlers.InvokeAsync((handler, context) => handler.DeletedAsync(context), context, _logger);
        }
    }
}
