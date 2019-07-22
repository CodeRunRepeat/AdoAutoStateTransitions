using Newtonsoft.Json;
using System;

namespace AdoAutoStateTransitionsEngine
{
    public class AdoWebHookMessage
    {
        public string id { get; set; }
        public string eventType { get; set; }
        public string publisherId { get; set; }
        public Message message { get; set; }
        public Message detailedMessage { get; set; }
        public Resource resource { get; set; }
        public string resourceVersion { get; set; }
        public ResourceContainers resourceContainers { get; set; }
        public DateTime createdDate { get; set; }
    }

    public class Message
    {
        public string text { get; set; }
        public string html { get; set; }
        public string markdown { get; set; }
    }

    public class Resource
    {
        public int id { get; set; }
        public int workItemId { get; set; }
        public int rev { get; set; }
        public RevisedBy revisedBy { get; set; }
        public DateTime revisedDate { get; set; }
        public Fields fields { get; set; }
        public _Links1 _links { get; set; }
        public string url { get; set; }
        public Revision revision { get; set; }
    }

    public class RevisedBy
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public string url { get; set; }
        public _Links _links { get; set; }
        public string uniqueName { get; set; }
        public string imageUrl { get; set; }
        public string descriptor { get; set; }
    }

    public class _Links
    {
        public Href avatar { get; set; }
    }

    public class Fields
    {
        [JsonProperty(PropertyName = "System.Rev")]
        public StringChange SystemRev { get; set; }
        [JsonProperty(PropertyName = "System.State")]
        public StringChange SystemState { get; set; }
        [JsonProperty(PropertyName = "System.Reason")]
        public StringChange SystemReason { get; set; }
        [JsonProperty(PropertyName = "System.AssignedTo")]
        public StringChange SystemAssignedTo { get; set; }
        [JsonProperty(PropertyName = "System.ChangedDate")]
        public DateChange SystemChangedDate { get; set; }
        [JsonProperty(PropertyName = "System.Watermark")]
        public StringChange SystemWatermark { get; set; }
    }

    public class DateChange
    {
        public DateTime oldValue { get; set; }
        public DateTime newValue { get; set; }
    }

    public class StringChange
    {
        public string oldValue { get; set; }
        public string newValue { get; set; }
    }

    public class _Links1
    {
        public Href self { get; set; }
        public Href parent { get; set; }
        public Href workItemUpdates { get; set; }
    }

    public class Href
    {
        public string href { get; set; }
    }

    public class Revision
    {
        public int id { get; set; }
        public int rev { get; set; }
        public Fields1 fields { get; set; }
        public string url { get; set; }
    }

    public class Fields1
    {
        public string SystemAreaPath { get; set; }
        public string SystemTeamProject { get; set; }
        public string SystemIterationPath { get; set; }
        public string SystemWorkItemType { get; set; }
        public string SystemState { get; set; }
        public string SystemReason { get; set; }
        public DateTime SystemCreatedDate { get; set; }
        public User SystemCreatedBy { get; set; }
        public DateTime SystemChangedDate { get; set; }
        public User SystemChangedBy { get; set; }
        public string SystemTitle { get; set; }
        public string MicrosoftVSTSCommonSeverity { get; set; }
        public string WEF_EB329F44FE5F4A94ACB1DA153FDF38BA_KanbanColumn { get; set; }
    }

    public class User
    {
        public string displayName { get; set; }
        public string url { get; set; }
        public _Links _links { get; set; }
        public string id { get; set; }
        public string uniqueName { get; set; }
        public string imageUrl { get; set; }
        public string descriptor { get; set; }
    }

    public class ResourceContainers
    {
        public Container collection { get; set; }
        public Container account { get; set; }
        public Container project { get; set; }
    }

    public class Container
    {
        public string id { get; set; }
    }
}
