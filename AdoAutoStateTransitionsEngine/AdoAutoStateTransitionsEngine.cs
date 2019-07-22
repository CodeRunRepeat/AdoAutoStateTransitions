using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public async void UpdateActiveStates(AdoWebHookMessage message)
        {
            if (!message.IsChangeToActive())
                return;

            var parent = await GetParentWorkItem(message.WorkItemId());
            if (parent.IsStateNew())
                await UpdateWorkItemState(parent.Id.GetValueOrDefault(), WorkItemState.Active.ToString());
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
        
        public async Task<WorkItem> UpdateWorkItemState(WorkItem workItem, string newState)
        {
            return await UpdateWorkItemState(workItem.Id.GetValueOrDefault(), newState);
        }

        private async Task<WorkItem> UpdateWorkItemState(int id, string newState)
        {
            var patchDocument = new JsonPatchDocument();
            patchDocument.Add(new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = "/fields/System.State",
                Value = newState
            });

            return await witClient.UpdateWorkItemAsync(patchDocument, id);
        }

        WorkItemTrackingHttpClient witClient;
    }
}
