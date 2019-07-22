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

namespace AdoAutoStateTransitionsEngine
{
    public class AdoEngine
    {
        public AdoEngine(string adoOrganization, string pat)
        {
            var c = new VssBasicCredential(string.Empty, pat);
            var connection = new VssConnection(new Uri(adoOrganization), c);

            witClient = connection.GetClient<WorkItemTrackingHttpClient>();
        }

        public async Task UpdateActiveState(AdoWebHookMessage message)
        {
            if (!message.IsChangeToActive())
                return;

            var id = message.WorkItemId();
            while (id > 0)
            {
                var parent = await GetParentWorkItem(id);
                if (parent != null && parent.IsStateNew())
                    await UpdateWorkItemState(parent.Id.GetValueOrDefault(), WorkItemState.Active.ToString(), "active state rule");

                id = parent == null ? 0 : parent.Id.GetValueOrDefault();
            }
        }

        public async Task UpdateClosedState(AdoWebHookMessage message)
        {
            if (!message.IsChangeToClosed())
                return;

            var parent = await GetParentWorkItem(message.WorkItemId());
            var allChildren = GetChildrenWorkItems(parent).Select(ac => ac.Result);

            if (allChildren
                .All(c => c.GetState() == WorkItemState.Closed.ToString() || c.GetState() == WorkItemState.Removed.ToString()))
            {
                var targetState = 
                    allChildren.Any(c => c.GetState() == WorkItemState.Closed.ToString()) ?
                        WorkItemState.Closed.ToString() :
                        WorkItemState.Removed.ToString();

                await UpdateWorkItemState(parent.Id.GetValueOrDefault(), targetState, "closed state rule");
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
    }
}
