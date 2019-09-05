using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AdoAutoStateTransitionsEngine
{
    public class AdoEngine
    {
        public AdoEngine(string adoOrganization, string pat, ILogger logger)
        {
            var c = new VssBasicCredential(string.Empty, pat);
            var connection = new VssConnection(new Uri(adoOrganization), c);

            witClient = connection.GetClient<WorkItemTrackingHttpClient>();
            this.logger = logger;
        }

        private async Task UpdateParentState(
            AdoWebHookMessage message, 
            WorkItemState state,
            bool allChildrenMustBeInState = true,
            WorkItemState? checkSourceState = null, 
            bool recursive = false)
        {
            if (!message.IsChangeToState(state))
                return;

            var id = message.WorkItemId();
            while (id > 0)
            {
                string reason = string.Format("{0} state rule", state);
                logger.LogInformation("Executing {0} for work item {1}", reason, id);

                var parent = await GetParentWorkItem(id);
                bool updateParent = true;
                if (parent != null && allChildrenMustBeInState)
                {
                    var allChildren = GetChildrenWorkItems(parent).Select(ac => ac.Result);
                    logger.LogTrace("Parent work item {0} has {1} children", parent?.Id, allChildren.Count());

                    updateParent = allChildren.All(c => c.GetState() == state.ToString());
                }

                logger.LogTrace("Parent work item {0} in state {1}", parent?.Id, parent.GetState());
                if (updateParent && parent != null && (!checkSourceState.HasValue || parent.IsInState(checkSourceState.Value)))
                {
                    await UpdateWorkItemState(parent.Id.GetValueOrDefault(), state.ToString(), reason);
                }

                id = (recursive && parent != null) ? parent.Id.GetValueOrDefault() : 0;
            }
        }

        public async Task UpdateActiveState(AdoWebHookMessage message)
        {
            await UpdateParentState(message, WorkItemState.Active, false, WorkItemState.New, true);
        }
        public async Task UpdateResolvedState(AdoWebHookMessage message)
        {
            await UpdateParentState(message, WorkItemState.Resolved);
        }

        public async Task UpdateClosedState(AdoWebHookMessage message)
        {
            if (!message.IsChangeToClosed())
                return;

            const string reason = "closed state rule";
            logger.LogInformation("Executing {0} for work item {1}", reason, message.WorkItemId());

            var parent = await GetParentWorkItem(message.WorkItemId());
            var allChildren = GetChildrenWorkItems(parent).Select(ac => ac.Result);

            logger.LogTrace("Parent work item {0} has {1} children", parent?.Id, allChildren.Count());

            if (allChildren
                .All(c => c.GetState() == WorkItemState.Closed.ToString() || c.GetState() == WorkItemState.Removed.ToString()))
            {
                logger.LogTrace("Parent work item {0} has all children in Closed or Removed", parent?.Id);
                var targetState = 
                    allChildren.Any(c => c.GetState() == WorkItemState.Closed.ToString()) ?
                        WorkItemState.Closed.ToString() :
                        WorkItemState.Removed.ToString();

                await UpdateWorkItemState(parent.Id.GetValueOrDefault(), targetState, reason);
            }
        }

        public async Task<WorkItem> GetWorkItem(int workItemId)
        {
            var workItem = await witClient.GetWorkItemAsync(workItemId, null, null, WorkItemExpand.Relations);
            return workItem;
        }
        public async Task<WorkItem> GetWorkItem(string workItemId)
        {
            int id;
            if (!int.TryParse(workItemId, out id))
                return null;

            return await GetWorkItem(id);
        }

        public async Task<WorkItem> GetParentWorkItem(int workItemId)
        {
            var workItem = await GetWorkItem(workItemId);
            var parent = workItem.Relations.FirstOrDefault(r => r.Rel == "System.LinkTypes.Hierarchy-Reverse");
            if (parent == null)
                return null;

            return await GetWorkItem(parent.Url.Split('/').Last());
        }

        public IEnumerable<Task<WorkItem>> GetChildrenWorkItems(WorkItem workItem)
        {
            return
                workItem
                .Relations
                .Where(r => r.Rel == "System.LinkTypes.Hierarchy-Forward")
                .Select(r => r.Url.Split('/').Last())
                .Select(async ids => await GetWorkItem(ids));
        }

        public IEnumerable<Task<WorkItem>> GetChildrenWorkItems(int workItemId)
        {
            var workItem = GetWorkItem(workItemId).Result;
            return GetChildrenWorkItems(workItem);
        }

        public async Task<WorkItem> UpdateWorkItemState(WorkItem workItem, string newState)
        {
            return await UpdateWorkItemState(workItem.Id.GetValueOrDefault(), newState);
        }

        public async Task<WorkItem> UpdateWorkItemState(int id, string newState, string reason = "[test]")
        {
            logger.LogInformation("Updating state for work item {0} to {1} due to {2}", id, newState, reason);

            var patchDocument = new JsonPatchDocument();
            patchDocument.Add(new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = "/fields/System.State",
                Value = newState
            });
            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.History",
                    Value = string.Format("ADO auto state transitions engine updating state to {0} due to {1}", newState, reason),
                }
            );

            return await witClient.UpdateWorkItemAsync(patchDocument, id);
        }

        WorkItemTrackingHttpClient witClient;
        ILogger logger;
    }
}
