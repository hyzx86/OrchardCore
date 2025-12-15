using EasyOC.Core.Shared.Workflows;
using Microsoft.Extensions.Logging;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.Workflows.Indexes;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using System;
using System.Threading.Tasks;
using YesSql;
using YesSql.Sql;

namespace OrchardCore.Workflows
{
    public class Migrations : DataMigration
    {
        private readonly IWorkflowStore _workflowStore;
        private readonly IVersioningWorkflowTypeStore _workflowTypeStore;
        private readonly ISession _session;
        private readonly IClock _clock;
        private readonly ILogger _logger;
        public Migrations(IWorkflowStore workflowStore, IVersioningWorkflowTypeStore workflowTypeStore, ISession session, IClock clock, ILogger<Migrations> logger)
        {
            _workflowStore = workflowStore;
            _workflowTypeStore = workflowTypeStore;
            _session = session;
            _clock = clock;
            _logger = logger;
        }

        public async Task<int> CreateAsync()
        {
            await SchemaBuilder.CreateMapIndexTableAsync<WorkflowTypeIndex>(table => table
                .Column<string>("WorkflowTypeId", c => c.WithLength(26))
                .Column<string>("Name")
                .Column<bool>("IsEnabled")
                .Column<bool>("HasStart")
            );

            await SchemaBuilder.AlterIndexTableAsync<WorkflowTypeIndex>(table => table
                .CreateIndex("IDX_WorkflowTypeIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "Name",
                    "IsEnabled",
                    "HasStart")
            );

            await SchemaBuilder.CreateMapIndexTableAsync<WorkflowTypeStartActivitiesIndex>(table => table
                .Column<string>("WorkflowTypeId")
                .Column<string>("Name")
                .Column<bool>("IsEnabled")
                .Column<string>("StartActivityId")
                .Column<string>("StartActivityName")
            );

            await SchemaBuilder.AlterIndexTableAsync<WorkflowTypeStartActivitiesIndex>(table => table
                .CreateIndex("IDX_WorkflowTypeStartActivitiesIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "StartActivityId",
                    "StartActivityName",
                    "IsEnabled")
            );

            await SchemaBuilder.CreateMapIndexTableAsync<WorkflowIndex>(table => table
                .Column<string>("WorkflowTypeId", c => c.WithLength(26))
                .Column<string>("WorkflowId", c => c.WithLength(26))
                .Column<string>("WorkflowStatus", c => c.WithLength(26))
                .Column<DateTime>("CreatedUtc")
            );

            await SchemaBuilder.AlterIndexTableAsync<WorkflowIndex>(table => table
                .CreateIndex("IDX_WorkflowIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "WorkflowId",
                    "WorkflowStatus",
                    "CreatedUtc")
            );

            await SchemaBuilder.CreateMapIndexTableAsync<WorkflowBlockingActivitiesIndex>(table => table
                .Column<string>("ActivityId")
                .Column<string>("ActivityName")
                .Column<bool>("ActivityIsStart")
                .Column<string>("WorkflowTypeId")
                .Column<string>("WorkflowId")
                .Column<string>("WorkflowCorrelationId")
            );

            await SchemaBuilder.AlterIndexTableAsync<WorkflowBlockingActivitiesIndex>(table => table
                .CreateIndex("IDX_WFBlockingActivities_DocumentId_ActivityId",
                    "DocumentId",
                    "ActivityId",
                    "WorkflowTypeId",
                    "WorkflowId")
            );

            await SchemaBuilder.AlterIndexTableAsync<WorkflowBlockingActivitiesIndex>(table => table
                .CreateIndex("IDX_WFBlockingActivities_DocumentId_ActivityName",
                    "DocumentId",
                    "ActivityName",
                    "WorkflowTypeId",
                    "WorkflowCorrelationId")
            );

            // Shortcut other migration steps on new content definition schemas.
            return 3;
        }

        // This code can be removed in a later version.
        public async Task<int> UpdateFrom1Async()
        {
            await SchemaBuilder.AlterIndexTableAsync<WorkflowIndex>(table =>
            {
                table.AddColumn<string>("WorkflowStatus");
            });

            return 2;
        }

        // This code can be removed in a later version.
        public async Task<int> UpdateFrom2Async()
        {
            await SchemaBuilder.AlterIndexTableAsync<WorkflowTypeIndex>(table => table
                .CreateIndex("IDX_WorkflowTypeIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "Name",
                    "IsEnabled",
                    "HasStart")
            );

            await SchemaBuilder.AlterIndexTableAsync<WorkflowTypeStartActivitiesIndex>(table => table
                .CreateIndex("IDX_WorkflowTypeStartActivitiesIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "StartActivityId",
                    "StartActivityName",
                    "IsEnabled")
            );

            await SchemaBuilder.AlterIndexTableAsync<WorkflowIndex>(table => table
                .CreateIndex("IDX_WorkflowIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "WorkflowId",
                    "WorkflowStatus",
                    "CreatedUtc")
            );

            await SchemaBuilder.AlterIndexTableAsync<WorkflowBlockingActivitiesIndex>(table => table
                .CreateIndex("IDX_WFBlockingActivities_DocumentId_ActivityId",
                    "DocumentId",
                    "ActivityId",
                    "WorkflowTypeId",
                    "WorkflowId")
            );

            await SchemaBuilder.AlterIndexTableAsync<WorkflowBlockingActivitiesIndex>(table => table
                .CreateIndex("IDX_WFBlockingActivities_DocumentId_ActivityName",
                    "DocumentId",
                    "ActivityName",
                    "WorkflowTypeId",
                    "WorkflowCorrelationId")
            );

            return 3;
        }

        public async Task<int> UpdateFrom3Async()
        {
            await SchemaBuilder.AlterIndexTableAsync<WorkflowTypeIndex>(table =>
            {

                table.AddColumn<string>(nameof(WorkflowTypeIndex.DisplayName), c => c.WithLength(255));
                table.AddColumn<string>(nameof(WorkflowTypeIndex.Description));
                table.AddColumn<string>(nameof(WorkflowTypeIndex.WorkflowTypeVersionId), c => c.WithLength(26));
                table.AddColumn<bool>(nameof(WorkflowTypeIndex.Latest), config => config.Nullable());
                table.AddColumn<DateTime>(nameof(WorkflowTypeIndex.CreatedUtc), config => config.Nullable());
                table.AddColumn<DateTime>(nameof(WorkflowTypeIndex.ModifiedUtc), config => config.Nullable());
                table.AddColumn<string>(nameof(WorkflowTypeIndex.CreatedBy));
                table.AddColumn<string>(nameof(WorkflowTypeIndex.ModifiedBy));
                table.DropIndex("IDX_WorkflowTypeIndex_DocumentId");
            });

            await SchemaBuilder.AlterIndexTableAsync<WorkflowTypeIndex>(table =>
            {
                table.CreateIndex("IDX_WorkflowTypeIndex_DocumentId",
                                    "DocumentId",
                                    "WorkflowTypeId",
                                    "Name",
                                    "IsEnabled",
                                    "HasStart",
                                    // new
                                    "WorkflowTypeVersionId",
                                    "Latest",
                                    "CreatedUtc",
                                    "ModifiedUtc",
                                    "CreatedBy",
                                    "ModifiedBy"
                                    );
            });

            await SchemaBuilder.AlterIndexTableAsync<WorkflowTypeStartActivitiesIndex>(table =>
            {
                table.AddColumn<string>(nameof(WorkflowTypeStartActivitiesIndex.WorkflowTypeVersionId), c => c.WithLength(26));
                table.DropIndex("IDX_WorkflowTypeStartActivitiesIndex_DocumentId");
            });

            await SchemaBuilder.AlterIndexTableAsync<WorkflowTypeStartActivitiesIndex>(table =>
                table.CreateIndex("IDX_WorkflowTypeStartActivitiesIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "StartActivityId",
                    "StartActivityName",
                    "IsEnabled",
                    // new
                    "WorkflowTypeVersionId"));

            await SchemaBuilder.AlterIndexTableAsync<WorkflowIndex>(table =>
            {
                table.AddColumn<string>(nameof(WorkflowIndex.WorkflowTypeVersionId), c => c.WithLength(26));
                table.AddColumn<DateTime>(nameof(WorkflowIndex.LastExecutedOnUtc), c => c.Nullable());
                table.AddColumn<string>(nameof(WorkflowIndex.StatusName));
                table.DropIndex("IDX_WorkflowIndex_DocumentId");
            });

            await SchemaBuilder.AlterIndexTableAsync<WorkflowIndex>(table => table
                   .CreateIndex("IDX_WorkflowIndex_DocumentId",
                       "DocumentId",
                       "WorkflowTypeId",
                       "WorkflowId",
                       "WorkflowStatus",
                       "CreatedUtc",
                       // new
                       "WorkflowTypeVersionId",
                       "LastExecutedOnUtc",
                       "StatusName"
                       )
               );


            await SchemaBuilder.AlterIndexTableAsync<WorkflowBlockingActivitiesIndex>(table =>
            {
                table.AddColumn<string>(nameof(WorkflowBlockingActivitiesIndex.WorkflowTypeVersionId), c => c.WithLength(26));
                table.DropIndex("IDX_WFBlockingActivities_DocumentId_ActivityId");
            });

            await SchemaBuilder.AlterIndexTableAsync<WorkflowBlockingActivitiesIndex>(table => table
                .CreateIndex("IDX_WFBlockingActivities_DocumentId_ActivityId",
                    "DocumentId",
                    "ActivityId",
                    "WorkflowTypeId",
                    "WorkflowId",
                    // new
                    nameof(WorkflowBlockingActivitiesIndex.WorkflowTypeVersionId)
                    )
            );

            return 4;
        }

        public async Task<int> UpdateFrom4Async()
        {
            //ShellScope.AddDeferredTask(async scope =>
            //{
            //    var _workflowStore = scope.ServiceProvider.GetRequiredService<IWorkflowStore>();
            //    var _workflowTypeStore = scope.ServiceProvider.GetRequiredService<IVersioningWorkflowTypeStore>(); ;
            //    var _session = scope.ServiceProvider.GetRequiredService<ISession>();
            //    var _store = scope.ServiceProvider.GetRequiredService<IStore>();

            //    var _clock = scope.ServiceProvider.GetRequiredService<IClock>();
            //    var _logger = scope.ServiceProvider.GetRequiredService<ILogger<Migrations>>();

            //    _logger.LogWarning("Run workflow migration UpdateFrom4Async Begin.");

            //    var existsedTypes = await _session.Query<WorkflowType>().ListAsync();
            //    foreach (var workflowType in existsedTypes)
            //    {
            //        var exisAudit = workflowType.GetWorkflowTypeVersionAudit();
            //        if (exisAudit.WorkflowTypeVersionId.IsNullOrEmpty())
            //        {
            //            workflowType.SetWorkflowTypeVersionAudit(audit =>
            //            {
            //                audit.DisplayName = workflowType.Name;
            //                audit.Latest = true;
            //                audit.CreatedUtc = _clock.UtcNow;
            //                audit.ModifiedUtc = _clock.UtcNow;
            //            });

            //            await _workflowTypeStore.SaveAsync(workflowType, true);
            //        }
            //    }

            //    _logger.LogWarning("Run workflow migration UpdateFrom4Async Done.");
            //});
            try
            {
                _logger.LogWarning("Run workflow migration UpdateFrom4Async Begin.");

                var existsedTypes = await _session.Query<WorkflowType>().ListAsync();
                foreach (var workflowType in existsedTypes)
                {
                    var exisAudit = workflowType.GetWorkflowTypeVersionAudit();
                    if (exisAudit.WorkflowTypeVersionId.IsNullOrEmpty())
                    {
                        workflowType.SetWorkflowTypeVersionAudit(audit =>
                        {
                            audit.DisplayName = workflowType.Name;
                            audit.Latest = true;
                            audit.CreatedUtc = _clock.UtcNow;
                            audit.ModifiedUtc = _clock.UtcNow;
                        });

                        await _workflowTypeStore.SaveAsync(workflowType, true);
                    }
                }

                _logger.LogWarning("Run workflow migration UpdateFrom4Async Done.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "run workflow migration UpdateFrom4Async Error.");
                throw;
            }
            return 5;
        }
    }
}
