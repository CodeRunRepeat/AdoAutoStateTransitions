using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AdoAutoStateTransitionsEngine
{
    public enum WorkItemState
    {
        Unknown,
        New,
        Active,
        Resolved,
        Closed,
        Removed,
    }

    public static class AdoAutoStateTransitionsEngineExtensions
    {
        public static string GetState(this WorkItem workItem)
        {
            return (workItem?.Fields["System.State"] as string);
        }

        public static bool IsInState(this WorkItem workItem, WorkItemState state)
        {
            return GetState(workItem) == state.ToString();
        }

        public static bool IsStateNew(this WorkItem workItem)
        {
            return IsInState(workItem, WorkItemState.New);
        }

        public static bool IsWorkItemUpdate(this AdoWebHookMessage message)
        {
            return message?.eventType == "workitem.updated";
        }

        public static bool IsStateChange(this AdoWebHookMessage message)
        {
            return
                message.IsWorkItemUpdate() &&
                message?.resource?.fields?.SystemState != null &&
                message?.resource?.fields?.SystemState.oldValue != message?.resource?.fields?.SystemState.newValue;
        }

        public static bool IsChangeToState(this AdoWebHookMessage message, WorkItemState state)
        {
            return
                message.IsStateChange() &&
                message?.resource?.fields?.SystemState.newValue == state.ToString();
        }

        public static bool IsChangeToActive(this AdoWebHookMessage message)
        {
            return IsChangeToState(message, WorkItemState.Active);
        }

        public static bool IsChangeToClosed(this AdoWebHookMessage message)
        {
            return
                IsChangeToState(message, WorkItemState.Closed) ||
                IsChangeToState(message, WorkItemState.Removed);
        }

        public static int WorkItemId(this AdoWebHookMessage message)
        {
            return (message?.resource?.workItemId).GetValueOrDefault();
        }
    }
}
